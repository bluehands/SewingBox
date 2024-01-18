using EventSourcing.Persistence.EntityFramework.Internal;
using EventSourcing.Persistence.EntityFramework.SqlServer.Internal;
using EventSourcing2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Persistence.EntityFramework.SqlServer;

public static class ServiceRegistration
{
    public static EventSourcingOptionsBuilder UseSqlServerEventStore(this EventSourcingOptionsBuilder optionsBuilder, string connectionString, Action<SqlServerEventStoreOptionsBuilder>? configure = null)
    {
        var sqlBuilder = new SqlServerEventStoreOptionsBuilder(optionsBuilder);
        sqlBuilder.UseConnectionString(connectionString);
        configure?.Invoke(sqlBuilder);

        SetDefaultEventStreamIfNotConfigured(optionsBuilder, sqlBuilder);
        return optionsBuilder;
    }

    static void SetDefaultEventStreamIfNotConfigured(EventSourcingOptionsBuilder optionsBuilder,
        SqlServerEventStoreOptionsBuilder sqlBuilder)
    {
        if (optionsBuilder.Options.FindExtension<PollingEventStreamOptionsExtension>() == null
            && optionsBuilder.Options.FindExtension<SqlDependencyEventStreamOptionsExtension>() == null)
        {
            sqlBuilder.UseBrokerNotificationEventStream();
        }
    }
}

public class SqlServerEventStoreOptionsBuilder : IEventSourcingExtensionsBuilderInfrastructure, IAllowPollingEventStreamBuilder
{
    readonly EventSourcingOptionsBuilder _optionsBuilder;
    EventSourcingOptionsBuilder IEventSourcingExtensionsBuilderInfrastructure.OptionsBuilder => _optionsBuilder;

    public SqlServerEventStoreOptionsBuilder(EventSourcingOptionsBuilder optionsBuilder) => _optionsBuilder = optionsBuilder;

    public SqlServerEventStoreOptionsBuilder UseConnectionString(string connectionString) =>
        WithOption<SqlServerEventStoreOptionsExtension>(options => options with
        {
            ConnectionString = connectionString
        });

    public SqlServerEventStoreOptionsBuilder UseBrokerNotificationEventStream(uint maxRowsPerSelect = 10000) =>
        WithOption<SqlDependencyEventStreamOptionsExtension>(e => e with
        {
            MaxRowsPerSelect = maxRowsPerSelect
        });

    SqlServerEventStoreOptionsBuilder WithOption<TExtension>(Func<TExtension, TExtension> modify) where TExtension : class, IEventSourcingOptionsExtension, new()
    {
        var options = modify(_optionsBuilder.GetOrCreateExtension<TExtension>());
        ((IEventSourcingBuilderInfrastructure)_optionsBuilder).AddOrUpdateExtension(options);
        return this;
    }
}

public record SqlDependencyEventStreamOptionsExtension(uint? MaxRowsPerSelect) : IEventSourcingOptionsExtension
{
    public SqlDependencyEventStreamOptionsExtension() : this(default(uint?))
    {
    }

    public void ApplyServices(IServiceCollection serviceCollection)
    {
        BrokerNotificationEventStream.AddEventStream(serviceCollection, MaxRowsPerSelect ?? 10000);
        serviceCollection.AddSingleton<IObservable<EventSourcing2.Event>>(sp => sp.GetRequiredService<EventStream<EventSourcing2.Event>>());
    }
}

public record SqlServerEventStoreOptionsExtension(string? ConnectionString) : IEventSourcingOptionsExtension
{
    public SqlServerEventStoreOptionsExtension() : this(default(string))
    {
    }

    public void ApplyServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddDbContext<EventStoreContext>(options =>
            options.UseSqlServer(ConnectionString, o => o.MigrationsAssembly(typeof(Marker).Assembly.GetName().Name!))
        );

        serviceCollection.AddEntityFrameworkServices();
    }
}