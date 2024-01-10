using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Persistence.EntityFramework.SqlServer
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddSqlServerEventStore(this IServiceCollection serviceCollection, string connectionString) =>
            serviceCollection.AddEntityFrameworkEventStore(new(),
                options =>
                {
                    options.UseSqlServer(connectionString, x => x.MigrationsAssembly(typeof(Marker).Assembly.GetName().Name!));
                }, new StoreRegistration());
    }
}
