using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using Ipfs;
using Ipfs.CoreApi;
using OwlCore.ComponentModel;
using OwlCore.Nomad.Kubo.PeerSwarm;
using OwlCore.Kubo;
using OwlCore.Nomad.Kubo;

namespace OwlCore.Console.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm that can be read but not modified.
/// </summary>
public class ReadOnlyPeerSwarm : IReadOnlyPeerSwarm, IDelegable<Models.PeerSwarm>
{
    /// <summary>
    /// Creates a new instance of <see cref="ReadOnlyPeer"/> from the given handler configuration.
    /// </summary>
    /// <param name="handlerConfig">The handler configuration to use.</param>
    /// <param name="kuboOptions">The options used to read and write data to and from Kubo.</param>
    /// <param name="client">The IPFS client used to interact with the network.</param>
    /// <returns>A new instance of <see cref="ReadOnlyPeer"/>.</returns>
    public static ReadOnlyPeerSwarm FromHandlerConfig(NomadKuboEventStreamHandlerConfig<Models.PeerSwarm> handlerConfig, INomadKuboRepository<ModifiablePeer, IReadOnlyPeer>? peerRepository, IKuboOptions kuboOptions, ICoreApi client)
    {
        Guard.IsNotNull(handlerConfig.RoamingId);
        Guard.IsNotNull(handlerConfig.RoamingValue);

        return new ReadOnlyPeerSwarm
        {
            Client = client,
            KuboOptions = kuboOptions,
            PeerRepository = peerRepository,
            Inner = handlerConfig.RoamingValue,
            Id = handlerConfig.RoamingId,
        };
    }

    /// <summary>
    /// The underlying roaming data published for this peer swarm.
    /// </summary>
    public required Models.PeerSwarm Inner { get; init; }

    /// <summary>
    /// Gets the repository used to get, create and manage peers.
    /// </summary>
    public required INomadKuboRepository<ModifiablePeer, IReadOnlyPeer>? PeerRepository { get; init; }

    /// <summary>
    /// The IPFS client used to interact with the network.
    /// </summary>
    public required ICoreApi Client { get; init; }

    /// <summary>
    /// The options used to read and write data to and from Kubo.
    /// </summary>
    public required IKuboOptions KuboOptions { get; init; }

    /// <summary>
    /// The ID of this peer swarm.
    /// </summary>
    public required string Id { get; init; }

    /// <inheritdoc/>
    public event EventHandler<IReadOnlyPeer[]>? ItemsAdded;

    /// <inheritdoc/>
    public event EventHandler<IReadOnlyPeer[]>? ItemsRemoved;

    /// <inheritdoc/>
    public async IAsyncEnumerable<IReadOnlyPeer> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var peer in Inner.Peers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return await GetAsync(peer, cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyPeer> GetAsync(string id, CancellationToken cancellationToken)
    {
        // Get read-only or modifiable based on provided repository.
        if (PeerRepository is not null)
            return await PeerRepository.GetAsync(id, cancellationToken);

        // Get read-only if no repository provided.
        cancellationToken.ThrowIfCancellationRequested();
        var peerCid = Cid.Decode(id);

        var (roamingData, _) = await peerCid.ResolveDagCidAsync<Models.Peer>(Client, nocache: !KuboOptions.UseCache, cancellationToken);
        Guard.IsNotNull(roamingData);

        return new ReadOnlyPeer
        {
            Id = peerCid,
            Inner = roamingData,
        };
    }
}
