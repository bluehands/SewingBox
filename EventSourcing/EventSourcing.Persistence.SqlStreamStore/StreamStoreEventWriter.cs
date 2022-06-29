using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Events;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.Logging;
using MoreLinq;
using SqlStreamStore;
using SqlStreamStore.Streams;

namespace EventSourcing.Persistence.SqlStreamStore;

class StreamStoreEventWriter : IEventWriter
{
	readonly IStreamStore _eventStore;
	readonly ILogger<StreamStoreEventWriter> _logger;
	readonly IEventSerializer<string> _serializer;

	public StreamStoreEventWriter(IStreamStore eventStore, ILogger<StreamStoreEventWriter> logger, IEventSerializer<string> serializer)
	{
		_eventStore = eventStore;
		_logger = logger;
		_serializer = serializer;
	}

	public async Task WriteEvents(IReadOnlyCollection<EventPayload> payloads) =>
		await payloads
			.GroupAdjacent(p => p.StreamId)
			.SelectAsyncSequential(async p =>
			{
				var streamId = p.Key;
				var messages = Enumerable.Select<EventPayload, NewStreamMessage>(p, StreamMessageFromPayload).ToArray();

				var results = await _eventStore.AppendToStream(
					IdTranslation.ToStreamStoreStreamId(streamId), ExpectedVersion.Any, messages);
				if (_logger.IsEnabled(LogLevel.Debug))
					_logger.LogDebug($"Added {messages.Length} events to database");
				return results;

			});

	NewStreamMessage StreamMessageFromPayload(EventPayload arg)
	{
		var serializablePayload = EventPayloadMapper.MapToSerializablePayload(arg);
		return new(Guid.NewGuid(), arg.EventType, _serializer.Serialize(serializablePayload));
	}
}