using System;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using FunicularSwitch.Generators;
using SqlStreamStore;

namespace EventSourcing.Persistence.SqlStreamStore;

/// <summary>
/// Options to use sql stream store package as underlying event store
/// </summary>
/// <param name="CreateStore">
/// If left empty IStreamStore has to be added to di container externally
/// </param> 
/// <param name="CreateSerializer">
/// If left empty IEventSerializer&lt;string&gt; has to be added to di container externally
/// </param>
/// <param name="PollingOptions"></param>
/// <param name="GetLastProcessedEventPosition"></param>
public record SqlStreamEventStoreOptions(
	Func<IServiceProvider, IStreamStore>? CreateStore,
	Func<IServiceProvider, IEventSerializer<string>>? CreateSerializer,
	PollingOptions PollingOptions,
	Func<Task<long>> GetLastProcessedEventPosition
);

[UnionType(CaseOrder = CaseOrder.AsDeclared)]
public abstract class PollingOptions
{
	public static readonly PollingOptions NoPolling = new NoPolling_();

	public static PollingOptions UsePolling(PeriodicObservable.PollStrategy<Event, long> pollStrategy, TimeSpan minPollInterval, TimeSpan maxPollInterval) => new UsePolling_(pollStrategy, minPollInterval, maxPollInterval);

	public class NoPolling_ : PollingOptions
	{
		public NoPolling_() : base(UnionCases.NoPolling)
		{
		}
	}

	public class UsePolling_ : PollingOptions
	{
		public PeriodicObservable.PollStrategy<Event, long> PollStrategy { get; }
		public TimeSpan MinPollInterval { get; }
		public TimeSpan MaxPollInterval { get; }

		public UsePolling_(PeriodicObservable.PollStrategy<Event, long> pollStrategy, TimeSpan minPollInterval, TimeSpan maxPollInterval) : base(UnionCases.UsePolling)
		{
			PollStrategy = pollStrategy;
			MinPollInterval = minPollInterval;
			MaxPollInterval = maxPollInterval;
		}
	}

	internal enum UnionCases
	{
		NoPolling,
		UsePolling
	}

	internal UnionCases UnionCase { get; }
	PollingOptions(UnionCases unionCase) => UnionCase = unionCase;

	public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
	bool Equals(PollingOptions other) => UnionCase == other.UnionCase;

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((PollingOptions)obj);
	}

	public override int GetHashCode() => (int)UnionCase;
}