using Ipfs;
using Newtonsoft.Json;

namespace OwlCore.Console.Nomad.Kubo.PeerSwarm.Models.Serialization.UpdateEvents;

[JsonConverter(typeof(PeerSwarmUpdateEventJsonConverter))]
public abstract record PeerSwarmUpdateEvent(string TargetId, string EventId);

public record PeerSwarmAddEvent(string TargetId, Cid PeerId) : PeerSwarmUpdateEvent(TargetId, nameof(PeerSwarmAddEvent));

public record PeerSwarmRemoveEvent(string TargetId, Cid PeerId) : PeerSwarmUpdateEvent(TargetId, nameof(PeerSwarmRemoveEvent));

public record PeerSwarmPeerAddressAddEvent(string TargetId, MultiAddress Address) : PeerSwarmUpdateEvent(TargetId, nameof(PeerSwarmPeerAddressAddEvent));

public record PeerSwarmPeerAddressRemoveEvent(string TargetId, MultiAddress Address) : PeerSwarmUpdateEvent(TargetId, nameof(PeerSwarmPeerAddressRemoveEvent));

public abstract record PeerSwarmTrackerUpdateEvent(string TargetId, string EventId) : PeerSwarmUpdateEvent(TargetId, EventId);

public record PeerSwarmTrackerAddEvent(string TargetId, Cid PeerSwarmId) : PeerSwarmTrackerUpdateEvent(TargetId, nameof(PeerSwarmTrackerAddEvent));

public record PeerSwarmTrackerRemoveEvent(string TargetId, Cid PeerSwarmId) : PeerSwarmTrackerUpdateEvent(TargetId, nameof(PeerSwarmTrackerRemoveEvent));
