using Ipfs;
using Ipfs.CoreApi;
using OwlCore.Nomad.Kubo.Events;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Provides methods for getting repositories for managing peers, peer swarms and peer swarm trackers.
/// </summary>
public static class PeerSwarmRepoFactory
{
    /// <summary>
    /// Gets a repository for managing peer swarms.
    /// </summary>
    /// <param name="roamingKeyName">The key name used to publish the roaming data.</param>
    /// <param name="localKeyName">The key name used to publish the local event stream.</param>
    /// <param name="peerRepository">The repository for getting and managing peers in the swarm.</param>
    /// <param name="client">The IPFS client used to interact with the network.</param>
    /// <param name="kuboOptions">The options used to read and write data to and from Kubo.</param>
    /// <returns>A repository for managing peer swarms.</returns>
    public static NomadKuboRepository<ModifiablePeerSwarmTracker, IReadOnlyPeerSwarmTracker, Models.PeerSwarmTracker, ValueUpdateEvent> GetPeerSwarmTrackerRepository(string roamingKeyName, string localKeyName, NomadKuboRepository<ModifiablePeer, IReadOnlyPeer, Models.Peer, ValueUpdateEvent>? peerRepository, NomadKuboRepository<ModifiablePeerSwarm, IReadOnlyPeerSwarm, Models.PeerSwarm, ValueUpdateEvent> peerSwarmRepository, ICoreApi client, IKuboOptions kuboOptions)
    {
        return new NomadKuboRepository<ModifiablePeerSwarmTracker, IReadOnlyPeerSwarmTracker, Models.PeerSwarmTracker, ValueUpdateEvent>
        {
            DefaultEventStreamLabel = "Peer Swarm Tracker",
            Client = client,
            KuboOptions = kuboOptions,
            GetEventStreamHandlerConfigAsync = async (roamingId, cancellationToken) =>
            {
                var (localKey, roamingKey, foundRoamingId) = await NomadKeyHelpers.RoamingIdToNomadKeysAsync(roamingId, roamingKeyName, localKeyName, client, cancellationToken);
                return new NomadKuboEventStreamHandlerConfig<Models.PeerSwarmTracker>
                {
                    RoamingId = roamingKey?.Id ?? (foundRoamingId is not null ? Cid.Decode(foundRoamingId) : null),
                    RoamingKey = roamingKey,
                    RoamingKeyName = roamingKeyName,
                    LocalKey = localKey,
                    LocalKeyName = localKeyName,
                };
            },
            GetDefaultRoamingValue = (localKey, roamingKey) => new Models.PeerSwarmTracker { Id = roamingKey.Id, Sources = [localKey.Id] },
            ModifiableFromHandlerConfig = config => ModifiablePeerSwarmTracker.FromHandlerConfig(config, peerSwarmRepository, kuboOptions, client),
            ReadOnlyFromHandlerConfig = config => ReadOnlyPeerSwarmTracker.FromHandlerConfig(config, peerSwarmRepository, peerRepository, kuboOptions, client),
        };
    }

    /// <summary>
    /// Gets a repository for managing peer swarms.
    /// </summary>
    /// <param name="roamingKeyName">The key name used to publish the roaming data.</param>
    /// <param name="localKeyName">The key name used to publish the local event stream.</param>
    /// <param name="peerRepository">The repository for getting and managing peers in the swarm.</param>
    /// <param name="client">The IPFS client used to interact with the network.</param>
    /// <param name="kuboOptions">The options used to read and write data to and from Kubo.</param>
    /// <returns>A repository for managing peer swarms.</returns>
    public static NomadKuboRepository<ModifiablePeerSwarm, IReadOnlyPeerSwarm, Models.PeerSwarm, ValueUpdateEvent> GetPeerSwarmRepository(string roamingKeyName, string localKeyName, NomadKuboRepository<ModifiablePeer, IReadOnlyPeer, Models.Peer, ValueUpdateEvent> peerRepository, ICoreApi client, IKuboOptions kuboOptions)
    {
        return new NomadKuboRepository<ModifiablePeerSwarm, IReadOnlyPeerSwarm, Models.PeerSwarm, ValueUpdateEvent>
        {
            DefaultEventStreamLabel = "Peer Swarm",
            Client = client,
            KuboOptions = kuboOptions,
            GetEventStreamHandlerConfigAsync = async (roamingId, cancellationToken) =>
            {
                var (localKey, roamingKey, foundRoamingId) = await NomadKeyHelpers.RoamingIdToNomadKeysAsync(roamingId, roamingKeyName, localKeyName, client, cancellationToken);
                return new NomadKuboEventStreamHandlerConfig<Models.PeerSwarm>
                {
                    RoamingId = roamingKey?.Id ?? (foundRoamingId is not null ? Cid.Decode(foundRoamingId) : null),
                    RoamingKey = roamingKey,
                    RoamingKeyName = roamingKeyName,
                    LocalKey = localKey,
                    LocalKeyName = localKeyName,
                };
            },
            GetDefaultRoamingValue = (localKey, roamingKey) => new Models.PeerSwarm { Id = roamingKey.Id, Sources = [localKey.Id] },
            ModifiableFromHandlerConfig = config => ModifiablePeerSwarm.FromHandlerConfig(config, peerRepository, kuboOptions, client),
            ReadOnlyFromHandlerConfig = config => ReadOnlyPeerSwarm.FromHandlerConfig(config, peerRepository, kuboOptions, client),
        };
    }

    /// <summary>
    /// Gets a repository for managing peers.
    /// </summary>
    /// <param name="roamingKeyName">The key name used to publish the roaming data.</param>
    /// <param name="localKeyName">The key name used to publish the local event stream.</param>
    /// <param name="client">The IPFS client used to interact with the network.</param>
    /// <param name="kuboOptions">The options used to read and write data to and from Kubo.</param>
    /// <returns>A repository for managing peers.</returns>
    public static NomadKuboRepository<ModifiablePeer, IReadOnlyPeer, Models.Peer, ValueUpdateEvent> GetPeerRepository(string roamingKeyName, string localKeyName, ICoreApi client, KuboOptions kuboOptions)
    {
        return new NomadKuboRepository<ModifiablePeer, IReadOnlyPeer, Models.Peer, ValueUpdateEvent>
        {
            DefaultEventStreamLabel = "Peer",
            Client = client,
            KuboOptions = kuboOptions,
            GetEventStreamHandlerConfigAsync = async (roamingId, cancellationToken) =>
            {
                var (localKey, roamingKey, foundRoamingId) = await NomadKeyHelpers.RoamingIdToNomadKeysAsync(roamingId, roamingKeyName, localKeyName, client, cancellationToken);
                return new NomadKuboEventStreamHandlerConfig<Models.Peer>
                {
                    RoamingId = roamingKey?.Id ?? (foundRoamingId is not null ? Cid.Decode(foundRoamingId) : null),
                    RoamingKey = roamingKey,
                    RoamingKeyName = roamingKeyName,
                    LocalKey = localKey,
                    LocalKeyName = localKeyName,
                };
            },
            GetDefaultRoamingValue = (localKey, roamingKey) => new Models.Peer { Id = roamingKey.Id, Sources = [localKey.Id] },
            ModifiableFromHandlerConfig = config => ModifiablePeer.FromHandlerConfig(config, kuboOptions, client),
            ReadOnlyFromHandlerConfig = ReadOnlyPeer.FromHandlerConfig,
        };
    }
}
