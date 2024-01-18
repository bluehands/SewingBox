using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EventSourcing2.Internal;

namespace EventSourcing2.Events;

public static class EventFactory
{
	delegate Event CreateEvent(long version, DateTimeOffset timestamp, EventPayload payload);

	public static void Initialize(IEnumerable<Assembly> payloadAssemblies) => _eventFactoryByPayloadType = typeof(EventPayload)
		.GetConcreteDerivedTypes(payloadAssemblies.Distinct())
		.Select(payloadType =>
        {
            var createEvent = BuildCreateEvent(payloadType);
            var serializableEventPayloadAttribute = payloadType.GetCustomAttribute<SerializableEventPayloadAttribute>();
            if (serializableEventPayloadAttribute != null)
            {
                EventPayloadMapper.AddIdentityMapper(serializableEventPayloadAttribute.EventType, payloadType);
            }

            return (payloadType, factory: createEvent);
        }).ToImmutableDictionary(t => t.payloadType, t => t.factory);

    static CreateEvent BuildCreateEvent(Type payloadType)
    {
        var eventType = typeof(Event<>).MakeGenericType(payloadType);
        var versionParam = Expression.Parameter(typeof(long));
        var timestampParam = Expression.Parameter(typeof(DateTimeOffset));
        var payloadParam = Expression.Parameter(typeof(EventPayload));

        var expression = Expression.Lambda<CreateEvent>(
            Expression.New(eventType.GetConstructors().Single(),
                versionParam,
                timestampParam,
                Expression.Convert(payloadParam, payloadType)),
            versionParam,
            timestampParam,
            payloadParam);
        var createEvent = expression.Compile();
        return createEvent;
    }

    static ImmutableDictionary<Type, CreateEvent> _eventFactoryByPayloadType = ImmutableDictionary<Type, CreateEvent>.Empty;

	public static Event EventFromPayload(EventPayload eventPayload, long version, DateTimeOffset timestamp)
    {
        var payloadType = eventPayload.GetType();
        if (_eventFactoryByPayloadType.TryGetValue(payloadType, out var createEvent))
            return createEvent(version, timestamp, eventPayload);

        createEvent = BuildCreateEvent(payloadType);
        _eventFactoryByPayloadType = _eventFactoryByPayloadType.SetItem(payloadType, createEvent);
        return createEvent(version, timestamp, eventPayload);
    }
}