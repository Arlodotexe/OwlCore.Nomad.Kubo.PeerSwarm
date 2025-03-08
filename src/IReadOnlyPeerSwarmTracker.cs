using OwlCore.ComponentModel;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm tracker that can be read but not modified.
/// </summary>
public interface IReadOnlyPeerSwarmTracker : IReadOnlyNomadKuboRegistry<IReadOnlyPeerSwarm>, IHasId
{
}
