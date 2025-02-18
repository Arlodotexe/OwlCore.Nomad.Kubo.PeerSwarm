using CommunityToolkit.Diagnostics;
using Ipfs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OwlCore.Console.Nomad.Kubo.PeerSwarm.Models.Serialization.UpdateEvents;
using OwlCore.Extensions;

namespace OwlCore.Nomad.Kubo.PeerSwarm.Serialization;

internal static partial class PeerRegistryUpdateEventSerializationHelpers
{
    internal static object? Read(JToken token, JsonSerializer serializer)
    {
        if (token.Type == JTokenType.Array)
        {
            var jsonArray = (JArray)token;
            return jsonArray.Select(jToken => Read(jToken, serializer)).PruneNull().FirstOrDefault();
        }

        if (token is JObject jObject)
            return Read(jObject, serializer);

        throw new NotSupportedException($"Token type {token.Type} is not supported.");
    }


    internal static PeerSwarmUpdateEvent? Read(JObject jObject, JsonSerializer serializer)
    {
        var id = jObject["TargetId"]?.Value<string>();
        var eventId = jObject["EventId"]?.Value<string>();

        // Basic validation
        Guard.IsNotNullOrWhiteSpace(id);
        Guard.IsNotNullOrWhiteSpace(eventId);

        return ReadCacheEvent(jObject, eventId, id, serializer);
    }

    internal static PeerSwarmUpdateEvent? ReadCacheEvent(JObject jObject, string eventId, string id, JsonSerializer serializer)
    {
        return eventId switch
        {
            nameof(PeerSwarmAddEvent) when
                jObject[nameof(PeerSwarmAddEvent.PeerId)] is { } token && token.ToObject<Cid>(serializer) is { } value =>
                new PeerSwarmAddEvent(id, value),

            nameof(PeerSwarmRemoveEvent) when
                jObject[nameof(PeerSwarmRemoveEvent.PeerId)] is { } token && token.ToObject<Cid>(serializer) is { } value =>
                new PeerSwarmRemoveEvent(id, value),

            nameof(PeerSwarmPeerAddressAddEvent) when
                jObject[nameof(PeerSwarmPeerAddressAddEvent.Address)] is { } addressToken && addressToken.ToObject<MultiAddress>(serializer) is { } address =>
                new PeerSwarmPeerAddressAddEvent(id, address),

            nameof(PeerSwarmPeerAddressRemoveEvent) when
                jObject[nameof(PeerSwarmPeerAddressRemoveEvent.Address)] is { } addressToken && addressToken.ToObject<MultiAddress>(serializer) is { } address =>
                new PeerSwarmPeerAddressRemoveEvent(id, address),

            nameof(PeerSwarmTrackerAddEvent) when
                jObject[nameof(PeerSwarmTrackerAddEvent.PeerSwarmId)] is { } addressToken && addressToken.ToObject<Cid>(serializer) is { } address =>
                new PeerSwarmTrackerAddEvent(id, address),

            nameof(PeerSwarmTrackerRemoveEvent) when
                jObject[nameof(PeerSwarmTrackerRemoveEvent.PeerSwarmId)] is { } addressToken && addressToken.ToObject<Cid>(serializer) is { } address =>
                new PeerSwarmTrackerRemoveEvent(id, address),

            _ => throw new InvalidOperationException($"Unhandled or missing event type: {eventId}")
        };
    }
}