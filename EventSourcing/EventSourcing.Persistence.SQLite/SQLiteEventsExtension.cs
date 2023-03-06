using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Events;
using FunicularSwitch.Extensions;

namespace EventSourcing.Persistence.SQLite;

public class SQLiteEventStore : IEventReader, IEventWriter
{
	readonly SQLiteExecutor _executor;
	readonly IEventSerializer<string> _eventSerializer;

	public SQLiteEventStore(SQLiteExecutor executor, IEventSerializer<string> eventSerializer)
	{
		_executor = executor;
		_eventSerializer = eventSerializer;
	}

	public Task<IEnumerable<Event>> ReadEvents(StreamId streamId, long upToPositionExclusive) => _executor.Execute(async con =>
	{
		var eventsFromDb = con.ReadEvents(streamId.StreamType, streamId.Id, upToPositionExclusive);
		return (IEnumerable<Event>) await eventsFromDb.Select(Map).ToListAsync();

	});

	Event Map(SQLiteEventsExtension.Event @event)
	{
		var payload = EventPayloadMapper.MapFromSerializedPayload(@event.EventType, @event.Payload,
			(type, o) => _eventSerializer.Deserialize(type, (string)o));
		return EventFactory.EventFromPayload(payload, @event.Version, new(@event.Timestamp, TimeSpan.Zero), false);
	}

	public async Task<IReadOnlyList<Event>> ReadEvents(long fromPositionInclusive) => await _executor.Execute(async con => await con.ReadEvents(fromPositionInclusive).Select(Map).ToListAsync());

	public Task WriteEvents(IReadOnlyCollection<EventPayload> payloads) => _executor.Execute(con =>
		con.WriteEvents(payloads
			.Select(p => new SQLiteEventsExtension.EventToInsert(
				p.StreamId.StreamType,
				p.StreamId.Id,
				p.EventType,
				_eventSerializer.Serialize(EventPayloadMapper.MapToSerializablePayload(p)))))
	);
}

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

static class SQLiteEventsExtension
{
	public static async Task WriteEvents(this SQLiteConnection dbConnection, IEnumerable<EventToInsert> events)
	{
		using var transaction = dbConnection.BeginTransaction();

		foreach (var eventToInsert in events)
		{
			const string insertSql = $@"insert into events (streamtype, streamid, type, payload) values ($streamtype, $streamid, $type, $payload)";
			using var insertCommand = new SQLiteCommand(insertSql, dbConnection);
			insertCommand.Parameters.AddWithValue("$streamtype", eventToInsert.StreamType);
			insertCommand.Parameters.AddWithValue("$streamid", eventToInsert.StreamId);
			insertCommand.Parameters.AddWithValue("$type", eventToInsert.EventType);
			insertCommand.Parameters.AddWithValue("$payload", eventToInsert.Payload);
			await insertCommand.ExecuteNonQueryAsync();
		}
		transaction.Commit();
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

	public static IAsyncEnumerable<Event> ReadEvents(this SQLiteConnection connection, long fromPositionInclusive)
	{
		const string sql = "select rowid, streamtype, streamid, type, payload, timestamp from events where rowid >= $fromPositionInclusive order by rowid asc";
		using var query = new SQLiteCommand(sql, connection);
		query.Parameters.AddWithValue("$fromPositionInclusive", fromPositionInclusive);
		return ReadRows(query);
	}

	public static IAsyncEnumerable<Event> ReadEvents(this SQLiteConnection connection, string streamType, string streamId, long upToPositionExclusive)
	{
		const string sql = "select rowid, streamtype, streamid, type, payload, timestamp from events where streamType == $streamType and streamId == $streamId and rowid < $upToVersionExclusive order by rowid asc";
		using var query = new SQLiteCommand(sql, connection);
		query.Parameters.AddWithValue("$upToVersionExclusive", upToPositionExclusive);
		query.Parameters.AddWithValue("$streamType", streamType);
		query.Parameters.AddWithValue("$streamId", streamId);
		return ReadRows(query);
	}

	static async IAsyncEnumerable<Event> ReadRows(SQLiteCommand query)
	{
		var reader = await query.ExecuteReaderAsync();
		var rowId = reader.GetOrdinal("rowid");
		var streamType = reader.GetOrdinal("streamtype");
		var streamId = reader.GetOrdinal("streamid");
		var type = reader.GetOrdinal("type");
		var payload = reader.GetOrdinal("payload");
		var timestamp = reader.GetOrdinal("timestamp");
		while (await reader.ReadAsync())
			yield return new(reader.GetInt64(rowId), reader.GetString(streamType), reader.GetString(streamId),
				reader.GetString(type), reader.GetString(payload), reader.GetDateTime(timestamp));
	}


	public record Event(long Version, string StreamType, string StreamId, string EventType, string Payload, DateTime Timestamp);
	public record EventToInsert(string StreamType, string StreamId, string EventType, string Payload);
}

public static class SQLiteConnectionExtension
{
	public static async Task ExecuteNonQuery(this SQLiteConnection dbConnection, string sql)
	{
		using var command = new SQLiteCommand(sql, dbConnection);
		await command.ExecuteNonQueryAsync();
	}

	public static async Task<bool> CheckTableExists(this SQLiteConnection dbConnection, string tableName)
	{
		using var checkCommand = new SQLiteCommand($"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';",
			dbConnection);
		using var reader = await checkCommand.ExecuteReaderAsync();
		var tableExists = reader.HasRows;
		return tableExists;
	}
}