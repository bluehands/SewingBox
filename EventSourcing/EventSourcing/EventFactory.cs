using EventSourcing.Internals;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;

namespace EventSourcing;

public static class EventFactory
{
	delegate Event CreateEvent(long version, DateTimeOffset timestamp, bool isFirstOfStream, EventPayload payload);

	public static void Initialize(IEnumerable<Assembly> payloadAssemblies) => _eventFactoryByPayloadType = typeof(EventPayload)
		.GetConcreteDerivedTypes(payloadAssemblies)
		.Select(payloadType =>
		{
			var eventType = typeof(Event<>).MakeGenericType(payloadType);
			var versionParam = Expression.Parameter(typeof(long));
			var timestampParam = Expression.Parameter(typeof(DateTimeOffset));
			var isFirstOfStreamParam = Expression.Parameter(typeof(bool));
			var payloadParam = Expression.Parameter(typeof(EventPayload));

			var expression = Expression.Lambda<CreateEvent>(
				Expression.New(eventType.GetConstructors().Single(),
					versionParam,
					timestampParam,
					isFirstOfStreamParam,
					Expression.Convert(payloadParam, payloadType)), 
				versionParam, 
				timestampParam, 
				isFirstOfStreamParam, 
				payloadParam);
			return (payloadType, factory: expression.Compile());
		}).ToImmutableDictionary(t => t.payloadType, t => t.factory);

	static ImmutableDictionary<Type, CreateEvent> _eventFactoryByPayloadType = ImmutableDictionary<Type, CreateEvent>.Empty;

	public static Event EventFromPayload(EventPayload eventPayload, long version, DateTimeOffset timestamp, bool isFirstOfStreamHint) =>
		_eventFactoryByPayloadType[eventPayload.GetType()](version, timestamp, isFirstOfStreamHint, eventPayload);
}