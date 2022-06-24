using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using EventSourcing.Internals;
using AsyncLock = EventSourcing.Internals.AsyncLock;

namespace EventSourcing;

public sealed class CommandStream : IObservable<Command>, IDisposable
{
	readonly AsyncLock _lock = new();
	readonly Subject<Command> _innerStream;
	readonly IObservable<Command> _commands;
	
	public CommandStream()
	{
		_innerStream = new();
		_commands = _innerStream.Publish().RefCount();
	}

	public IDisposable Subscribe(IObserver<Command> observer) => _commands.Subscribe(observer);

	public async Task SendCommand(Command command) => await _lock.ExecuteGuarded(() => _innerStream.OnNext(command)).ConfigureAwait(false);

	// ReSharper disable once ParameterHidesMember
	public Task SendCommands(IEnumerable<Command> commands) => Task.WhenAll(commands.Select(c => SendCommand(c)));

	public void Dispose()
	{
		_lock.Dispose();
		_innerStream.Dispose();
	}
}

public static class CommandStreamExtension
{
	public static IDisposable SubscribeCommandProcessors(this IObservable<Command> commands, GetCommandProcessor getCommandProcessor, WriteEvents writeEvents) =>
		commands
			.Process(getCommandProcessor, writeEvents)
			.Do(r => r.LogResult())
			//.Buffer(TimeSpan.FromMilliseconds(100))
			//.Where(l => l.Count > 0)
			.SubscribeAsync(async processingResult =>
			{
				try
				{
					var commandProcessedEvents = new List<EventPayload> { processingResult.ToCommandProcessedEvent() };
					await writeEvents(commandProcessedEvents).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					Log.Util.Error(e, "Failed to persist command processed events");
				}
			});

	public static IObservable<ProcessingResult> Process(this IObservable<Command> commands,
		GetCommandProcessor getCommandProcessor, WriteEvents writeEvents) =>
		commands
			.SelectMany(async c =>
			{
				var result = await CommandProcessor.Process(c, getCommandProcessor).ConfigureAwait(false);
				return await result.Match(
                        processed: async p =>
						{
							try
							{
								await writeEvents(p.ResultEvents).ConfigureAwait(false);
								return p;
							}
							catch (Exception e)
							{
								return new ProcessingResult.Faulted_(e, p.CommandId);
							}
						},
						cancelled: Task.FromResult<ProcessingResult>,
						faulted: Task.FromResult<ProcessingResult>,
						unhandled: Task.FromResult<ProcessingResult>)
					.ConfigureAwait(false);
			});

	public static async Task<OperationResult<Unit>> SendCommandAndWaitUntilProcessed(this CommandStream commandStream, IObservable<CommandProcessed> commandProcessedEvents, Command command)
	{
		var processed = commandProcessedEvents
			.FirstAsync(c => c.CommandId == command.Id)
			.ToTask(CancellationToken.None, Scheduler.Default); //this is needed if we might be called from sync / async mixtures (https://blog.stephencleary.com/2012/12/dont-block-in-asynchronous-code.html)
		await commandStream.SendCommand(command).ConfigureAwait(false);
		//while (await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(1)), processed) != processed)
		//{
		//	ThreadPool.GetAvailableThreads(out var availableThreads, out var completionPortThreads);
		//	Log.Util.Info($"Waiting for command {command.Id} to be processed. Available threads {availableThreads}, completion threads {completionPortThreads}");
		//}

		return (await processed.ConfigureAwait(false)).OperationResult;
	}
}