using System;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace EventSourcing.Persistence.SQLite;

public static class SchemaCreation
{
	public static void AssertEventSchema(this SQLiteExecutor executor)
	{
		AssertDbFile(executor.ConnectionString);
		executor.Execute(CreateEventTable);
	}

	static void AssertDbFile(string connectionString)
	{
		var fileNameOrInMemory = GetDataSourceFromConnectionString(connectionString);
		var isInMemory = string.Equals(fileNameOrInMemory, ":memory:", StringComparison.OrdinalIgnoreCase);
		if (!isInMemory && !File.Exists(fileNameOrInMemory))
		{
			SQLiteConnection.CreateFile(fileNameOrInMemory);
		}
	}

	static string GetDataSourceFromConnectionString(string connectionString)
	{
		var connectionStringBuilder = new DbConnectionStringBuilder
		{
			ConnectionString = connectionString
		};
		var fileNameOrInMemory = (string)connectionStringBuilder["datasource"];
		return fileNameOrInMemory;
	}

	public static async Task CreateEventTable(this SQLiteConnection dbConnection)
	{
		const string tableName = "events";
		var tableExists = await dbConnection.CheckTableExists(tableName);
		if (tableExists)
			return;

		await dbConnection.ExecuteNonQuery(@"create table events (streamtype text, streamid text, type text, payload text, timestamp timestamp default (strftime('%Y-%m-%d %H:%M:%f', 'now')))");
		await dbConnection.ExecuteNonQuery(@"create index idx_streamtype_streamid on events (streamtype, streamid);");
	}
}