using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Persistence.EntityFramework.Sqlite;

public static class ServiceRegistration
{
    //public static IServiceCollection AddSqliteEventStore(this IServiceCollection serviceCollection, string connectionString) =>
    //    serviceCollection.AddEntityFrameworkEventStore(new EventStoreOptions(),
    //        options =>
    //        {
    //            options.UseSqlite(connectionString, x => x.MigrationsAssembly(typeof(Marker).Assembly.GetName().Name!));
    //        });
}

