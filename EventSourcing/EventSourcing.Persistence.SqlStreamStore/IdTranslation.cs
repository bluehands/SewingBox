using SqlStreamStore.Streams;

namespace EventSourcing.Persistence.SqlStreamStore;

static class IdTranslation
{
	public static StreamId ToStreamStoreStreamId(Events.StreamId streamId) => $"{streamId.StreamType}_{streamId.Id}";
}