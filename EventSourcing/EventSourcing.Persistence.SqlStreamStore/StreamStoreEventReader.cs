using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing.Events;
using FunicularSwitch.Extensions;
using SqlStreamStore;
using SqlStreamStore.Streams;

namespace EventSourcing.Persistence.SqlStreamStore;

class StreamStoreEventReader : IEventReader
{
	static readonly int s_ReadPageSize = 1000;
	readonly IStreamStore _eventStore;
	readonly IEventSerializer<string> _eventSerializer;

	public StreamStoreEventReader(IStreamStore eventStore, IEventSerializer<string> eventSerializer)
	{
		_eventStore = eventStore;
		_eventSerializer = eventSerializer;
	}

	public async Task<IEnumerable<Event>> ReadEvents(Events.StreamId streamId, long upToVersionExclusive)
	{
		var allForStream = await _eventStore
			.ReadStreamForwards(IdTranslation.ToStreamStoreStreamId(streamId), StreamVersion.Start, int.MaxValue)
			.ConfigureAwait(false);

		return await allForStream.Messages
			.Where(e => e.Position < upToVersionExclusive)
			.SelectAsync(ToEvent)
			.ConfigureAwait(false);
	}

	public Task<IEnumerable<Event>> ReadEvents() => throw new NotImplementedException();

	public async Task<IReadOnlyList<Event>> ReadEvents(long fromPositionInclusive)
	{
		var events = Enumerable.Empty<StreamMessage>();
		var currentPosition = fromPositionInclusive;
		while (true)
		{
			var page = await _eventStore.ReadAllForwards(currentPosition, s_ReadPageSize);
			currentPosition = page.NextPosition;
			events = events.Concat(page.Messages);
			if (page.IsEnd)
				break;
		}

		var result = await events.SelectAsync(ToEvent);
		return result;
	}

	public async Task<Event> ToEvent(StreamMessage m)
	{
		var jsonData = await m.GetJsonData();
		var payload = EventPayloadMapper.MapFromSerializedPayload(m.Type, jsonData, (type, o) =>  _eventSerializer.Deserialize(type, (string)o));
		return EventFactory.EventFromPayload(payload, m.Position, new(m.CreatedUtc, TimeSpan.Zero), m.StreamVersion == StreamVersion.Start);
	}
}