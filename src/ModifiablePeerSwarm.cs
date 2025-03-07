using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using Ipfs;
using OwlCore.ComponentModel;
using OwlCore.Nomad.Kubo.Events;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm that can be modified.
/// </summary>
public class ModifiablePeerSwarm : NomadKuboEventStreamHandler<ValueUpdateEvent>, IModifiablePeerSwarm, INomadKuboEventStreamHandler<ValueUpdateEvent>, IDelegable<Models.PeerSwarm>
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

        var peerIdCid = await Client.Dag.PutAsync(peer.Id, pin: KuboOptions.ShouldPin, cancel: cancellationToken);
        var addEvent = new ValueUpdateEvent(null, (DagCid)peerIdCid, false);

        EventStreamPosition = await AppendNewEntryAsync(targetId: EventStreamHandlerId, eventId: nameof(AddAsync), addEvent, DateTime.UtcNow, cancellationToken);
        await ApplyEntryUpdateAsync(EventStreamPosition, addEvent, peer, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(IReadOnlyPeer peer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var peerIdCid = await Client.Dag.PutAsync(peer.Id, pin: KuboOptions.ShouldPin, cancel: cancellationToken);
        var removeEvent = new ValueUpdateEvent(null, (DagCid)peerIdCid, true);
        
        EventStreamPosition = await AppendNewEntryAsync(targetId: EventStreamHandlerId, eventId: nameof(RemoveAsync), removeEvent, DateTime.UtcNow, cancellationToken);
        await ApplyEntryUpdateAsync(EventStreamPosition, removeEvent, peer, cancellationToken);
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

    /// <summary>
    /// Applies an event stream update event and raises the relevant events.
    /// </summary>
    /// <remarks>
    /// This method will call <see cref="GetAsync(string, CancellationToken)"/> and create a new instance to pass to the event handlers.
    /// <para/>
    /// If already have an instance of the peer, you should call <see cref="ApplyEntryUpdateAsync(EventStreamEntry{Cid}, ValueUpdateEvent, IReadOnlyPeer, CancellationToken)"/> instead.
    /// </remarks>
    /// <param name="eventStreamEntry">The event stream entry to apply.</param>
    /// <param name="updateEvent">The update event to apply.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Unknown <see cref="EventStreamEntry{TContentPointer}.EventId"/>.</exception>
    public override async Task ApplyEntryUpdateAsync(EventStreamEntry<Cid> eventStreamEntry, ValueUpdateEvent updateEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        Guard.IsNotNull(updateEvent.Value);
        var peerId = await Client.Dag.GetAsync<Cid>(updateEvent.Value, cancel: cancellationToken);

        await ApplyEntryUpdateAsync(eventStreamEntry, updateEvent, peerId, cancellationToken);
    }

    /// <summary>
    /// Applies an event stream update event and raises the relevant events.
    /// </summary>
    /// <param name="eventStreamEntry">The event stream entry to apply.</param>
    /// <param name="updateEvent">The update event to apply.</param>
    /// <param name="peerId">The peerId being added or removed from the peer swarm.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Unknown <see cref="EventStreamEntry{TContentPointer}.EventId"/>.</exception>
    public async Task ApplyEntryUpdateAsync(EventStreamEntry<Cid> eventStreamEntry, ValueUpdateEvent updateEvent, Cid peerId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var arg = await GetAsync(peerId, cancellationToken);
        await ApplyEntryUpdateAsync(eventStreamEntry, updateEvent, arg, cancellationToken);
    }

    /// <summary>
    /// Applies an event stream update event and raises the relevant events.
    /// </summary>
    /// <param name="eventStreamEntry">The event stream entry to apply.</param>
    /// <param name="updateEvent">The update event to apply.</param>
    /// <param name="arg">The peer being added or removed from the peer swarm.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Unknown <see cref="EventStreamEntry{TContentPointer}.EventId"/>.</exception>
    public Task ApplyEntryUpdateAsync(EventStreamEntry<Cid> eventStreamEntry, ValueUpdateEvent updateEvent, IReadOnlyPeer arg, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (eventStreamEntry.EventId)
        {
            case nameof(AddAsync):
            {
                Inner.Peers = [.. Inner.Peers, arg.Id];
                ItemsAdded?.Invoke(this, [arg]);
                break;
            }
            case nameof(RemoveAsync):
            {
                Inner.Peers = [.. Inner.Peers.Except([(Cid)arg.Id])];
                ItemsRemoved?.Invoke(this, [arg]);
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(updateEvent), updateEvent, null);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Flushes the changes to the underlying device.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    public Task FlushAsync(CancellationToken cancellationToken)
    {
        var localPublish = this.PublishLocalAsync<ModifiablePeerSwarm, ValueUpdateEvent>(cancellationToken);
        var roamingPublish = this.PublishRoamingAsync<ModifiablePeerSwarm, ValueUpdateEvent, Models.PeerSwarm>(cancellationToken);
        
        return Task.WhenAll(localPublish, roamingPublish);
    }
}