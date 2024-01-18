using EventSourcing.Persistence.EntityFramework.Sqlite.Internal;
using EventSourcing2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Persistence.EntityFramework.Sqlite;

public static class ServiceRegistration
{
    public static EventSourcingOptionsBuilder UseSqliteEventStore(this EventSourcingOptionsBuilder optionsBuilder, string connectionString, Action<SqliteEventStoreOptionsBuilder>? configure = null)
    {
        var sqlBuilder = new SqliteEventStoreOptionsBuilder(optionsBuilder);
        sqlBuilder.UseConnectionString(connectionString);
        configure?.Invoke(sqlBuilder);

        SetDefaultEventStreamIfNotConfigured(optionsBuilder, sqlBuilder);
        return optionsBuilder;
    }

    static void SetDefaultEventStreamIfNotConfigured(EventSourcingOptionsBuilder optionsBuilder, SqliteEventStoreOptionsBuilder sqlBuilder)
    {
        if (optionsBuilder.Options.FindExtension<PollingEventStreamOptionsExtension>() == null)
        {
            sqlBuilder.UsePollingEventStream();
        }
    }
}

public class SqliteEventStoreOptionsBuilder(EventSourcingOptionsBuilder optionsBuilder)
    : IEventSourcingExtensionsBuilderInfrastructure, IAllowPollingEventStreamBuilder
{
    EventSourcingOptionsBuilder IEventSourcingExtensionsBuilderInfrastructure.OptionsBuilder => optionsBuilder;

    public SqliteEventStoreOptionsBuilder UseConnectionString(string connectionString) =>
        WithOption<SqliteEventStoreOptionsExtension>(options => options with
        {
            ConnectionString = connectionString
        });

    SqliteEventStoreOptionsBuilder WithOption<TExtension>(Func<TExtension, TExtension> modify) where TExtension : class, IEventSourcingOptionsExtension, new()
    {
        var options = modify(optionsBuilder.GetOrCreateExtension<TExtension>());
        ((IEventSourcingBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(options);
        return this;
    }
}

public record SqliteEventStoreOptionsExtension(string? ConnectionString) : IEventSourcingOptionsExtension
{
    public SqliteEventStoreOptionsExtension() : this(default(string))
    {
    }

    public void ApplyServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContext<EventStoreContext>(options =>
            options.UseSqlite(ConnectionString, o => o.MigrationsAssembly(typeof(Marker).Assembly.GetName().Name!))
        );

        serviceCollection.AddEntityFrameworkServices();
    }
}