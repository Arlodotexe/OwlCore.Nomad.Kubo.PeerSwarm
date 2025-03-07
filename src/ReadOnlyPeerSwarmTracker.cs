using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.ComponentModel;
using OwlCore.Kubo;
using OwlCore.Nomad.Kubo.PeerSwarm.Models;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm tracker that can be read but not modified.
/// </summary>
public class ReadOnlyPeerSwarmTracker : IReadOnlyPeerSwarmTracker, IDelegable<PeerSwarmTracker>
{
    /// <summary>
    /// Creates a new instance of <see cref="ReadOnlyPeerSwarmTracker"/> from the given handler configuration.
    /// </summary>
    /// <param name="handlerConfig">The handler configuration to use.</param>
    /// <param name="kuboOptions">The options used to read and write data to and from Kubo.</param>
    /// <param name="client">The IPFS client used to interact with the network.</param>
    /// <returns>A new instance of <see cref="ReadOnlyPeer"/>.</returns>
    public static ReadOnlyPeerSwarmTracker FromHandlerConfig(NomadKuboEventStreamHandlerConfig<PeerSwarmTracker> handlerConfig, INomadKuboRepository<ModifiablePeerSwarm, IReadOnlyPeerSwarm>? peerSwarmRepository, INomadKuboRepository<ModifiablePeer, IReadOnlyPeer>? peerRepository, IKuboOptions kuboOptions, ICoreApi client)
    {
        Guard.IsNotNull(handlerConfig.RoamingId);
        Guard.IsNotNull(handlerConfig.RoamingValue);

        return new ReadOnlyPeerSwarmTracker
        {
            PeerRepository = peerRepository,
            PeerSwarmRepository = peerSwarmRepository,
            Client = client,
            KuboOptions = kuboOptions,
            Inner = handlerConfig.RoamingValue,
            Id = handlerConfig.RoamingId,
        };
    }

    /// <summary>
    /// The underlying roaming data published for this peer swarm tracker.
    /// </summary>
    public required PeerSwarmTracker Inner { get; init; }

    /// <summary>
    /// Gets the repository used to get, create and manage peer swarms.
    /// </summary>
    public required INomadKuboRepository<ModifiablePeer, IReadOnlyPeer>? PeerRepository { get; init; }

    /// <summary>
    /// Gets the repository used to get, create and manage peers.
    /// </summary>
    public required INomadKuboRepository<ModifiablePeerSwarm, IReadOnlyPeerSwarm>? PeerSwarmRepository { get; init; }

    /// <summary>
    /// The roaming Id for this peer swarm tracker.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The IPFS client used to interact with the network.
    /// </summary>
    public required ICoreApi Client { get; init; }

    /// <summary>
    /// The options used to read and write data to and from Kubo.
    /// </summary>
    public required IKuboOptions KuboOptions { get; init; }

    /// <inheritdoc />
    public event EventHandler<IReadOnlyPeerSwarm[]>? ItemsAdded;

    /// <inheritdoc />
    public event EventHandler<IReadOnlyPeerSwarm[]>? ItemsRemoved;

    /// <inheritdoc />
    public async IAsyncEnumerable<IReadOnlyPeerSwarm> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var peer in Inner.PeerSwarms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return await GetAsync(peer, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyPeerSwarm> GetAsync(string id, CancellationToken cancellationToken)
    {
        // Use PeerSwarmRepository if it is available
        if (PeerSwarmRepository != null)
            return await PeerSwarmRepository.GetAsync(id, cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();
        var peerSwarmCid = Cid.Decode(id);

        var (roamingData, _) = await peerSwarmCid.ResolveDagCidAsync<Models.PeerSwarm>(Client, nocache: !KuboOptions.UseCache, cancellationToken);
        Guard.IsNotNull(roamingData);

        return new ReadOnlyPeerSwarm
        {
            PeerRepository = PeerRepository,
            Id = peerSwarmCid,
            Inner = roamingData,
            Client = Client,
            KuboOptions = KuboOptions,
        };
    }
}
