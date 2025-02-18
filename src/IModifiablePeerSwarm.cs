using OwlCore.Console.Nomad.Kubo;
using OwlCore.Console.Nomad.Kubo.PeerSwarm;
using OwlCore.Nomad.Kubo;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm that can be modified.
/// </summary>
public interface IModifiablePeerSwarm : IReadOnlyPeerSwarm, IModifiableNomadKuboRegistry<IReadOnlyPeer>, INomadKuboRepository<ModifiablePeer, IReadOnlyPeer>
{
}