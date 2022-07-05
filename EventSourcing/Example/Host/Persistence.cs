using EventSourcing.Events;
using EventSourcing.Persistence.SqlStreamStore;
using SqlStreamStore;

namespace Example.Host;

[FunicularSwitch.Generators.UnionType(CaseOrder = FunicularSwitch.Generators.CaseOrder.AsDeclared)]
public abstract class Persistence
{
	public static readonly Persistence InMemoryNoMappers = new InMemoryNoMappers_();

	public static Persistence SqlStreamStore(SqlStreamEventStoreOptions options) => new SqlStreamStore_(options);

	public class InMemoryNoMappers_ : Persistence
	{
		public InMemoryNoMappers_() : base(UnionCases.InMemoryNoMappers)
		{
		}
	}

	public class SqlStreamStore_ : Persistence
	{
		public SqlStreamEventStoreOptions Options { get; }

		public SqlStreamStore_(SqlStreamEventStoreOptions options) : base(UnionCases.SqlStreamStore) => Options = options;
	}

	internal enum UnionCases
	{
		InMemoryNoMappers,
		SqlStreamStore
	}

	internal UnionCases UnionCase { get; }
	Persistence(UnionCases unionCase) => UnionCase = unionCase;

	public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
	bool Equals(Persistence other) => UnionCase == other.UnionCase;

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((Persistence)obj);
	}

	public override int GetHashCode() => (int)UnionCase;
}

public static class StreamStoreDemoOptions
{
	public static readonly SqlStreamEventStoreOptions InMemory = new(
		CreateSerializer: _ => new JsonEventSerializer(),
		CreateStore: _ => new InMemoryStreamStore(),
		PollingOptions: PollingOptions.UsePolling(EventStream.PollStrategyRetryForever, TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(5)), 
		GetLastProcessedEventPosition: () => Task.FromResult(-1L)
	);

	public static readonly SqlStreamEventStoreOptions LocalSqlExpress = new(
		CreateSerializer: _ => new JsonEventSerializer(),
		CreateStore: _ =>
		{
			var store = new MsSqlStreamStoreV3(
				new(@"Server=.\SQLSERVEREXPRESS;Database=Fehlermanagement;Integrated Security=true"));
			store.CreateSchemaIfNotExists();
			return store;
		},
		PollingOptions: PollingOptions.UsePolling(EventStream.PollStrategyRetryOnFail(5), TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(5)), 
		GetLastProcessedEventPosition: () => Task.FromResult(-1L)
	);
}
