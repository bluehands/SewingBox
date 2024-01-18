using System.Data;
using FunicularSwitch.Extensions;
using Microsoft.Data.SqlClient;
using System.Reactive.Linq;
using EventSourcing2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Persistence.EntityFramework.SqlServer;

static class Commands
{
    public static string SelectCommand(uint maxRows) =>
        $"""
         SELECT TOP {maxRows}
         	[{nameof(Event.Position)}],
         	[{nameof(Event.EventType)}],
         	[{nameof(Event.StreamId)}],
         	[{nameof(Event.StreamType)}],
         	[{nameof(Event.Timestamp)}],
         	[{nameof(Event.Payload)}]
         FROM dbo.Events
         WHERE {nameof(Event.Position)} > @{nameof(Event.Position)}
         ORDER BY {nameof(Event.Position)}
         """;
}

public static class BrokerNotificationEventStream
{
    class SqlServerExecutor : IDbExecutor
    {
        readonly IServiceProvider _serviceProvider;

        public SqlServerExecutor(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<T> Execute<T>(Func<SqlConnection, Task<T>> executeWithConnection)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<EventStoreContext>();
            return await executeWithConnection((SqlConnection)dbContext.Database.GetDbConnection());
        }
    }

    public static void AddEventStream(IServiceCollection services, uint maxRowsPerSelect)
    {
        services.AddSingleton(sp =>
        {
            var scope = sp.CreateScope();
            var eventMapper = scope.ServiceProvider.GetRequiredService<IEventMapper<Event>>();

            var innerStream = ChangeListener.GetChangeStream(new SqlServerExecutor(sp), (lastPosition, connection) =>
                    {
                        var cmd = connection.CreateCommand();
                        cmd.CommandText = Commands.SelectCommand(maxRowsPerSelect);
                        cmd.Parameters.Add(new($"@{nameof(Event.Position)}", SqlDbType.BigInt)
                        {
                            Value = lastPosition
                        });
                        return cmd;
                    },
                    async (reader, state, _) =>
                    {
                        var dbEvents = ReadEvents(reader);
                        var result = await eventMapper.MapFromDbEvents(dbEvents).ToListAsync();
                        var nextState = result.Count > 0 ? result[^1].Position : state;
                        return (result, nextState);
                    }, 0L)
                .SelectMany(l => l);

            return new EventStream<EventSourcing2.Event>(innerStream, scope);
        });
    }

    static async IAsyncEnumerable<Event> ReadEvents(SqlDataReader reader)
    {
        var ordinal = GetOrdinal(reader);
        while (await reader.ReadAsync())
        {
            Event @event;
            try
            {
                @event = Read(reader, ordinal);
            }
            catch (Exception e)
            {
                //Log.ASR.Error(e, "Failed to deserialize message, skipped");
                continue;
            }

            yield return @event;
        }
    }

    static Event Read(SqlDataReader reader, EventOrdinal ordinal) =>
        new(
            reader.GetInt64(ordinal.Position),
            reader.GetString(ordinal.StreamType),
            reader.GetString(ordinal.StreamId),
            reader.GetString(ordinal.EventType),
            reader.GetString(ordinal.Payload),
            reader.GetDateTimeOffset(ordinal.Timestamp)
        );

    static EventOrdinal GetOrdinal(SqlDataReader reader) =>
        new(reader.GetOrdinal(nameof(Event.Position)),
            reader.GetOrdinal(nameof(Event.EventType)),
            reader.GetOrdinal(nameof(Event.StreamType)),
            reader.GetOrdinal(nameof(Event.StreamId)),
            reader.GetOrdinal(nameof(Event.Timestamp)),
            reader.GetOrdinal(nameof(Event.Payload))
        );

    record struct EventOrdinal(int Position, int EventType, int StreamType, int StreamId, int Timestamp, int Payload);
}

interface IDbExecutor
{
    Task<T> Execute<T>(Func<SqlConnection, Task<T>> executeWithConnection);
}

static class ChangeListener
{
    public static IObservable<T> GetChangeStream<T, TState>(
        IDbExecutor dbExecutor,
        Func<TState, SqlConnection, SqlCommand> createCommand,
        Func<SqlDataReader, TState, SqlNotificationInfo?, Task<(T result, TState nextState)>> read,
        TState initialState) =>
        Observable.Create<T>(async (observer, cancellationToken) =>
        {
            await dbExecutor.Execute(con =>
            {
                //TODO: call stop when disposed????
                SqlDependency.Start(con.ConnectionString);
                return Task.FromResult(42);
            });

            var listener = new SqlDependencyListener<T, TState>(dbExecutor, initialState, createCommand, read, observer, cancellationToken);
            await listener.Run();
        });

    class SqlDependencyListener<T, TState>
    {
        readonly IDbExecutor _dbExecutor;
        readonly Func<TState, SqlConnection, SqlCommand> _createCommand;
        readonly Func<SqlDataReader, TState, SqlNotificationInfo?, Task<(T result, TState nextState)>> _read;
        readonly IObserver<T> _observer;
        readonly CancellationToken _cancellationToken;
        TState _currentState;
        readonly string _name;
        readonly ILogger<SqlDependencyListener<T, TState>>? _log;

        public SqlDependencyListener(
            IDbExecutor dbExecutor,
            TState initialState,
            Func<TState, SqlConnection, SqlCommand> createCommand,
            Func<SqlDataReader, TState, SqlNotificationInfo?, Task<(T result, TState nextState)>> read,
            IObserver<T> observer,
            CancellationToken cancellationToken,
            ILogger<SqlDependencyListener<T, TState>>? log = null)
        {
            _dbExecutor = dbExecutor;
            _createCommand = createCommand;
            _read = read;
            _observer = observer;
            _cancellationToken = cancellationToken;
            _log = log;
            _currentState = initialState;
            _name = typeof(T).BeautifulName();
        }

        public async Task Run()
        {
            SqlNotificationInfo? notificationInfo = null;
            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    notificationInfo = await ReloadData(notificationInfo);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _log?.LogWarning(e, $"{_name} SqlDependency failed, retrying in 5 seconds");
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), _cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        Task<SqlNotificationInfo> ReloadData(SqlNotificationInfo? changeInfo)
        {
            return _dbExecutor.Execute(async connection =>
            {
                await connection.OpenAsync(_cancellationToken);

                await using var command = _createCommand(_currentState, connection);
                var dependency = new SqlDependency(command);
                var tcs = new TaskCompletionSource<SqlNotificationInfo>(TaskCreationOptions.RunContinuationsAsynchronously);
                dependency.OnChange += (_, e) =>
                {
                    if (e.Info == SqlNotificationInfo.Invalid)
                        _log?.LogError($"{_name} sql dependency error: {e.Type}, {e.Info}, {e.Source}. Sql command might not be valid for sql dependency.");

                    _log?.LogDebug($"{_name} sql dependency change received: {e.Type}, {e.Info}, {e.Source}");
                    tcs.TrySetResult(e.Info);
                };

                await using (var reader = await command.ExecuteReaderAsync(_cancellationToken))
                {
                    var (result, nextState) = await _read(reader, _currentState, changeInfo);
                    _currentState = nextState;
                    _observer.OnNext(result);
                }

                return await tcs.Task.WaitAsync(_cancellationToken);
            });
        }
    }
}