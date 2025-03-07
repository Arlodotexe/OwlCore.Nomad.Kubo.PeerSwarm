using OwlCore.ComponentModel;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm that can be read but not modified.
/// </summary>
public interface IReadOnlyPeerSwarm : IReadOnlyNomadKuboRegistry<IReadOnlyPeer>, IHasId
{
}
