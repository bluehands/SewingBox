using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using EventSourcing2.Internals;

namespace EventSourcing2.Events;

public static class EventFactory
{
	delegate Event CreateEvent(long version, DateTimeOffset timestamp, EventPayload payload);

	public static void Initialize(IEnumerable<Assembly> payloadAssemblies) => _eventFactoryByPayloadType = typeof(EventPayload)
		.GetConcreteDerivedTypes(payloadAssemblies)
		.Select(payloadType =>
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
			return (payloadType, factory: expression.Compile());
		}).ToImmutableDictionary(t => t.payloadType, t => t.factory);

	static ImmutableDictionary<Type, CreateEvent> _eventFactoryByPayloadType = ImmutableDictionary<Type, CreateEvent>.Empty;

	public static Event EventFromPayload(EventPayload eventPayload, long version, DateTimeOffset timestamp) =>
		_eventFactoryByPayloadType[eventPayload.GetType()](version, timestamp, eventPayload);
}