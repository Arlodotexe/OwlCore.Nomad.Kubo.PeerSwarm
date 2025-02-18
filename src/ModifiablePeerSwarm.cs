using CommunityToolkit.Diagnostics;
using Ipfs;
using OwlCore.ComponentModel;
using OwlCore.Nomad.Kubo.PeerSwarm;
using OwlCore.Console.Nomad.Kubo.PeerSwarm.Models.Serialization.UpdateEvents;
using OwlCore.Nomad;
using OwlCore.Nomad.Kubo;
using System.Runtime.CompilerServices;

namespace OwlCore.Console.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm that can be modified.
/// </summary>
public class ModifiablePeerSwarm : NomadKuboEventStreamHandler<PeerSwarmUpdateEvent>, IModifiablePeerSwarm, INomadKuboEventStreamHandler<PeerSwarmUpdateEvent>, IDelegable<Models.PeerSwarm>
{
    /// <summary>
    /// Creates a new instance of <see cref="ModifiablePeerSwarm"/> from the specified handler configuration.
    /// </summary>
    /// <param name="handlerConfig">The handler configuration to use.</param>
    /// <param name="kuboOptions">The options used to read and write data to and from Kubo.</param>
    /// <param name="client">The IPFS client used to interact with the network.</param>
    /// <returns>A new instance of <see cref="ModifiablePeerSwarm"/>.</returns>
    public static ModifiablePeerSwarm FromHandlerConfig(NomadKuboEventStreamHandlerConfig<Models.PeerSwarm> handlerConfig, INomadKuboRepository<ModifiablePeer, IReadOnlyPeer> peerRepository, IKuboOptions kuboOptions, Ipfs.CoreApi.ICoreApi client)
    {
        Guard.IsNotNull(handlerConfig.RoamingValue);
        Guard.IsNotNull(handlerConfig.RoamingKey);
        Guard.IsNotNull(handlerConfig.LocalValue);
        Guard.IsNotNull(handlerConfig.LocalKey);

        return new ModifiablePeerSwarm
        {
            PeerRepository = peerRepository,
            EventStreamHandlerId = handlerConfig.RoamingKey.Id,
            Inner = new() { Id = handlerConfig.RoamingKey.Id },
            LocalEventStream = handlerConfig.LocalValue,
            RoamingKey = handlerConfig.RoamingKey,
            LocalEventStreamKey = handlerConfig.LocalKey,
            Sources = handlerConfig.RoamingValue.Sources,
            KuboOptions = kuboOptions,
            Client = client,
        };
    }

    /// <inheritdoc />
    public string Id => EventStreamHandlerId;

    /// <inheritdoc />
    public override required ICollection<Cid> Sources
    {
        get => Inner.Sources;
        init
        {
            foreach (var val in value)
                if (Inner.Sources.All(x => val != x))
                    Inner.Sources.Add(val);
        }
    }

    /// <summary>
    /// The roaming peer swarm data.
    /// </summary>
    public required Models.PeerSwarm Inner { get; set; }

    /// <summary>
    /// Gets the repository used to get, create and manage peers.
    /// </summary>
    public required INomadKuboRepository<ModifiablePeer, IReadOnlyPeer> PeerRepository { get; init; }

    /// <inheritdoc />
    public event EventHandler<IReadOnlyPeer[]>? ItemsAdded;

    /// <inheritdoc />
    public event EventHandler<IReadOnlyPeer[]>? ItemsRemoved;

    /// <inheritdoc />
    public async Task<ModifiablePeer> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var created = await PeerRepository.CreateAsync(cancellationToken);
        await AddAsync(created, cancellationToken);

