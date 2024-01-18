using EventSourcing2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Persistence.EntityFramework.SqlServer
{
    public static class ServiceRegistration
    {
        public static EventSourcingOptionsBuilder UseSqlServerEventStore(this EventSourcingOptionsBuilder optionsBuilder, string connectionString, Action<SqlServerEventStoreOptionsBuilder>? configure = null)
        {
            var sqlBuilder = new SqlServerEventStoreOptionsBuilder(optionsBuilder);
            sqlBuilder.UseConnectionString(connectionString);
            configure?.Invoke(sqlBuilder);
            return optionsBuilder;
        }
    }

    public class SqlServerEventStoreOptionsBuilder : IEventSourcingExtensionsBuilderInfrastructure
    {
        readonly EventSourcingOptionsBuilder _optionsBuilder;
        EventSourcingOptionsBuilder IEventSourcingExtensionsBuilderInfrastructure.OptionsBuilder => _optionsBuilder;

        public SqlServerEventStoreOptionsBuilder(EventSourcingOptionsBuilder optionsBuilder) => _optionsBuilder = optionsBuilder;

        public SqlServerEventStoreOptionsBuilder UseConnectionString(string connectionString) =>
            WithOption(options => options with
            {
                ConnectionString = connectionString
            });

        SqlServerEventStoreOptionsBuilder WithOption(Func<SqlServerEventStoreOptionsExtension, SqlServerEventStoreOptionsExtension> modify)
        {
            var options = modify(_optionsBuilder.GetOrCreateExtension());
            ((IEventSourcingBuilderInfrastructure)_optionsBuilder).AddOrUpdateExtension(options);
            return this;
        }
    }

    static class OptionsBuilderExtension
    {
        public static SqlServerEventStoreOptionsExtension GetOrCreateExtension(this EventSourcingOptionsBuilder optionsBuilder) =>
            optionsBuilder.Options.FindExtension<SqlServerEventStoreOptionsExtension>() ??
            new SqlServerEventStoreOptionsExtension();
    }

    public record SqlServerEventStoreOptionsExtension(string? ConnectionString) : IEventSourcingOptionsExtension
    {
        public SqlServerEventStoreOptionsExtension() : this(default(string))
        {
        }

        public void ApplyServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<EventStoreContext>(options =>
                options.UseSqlServer(ConnectionString,
                    o => o.MigrationsAssembly(typeof(Marker).Assembly.GetName().Name!))
            );

            serviceCollection.AddEntityFrameworkServices();
        }
    }
}
