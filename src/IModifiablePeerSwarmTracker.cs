using OwlCore.Console.Nomad.Kubo;
using OwlCore.Console.Nomad.Kubo.PeerSwarm;
using OwlCore.Nomad.Kubo;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm tracker that can be modified.
/// </summary>
public interface IModifiablePeerSwarmTracker : IReadOnlyPeerSwarmTracker, IModifiableNomadKuboRegistry<IReadOnlyPeerSwarm>, INomadKuboRepository<ModifiablePeerSwarm, IReadOnlyPeerSwarm>
{
}