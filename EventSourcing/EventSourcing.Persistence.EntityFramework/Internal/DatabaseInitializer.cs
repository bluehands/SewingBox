using EventSourcing2;
using Microsoft.EntityFrameworkCore;

namespace EventSourcing.Persistence.EntityFramework.Internal;

public class DatabaseInitializer(EventStoreContext eventStoreContext) : IInitializer<SchemaInitialization>
{
    public async Task Initialize()
    {
        await eventStoreContext.Database.MigrateAsync();
    }
}