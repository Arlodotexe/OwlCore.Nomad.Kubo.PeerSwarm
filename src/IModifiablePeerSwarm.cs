namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm that can be modified.
/// </summary>
public interface IModifiablePeerSwarm : IReadOnlyPeerSwarm, IModifiableNomadKuboRegistry<IReadOnlyPeer>, INomadKuboRepository<ModifiablePeer, IReadOnlyPeer>
{
}