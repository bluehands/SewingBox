using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace EventSourcing2
{
    public class EventSourcingContext(
        IEnumerable<IInitializer> initializers,
        EventStream<Event>? eventStream = null,
        ILogger<EventSourcingContext>? logger = null)
    {
        readonly IReadOnlyCollection<IInitializer> _initializers = initializers.ToImmutableArray();

        public virtual async Task Initialize()
        {
            await Initialize<SchemaInitialization>();
            if (eventStream != null)
            {
                await Initialize<BeforeEventsReplay>();
                logger?.LogInformation("Starting event stream.");
                eventStream.Start();
            }
        }

        protected virtual Task Initialize<TPhase>() where TPhase : IInitializationPhase =>
            Task.WhenAll(_initializers.OfType<IInitializer<TPhase>>().Select(async i =>
            {
                logger?.LogInformation("{InitializationPhase}: Running initializer {Initializer}", typeof(TPhase).Name, i.GetType().Name);
                await i.Initialize();
                logger?.LogInformation("{initializer} done", i.GetType().Name);
            }));
    }

    public interface IInitializer
    {
        Task Initialize();
    }

    // ReSharper disable once UnusedTypeParameter
    public interface IInitializer<TPhase> : IInitializer where TPhase : IInitializationPhase;

    public interface IInitializationPhase;

    public sealed class SchemaInitialization : IInitializationPhase;
    public sealed class BeforeEventsReplay : IInitializationPhase;
}
