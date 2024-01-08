using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventSourcing2;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.Persistence.EntityFramework;

public class EventStoreContext(DbContextOptions<EventStoreContext> contextOptions) : DbContext(contextOptions)
{
    public DbSet<Event> Events { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    }
}

[Index(nameof(StreamType), nameof(StreamId))]
public record Event(
    [property: Key]
    [property: DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    long Position,
    string StreamType,
    string StreamId,
    string EventType,
    string Payload,
    DateTimeOffset Timestamp
);

class EventStore(EventStoreContext eventStore) : IEventReader<Event>, IEventWriter<Event>
{
    public IAsyncEnumerable<Event> ReadEvents(StreamId streamId) => eventStore.Events
        .Where(e => e.StreamType == streamId.StreamType && e.StreamId == streamId.Id)
        .AsAsyncEnumerable();

    public IAsyncEnumerable<Event> ReadEvents(long fromPositionInclusive) => eventStore.Events
        .Where(e => e.Position >= fromPositionInclusive)
        .AsAsyncEnumerable();

    public async Task WriteEvents(IEnumerable<Event> payloads)
    {
        await eventStore.Events.AddRangeAsync(payloads);
        await eventStore.SaveChangesAsync();
    }
}