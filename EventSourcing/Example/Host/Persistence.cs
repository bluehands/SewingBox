namespace Example.Host;
[FunicularSwitch.Generators.UnionType(CaseOrder = FunicularSwitch.Generators.CaseOrder.AsDeclared)]
public abstract class Persistence
{
	public static readonly Persistence InMemoryNoMappers = new InMemoryNoMappers_();
	public static readonly Persistence SqlStreamStoreInMemory = new SqlStreamStoreInMemory_();

	public static Persistence MsSqlStreamStore(string connectionString) => new MsSqlStreamStore_(connectionString);
	public static Persistence SQLite(string connectionString) => new SQLite_(connectionString);

	public class InMemoryNoMappers_ : Persistence
	{
		public InMemoryNoMappers_() : base(UnionCases.InMemoryNoMappers)
		{
		}
	}

	public class SqlStreamStoreInMemory_ : Persistence
	{
		public SqlStreamStoreInMemory_() : base(UnionCases.SqlStreamStoreInMemory)
		{
		}
	}

	public class MsSqlStreamStore_ : Persistence
	{
		public string ConnectionString { get; }

		public MsSqlStreamStore_(string connectionString) : base(UnionCases.MsSqlStreamStore) => ConnectionString = connectionString;
	}

	public class SQLite_ : Persistence
	{
		public string ConnectionString { get; }

		public SQLite_(string connectionString) : base(UnionCases.SQLite) => ConnectionString = connectionString;
	}

	internal enum UnionCases
	{
		InMemoryNoMappers,
		SqlStreamStoreInMemory,
		MsSqlStreamStore,
		SQLite
	}

	internal UnionCases UnionCase { get; }

	Persistence(UnionCases unionCase) => UnionCase = unionCase;
	public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
	bool Equals(Persistence other) => UnionCase == other.UnionCase;
	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj))
			return false;
		if (ReferenceEquals(this, obj))
			return true;
		if (obj.GetType() != GetType())
			return false;
		return Equals((Persistence)obj);
	}

	public override int GetHashCode() => (int)UnionCase;
}

public static class StreamStoreDemoOptions
{
	public const string LocalSqlExpress = @"Server=.\SQLSERVEREXPRESS;Database=Fehlermanagement;Integrated Security=true";
}