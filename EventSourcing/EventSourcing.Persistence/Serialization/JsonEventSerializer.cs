using System;

namespace EventSourcing.Persistence.Serialization;

public class JsonEventSerializer : IEventSerializer<string>
{
    public string Serialize(object serializablePayload) => System.Text.Json.JsonSerializer.Serialize(serializablePayload);

    public object Deserialize(Type serializablePayloadType, string serializedPayload) => System.Text.Json.JsonSerializer.Deserialize(serializedPayload, serializablePayloadType)!;
}