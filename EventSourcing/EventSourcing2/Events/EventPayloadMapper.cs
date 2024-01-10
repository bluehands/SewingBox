using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using EventSourcing2.Internals;

namespace EventSourcing2.Events;

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

    internal static void AddIdentityMapper(string eventType, Type payloadType)
    {
        _mappers = _mappers.SetItem(eventType, (EventPayloadMapper)Activator.CreateInstance(typeof(IdentityMapper<>).MakeGenericType(payloadType)));
    }

	static ImmutableDictionary<string, EventPayloadMapper> InternalRegister(IEnumerable<Assembly> assemblies)
	{
		return typeof(EventPayloadMapper)
			.GetConcreteDerivedTypes(assemblies)
			.Where(t => t.GetConstructors().Any(c => c.GetParameters().Length == 0))
			.Select(t => new
			{
				t,
				att = t.GetArgumentOfFirstGenericBaseType().GetCustomAttribute<SerializableEventPayloadAttribute>()
			})
			.Where(t =>
			{
				if (t.att == null)
					throw new ArgumentException(
						$"Type {t.t.GetArgumentOfFirstGenericBaseType().BeautifulName()} is used as payload type in PayloadMapper {t.t.BeautifulName()}. It has to be marked with {nameof(SerializableEventPayloadAttribute)}.");

				return t.att != null;
			})
			.ToImmutableDictionary(t => t.att!.EventType, t => (EventPayloadMapper)Activator.CreateInstance(t.t)!);
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

sealed class IdentityMapper<T> : EventPayloadMapper<T, T> where T : EventPayload
{
    protected override T MapFromSerializablePayload(T serialized) => serialized;

    protected override T MapToSerializablePayload(T payload) => payload;
}