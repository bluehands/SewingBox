using System.Collections.Immutable;
using System.Reflection;
using EventSourcing.Internals;

namespace EventSourcing.Events;

public abstract class EventPayloadMapper<TSerializablePayload, TEventPayload> : EventPayloadMapper
	where TEventPayload : EventPayload
{
	protected override EventPayload MapFromSerializablePayload(object eventStoreEvent, Func<Type, object, object>? deserializePayload = null)
	{
		var serializablePayload = deserializePayload?.Invoke(typeof(TSerializablePayload), eventStoreEvent) ?? eventStoreEvent;
		var fromEvent = MapFromSerializablePayload((TSerializablePayload)serializablePayload);
		return fromEvent;
	}

	protected override object InternalMapToSerializablePayload(EventPayload payload)
	{
		var outgoingEvent = (TEventPayload)payload;
		var serializableEvent = MapToSerializablePayload(outgoingEvent)!;
		return serializableEvent;
	}

	protected abstract TEventPayload MapFromSerializablePayload(TSerializablePayload serialized);

	protected abstract TSerializablePayload MapToSerializablePayload(TEventPayload payload);
}

public abstract class EventPayloadMapper
{
	static ImmutableDictionary<string, EventPayloadMapper> _mappers =
		ImmutableDictionary<string, EventPayloadMapper>.Empty;

	public static void Register(IEnumerable<Assembly> assembly)
	{
		_mappers = _mappers.SetItems(InternalRegister(assembly));
	}

	static ImmutableDictionary<string, EventPayloadMapper> InternalRegister(IEnumerable<Assembly> assemblies)
	{
		return typeof(EventPayloadMapper)
			.GetConcreteDerivedTypes(assemblies)
			.Where(t => t.GetConstructors().Any(c => c.GetParameters().Length == 0))
			.Select(t => new
			{
				t,
				att = t.GetArgumentOfFirstGenericBaseType().GetCustomAttribute<SerializedEventPayloadAttribute>()
			})
			.Where(_ =>
			{
				if (_.att == null)
					throw new ArgumentException(
						$"Type {_.t.GetArgumentOfFirstGenericBaseType().BeautifulName()} is used as payload type in PayloadMapper {_.t.BeautifulName()}. It has to be marked with {nameof(SerializedEventPayloadAttribute)}.");

				return _.att != null;
			})
			.ToImmutableDictionary(_ => _.att!.EventType, _ => (EventPayloadMapper)Activator.CreateInstance(_.t)!);
	}

	public static EventPayload MapFromSerializedPayload(string eventType, object serializedPayload,
		Func<Type, object, object>? deserializePayload = null)
	{
		if (!_mappers.TryGetValue(eventType, out var mapper))
		{
			throw new($"No payload mapper registered for event type {eventType}");
		}

		return mapper.MapFromSerializablePayload(serializedPayload, deserializePayload);
	}

	public static object MapToSerializablePayload(EventPayload payload)
	{
		var eventType = payload.EventType;
		if (!_mappers.TryGetValue(eventType, out var mapper))
		{
			throw new($"No payload mapper registered for event type {eventType}");
		}

		return mapper.InternalMapToSerializablePayload(payload);
	}

	protected abstract EventPayload MapFromSerializablePayload(object eventStoreEvent,
		Func<Type, object, object>? deserializePayload = null);

	protected abstract object InternalMapToSerializablePayload(EventPayload payload);

	public static bool IsMapperRegistered(string eventType) => _mappers.ContainsKey(eventType);
	public static int MapperCount => _mappers.Count;

	public static IEnumerable<Type> SerializablePayloadTypes()
	{
		return _mappers
			.Values
			.Select(m => m
				.GetType()
				.GetBaseType(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(EventPayloadMapper<,>))
				.GetGenericArguments()[0]);
	}
}