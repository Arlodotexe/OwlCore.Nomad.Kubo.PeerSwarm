﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OwlCore.Nomad.Kubo.PeerSwarm.Serialization;
using OwlCore.Console.Nomad.Kubo.PeerSwarm.Models.Serialization.UpdateEvents;

namespace OwlCore.Console.Nomad.Kubo.PeerSwarm.Models.Serialization;

/// <summary>
/// A custom json convert for discriminating the various types of <see cref="PeerSwarmUpdateEvent"/>.
/// </summary>
public class PeerSwarmUpdateEventJsonConverter : JsonConverter
{
    /// <inheritdoc />
    public override bool CanConvert(Type objectType)
    {
        if (objectType == typeof(PeerSwarmUpdateEvent))
            return true;

        var arrayElement = objectType.GetElementType();
        if (objectType.IsArray && arrayElement == typeof(PeerSwarmUpdateEvent))
            return true;

        return false;
    }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanWrite => true;

    /// <inheritdoc/>
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (reader.TokenType == JsonToken.PropertyName)
        {
            return reader.Value;
        }

        if (reader.TokenType == JsonToken.StartObject)
        {
            var jobj = JObject.Load(reader);
            return PeerRegistryUpdateEventSerializationHelpers.Read(jobj, serializer);
        }

        if (reader.TokenType == JsonToken.StartArray)
        {
            var jarray = JArray.Load(reader);
            return PeerRegistryUpdateEventSerializationHelpers.Read(jarray, serializer);
        }

        throw new NotSupportedException($"Token type {reader.TokenType} is not supported.");
    }

    /// <inheritdoc/>
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is null)
            return;

        if (value is PeerSwarmUpdateEvent connection)
        {
            var jObject = PeerRegistryUpdateEventSerializationHelpers.Write(connection);
            jObject?.WriteTo(writer);
        }
        else if (value is PeerSwarmUpdateEvent[] connections)
        {
            var jArray = new JArray();
            foreach (var item in connections)
            {
                var jObject = PeerRegistryUpdateEventSerializationHelpers.Write(item);

                if (jObject is null)
                    jArray.Add(JValue.CreateNull());
                else
                    jArray.Add(jObject);
            }

            jArray.WriteTo(writer);
        }
        else
        {
            throw new NotSupportedException($"Value type {value.GetType()} is not supported.");
        }
    }
}