        return created;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(ModifiablePeer item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await RemoveAsync(item, cancellationToken);
        await PeerRepository.DeleteAsync(item, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(IReadOnlyPeer peer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var addEvent = new PeerSwarmAddEvent(EventStreamHandlerId, peer.Id);
        await ApplyEntryUpdateAsync(addEvent, peer, cancellationToken);
        EventStreamPosition = await AppendNewEntryAsync(addEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(IReadOnlyPeer peer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var removeEvent = new PeerSwarmRemoveEvent(Id, peer.Id);
        await ApplyEntryUpdateAsync(removeEvent, peer, cancellationToken);
        await AppendNewEntryAsync(removeEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IReadOnlyPeer> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var roamingPeerId in Inner.Peers)
            yield return await GetAsync(roamingPeerId, cancellationToken);
    }

    /// <summary>
    /// Gets a peer by its id.
    /// </summary>
    /// <param name="id">The roaming id to retrieve peer data from.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    public async Task<IReadOnlyPeer> GetAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Handles read/write permissions internally
        return await PeerRepository.GetAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public override Task ResetEventStreamPositionAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Inner.Peers = [];
        EventStreamPosition = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ApplyEntryUpdateAsync(PeerSwarmUpdateEvent updateEvent, IReadOnlyPeer arg, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (updateEvent)
        {
            case PeerSwarmAddEvent peerAddedEvent:
                {
                    Inner.Peers = [.. Inner.Peers, peerAddedEvent.PeerId];
                    ItemsAdded?.Invoke(this, [arg]);
                    break;
                }
            case PeerSwarmRemoveEvent peerRemovedEvent:
                {
                    Inner.Peers = [.. Inner.Peers.Except([peerRemovedEvent.PeerId])];
                    ItemsRemoved?.Invoke(this, [arg]);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(updateEvent), updateEvent, null);
        }
    }

    /// <summary>
    /// Applies an event stream update event and raises the relevant events.
    /// </summary>
    /// <remarks>
    /// This method will call <see cref="GetAsync(string, CancellationToken)"/> and create a new instance to pass to the event handlers.
    /// <para/>
    /// If already have an instance of the peer, you should call <see cref="ApplyEntryUpdateAsync(PeerSwarmUpdateEvent, IReadOnlyPeer, CancellationToken)"/> instead.
    /// </remarks>
    /// <param name="updateEvent">The update event to apply.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public override async Task ApplyEntryUpdateAsync(PeerSwarmUpdateEvent updateEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (updateEvent)
        {
            case PeerSwarmAddEvent peerAddedEvent:
                {
                    Inner.Peers = [.. Inner.Peers, peerAddedEvent.PeerId];
                    var arg = await GetAsync(peerAddedEvent.PeerId, cancellationToken);
                    ItemsAdded?.Invoke(this, [arg]);
                    break;
                }
            case PeerSwarmRemoveEvent peerRemovedEvent:
                {
                    Inner.Peers = [.. Inner.Peers.Except([peerRemovedEvent.PeerId])];
                    var arg = await GetAsync(peerRemovedEvent.PeerId, cancellationToken);
                    ItemsRemoved?.Invoke(this, [arg]);
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(updateEvent), updateEvent, null);
        }
    }

    /// <inheritdoc cref="INomadKuboEventStreamHandler{TEventEntryContent}.AppendNewEntryAsync" />
    public override async Task<EventStreamEntry<Cid>> AppendNewEntryAsync(PeerSwarmUpdateEvent updateEvent, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Use extension method for code deduplication (can't use inheritance).
        var localUpdateEventCid = await Client.Dag.PutAsync(updateEvent, pin: KuboOptions.ShouldPin, cancel: cancellationToken);
        var newEntry = await this.AppendEventStreamEntryAsync(localUpdateEventCid, updateEvent.EventId, targetId: EventStreamHandlerId, cancellationToken);
        return newEntry;
    }

    /// <summary>
    /// Flushes the changes to the underlying device.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    public Task FlushAsync(CancellationToken cancellationToken)
    {
        var localPublish = this.PublishLocalAsync<ModifiablePeerSwarm, PeerSwarmUpdateEvent>(cancellationToken);
        var roamingPublish = this.PublishRoamingAsync<ModifiablePeerSwarm, PeerSwarmUpdateEvent, Models.PeerSwarm>(cancellationToken);
        
        return Task.WhenAll(localPublish, roamingPublish);
    }
}