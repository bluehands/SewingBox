using System;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using FunicularSwitch.Generators;

namespace EventSourcing.Persistence.SqlStreamStore;

public record SqlStreamEventStoreOptions(
	PollingOptions PollingOptions,
	Func<Task<long>> GetLastProcessedEventPosition
)
{
	public static SqlStreamEventStoreOptions Create(PollingOptions? pollingOptions = null, Func<Task<long>>? getLastProcessedEventPosition = null) =>
		new(
			pollingOptions ?? PollingOptions.UsePolling(EventStream.PollStrategyRetryOnFail(5),
				TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2)),
			 getLastProcessedEventPosition ?? (() => Task.FromResult(-1L))
		);
}

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