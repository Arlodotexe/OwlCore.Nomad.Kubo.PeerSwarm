using Newtonsoft.Json.Linq;
using OwlCore.Console.Nomad.Kubo.PeerSwarm.Models.Serialization.UpdateEvents;

namespace OwlCore.Nomad.Kubo.PeerSwarm.Serialization;

internal static partial class PeerRegistryUpdateEventSerializationHelpers
{
    internal static JObject? Write(PeerSwarmUpdateEvent @event)
    {
        var jObject = new JObject
        {
            { "TargetId", new JValue(@event.TargetId) },
            { "EventId", new JValue(@event.EventId) }
        };

        WriteCacheEvent(@event, jObject);

        return jObject;
    }

    private static void WriteCacheEvent(PeerSwarmUpdateEvent @event, JObject jObject)
    {
        switch (@event)
        {
            case PeerSwarmAddEvent addEvent:
                jObject.Add(nameof(addEvent.PeerId), new JValue(addEvent.PeerId.ToString()));
                break;
            case PeerSwarmRemoveEvent removeEvent:
                jObject.Add(nameof(removeEvent.PeerId), new JValue(removeEvent.PeerId.ToString()));
                break;
            case PeerSwarmPeerAddressAddEvent swarmPeerAddressAddEvent:
                jObject.Add(nameof(swarmPeerAddressAddEvent.Address), new JValue(swarmPeerAddressAddEvent.Address.ToString()));
                break;
            case PeerSwarmPeerAddressRemoveEvent swarmPeerAddressRemoveEvent:
                jObject.Add(nameof(swarmPeerAddressRemoveEvent.Address), new JValue(swarmPeerAddressRemoveEvent.Address.ToString()));
                break;
            case PeerSwarmTrackerAddEvent swarmPeerAddressAddEvent:
                jObject.Add(nameof(swarmPeerAddressAddEvent.PeerSwarmId), new JValue(swarmPeerAddressAddEvent.PeerSwarmId.ToString()));
                break;
            case PeerSwarmTrackerRemoveEvent swarmPeerAddressRemoveEvent:
                jObject.Add(nameof(swarmPeerAddressRemoveEvent.PeerSwarmId), new JValue(swarmPeerAddressRemoveEvent.PeerSwarmId.ToString()));
                break;
        }
    }
}
