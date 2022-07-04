using System;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using FunicularSwitch.Generators;
using SqlStreamStore;

namespace EventSourcing.Persistence.SqlStreamStore;

public record SqlStreamEventStoreOptions(Func<IServiceProvider, IStreamStore> CreateStore, Func<IServiceProvider, IEventSerializer<string>> CreateSerializer, PollingOptions PollingOptions, Func<Task<long>> GetLastProcessedEventPosition);

[UnionType(CaseOrder = CaseOrder.AsDeclared)]
public abstract class PollingOptions
{
	public static readonly PollingOptions NoPolling = new NoPolling_();

	public static PollingOptions UsePolling(PeriodicObservable.PollStrategy<Event, long> pollStrategy) => new UsePolling_(pollStrategy);

	public class NoPolling_ : PollingOptions
	{
		public NoPolling_() : base(UnionCases.NoPolling)
		{
		}
	}

	public class UsePolling_ : PollingOptions
	{
		public PeriodicObservable.PollStrategy<Event, long> PollStrategy { get; }

		public UsePolling_(PeriodicObservable.PollStrategy<Event, long> pollStrategy) : base(UnionCases.UsePolling) => PollStrategy = pollStrategy;
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