using System;
using System.Data.SQLite;
using System.Threading.Tasks;
using FunicularSwitch.Extensions;

namespace EventSourcing.Persistence.SQLite;

public class SQLiteExecutor
{
	public string ConnectionString { get; }

	public SQLiteExecutor(string connectionString) => ConnectionString = connectionString;

	public Task Execute(Func<SQLiteConnection, Task> action)
		=> Execute(action.ToFunc());

	public async Task<T> Execute<T>(Func<SQLiteConnection, Task<T>> func)
	{
		using var connection = new SQLiteConnection(ConnectionString);
		await connection.OpenAsync();
		return await func(connection);
	}
}