using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using Ipfs;
using OwlCore.ComponentModel;
using OwlCore.Nomad.Kubo.Events;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer swarm tracker that can be modified.
/// </summary>
public class ModifiablePeerSwarmTracker : NomadKuboEventStreamHandler<ValueUpdateEvent>, INomadKuboEventStreamHandler<ValueUpdateEvent>, IModifiablePeerSwarmTracker, IDelegable<Models.PeerSwarmTracker>
{
    /// <summary>
    /// Creates a new instance of <see cref="ModifiablePeerSwarmTracker"/> from the specified handler configuration.
    /// </summary>
    /// <param name="handlerConfig">The handler configuration to use.</param>
    /// <param name="kuboOptions">The options used to read and write data to and from Kubo.</param>
    /// <param name="client">The IPFS client used to interact with the network.</param>
    /// <returns>A new instance of <see cref="ModifiablePeerSwarmTracker"/>.</returns>
    public static ModifiablePeerSwarmTracker FromHandlerConfig(NomadKuboEventStreamHandlerConfig<Models.PeerSwarmTracker> handlerConfig, INomadKuboRepository<ModifiablePeerSwarm, IReadOnlyPeerSwarm> peerSwarmRepository, IKuboOptions kuboOptions, Ipfs.CoreApi.ICoreApi client)
    {
        Guard.IsNotNull(handlerConfig.RoamingValue);
        Guard.IsNotNull(handlerConfig.RoamingKey);
        Guard.IsNotNull(handlerConfig.LocalValue);
        Guard.IsNotNull(handlerConfig.LocalKey);

        return new ModifiablePeerSwarmTracker
        {
            PeerSwarmRepository = peerSwarmRepository,
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

    /// <inheritdoc />
    public required Models.PeerSwarmTracker Inner { get; set; }

    /// <summary>
    /// Gets the repository used to get, create and manage peers.
    /// </summary>
    public required INomadKuboRepository<ModifiablePeerSwarm, IReadOnlyPeerSwarm> PeerSwarmRepository { get; init; }

    /// <inheritdoc />
    public event EventHandler<IReadOnlyPeerSwarm[]>? ItemsAdded;

    /// <inheritdoc />
    public event EventHandler<IReadOnlyPeerSwarm[]>? ItemsRemoved;

    /// <inheritdoc />
    public async Task<ModifiablePeerSwarm> CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var created = await PeerSwarmRepository.CreateAsync(cancellationToken);
        await AddAsync(created, cancellationToken);

        return created;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(ModifiablePeerSwarm item, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await RemoveAsync(item, cancellationToken);
        await PeerSwarmRepository.DeleteAsync(item, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(IReadOnlyPeerSwarm peerSwarm, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var peerSwarmIdCid = await Client.Dag.PutAsync(peerSwarm.Id, pin: KuboOptions.ShouldPin, cancel: cancellationToken);
        var addEvent = new ValueUpdateEvent(null, (DagCid)peerSwarmIdCid, false);

        EventStreamPosition = await AppendNewEntryAsync(targetId: EventStreamHandlerId, eventId: nameof(AddAsync), addEvent, DateTime.UtcNow, cancellationToken);
        await ApplyEntryUpdateAsync(EventStreamPosition, addEvent, peerSwarm, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(IReadOnlyPeerSwarm peerSwarm, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var peerSwarmIdCid = await Client.Dag.PutAsync(peerSwarm.Id, pin: KuboOptions.ShouldPin, cancel: cancellationToken);
        var removeEvent = new ValueUpdateEvent(null, (DagCid)peerSwarmIdCid, true);

        EventStreamPosition = await AppendNewEntryAsync(targetId: EventStreamHandlerId, eventId: nameof(RemoveAsync), removeEvent, DateTime.UtcNow, cancellationToken);
        await ApplyEntryUpdateAsync(EventStreamPosition, removeEvent, peerSwarm, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyPeerSwarm> GetAsync(string id, CancellationToken cancellationToken)
    {
        return PeerSwarmRepository.GetAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IReadOnlyPeerSwarm> GetAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var peerSwarmCid in Inner.PeerSwarms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return await GetAsync(peerSwarmCid, cancellationToken);
        }
    }

    /// <inheritdoc />
    public override Task ResetEventStreamPositionAsync(CancellationToken cancellationToken)
    {
        Inner.PeerSwarms = [];
        EventStreamPosition = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task ApplyEntryUpdateAsync(EventStreamEntry<Cid> eventStreamEntry, ValueUpdateEvent updateEvent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        Guard.IsNotNull(updateEvent.Value);
        var peerSwarmId = await Client.Dag.GetAsync<Cid>(updateEvent.Value, cancel: cancellationToken);

        await ApplyEntryUpdateAsync(eventStreamEntry, updateEvent, peerSwarmId, cancellationToken);
    }

    /// <summary>
    /// Applies an event stream update event and raises the relevant events.
    /// </summary>
    /// <param name="eventStreamEntry">The event stream entry to apply.</param>
    /// <param name="updateEvent">The update event to apply.</param>
    /// <param name="peerSwarmId">The ID of the peer swarm being added or removed from the peer swarm tracker.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    /// <exception cref="ArgumentOutOfRangeException">Unknown <see cref="EventStreamEntry{TContentPointer}.EventId"/>.</exception>
    public async Task ApplyEntryUpdateAsync(EventStreamEntry<Cid> eventStreamEntry, ValueUpdateEvent updateEvent, Cid peerSwarmId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var arg = await GetAsync(peerSwarmId, cancellationToken);
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
    public Task ApplyEntryUpdateAsync(EventStreamEntry<Cid> eventStreamEntry, ValueUpdateEvent updateEvent, IReadOnlyPeerSwarm arg, CancellationToken cancellationToken)
    {
        switch (eventStreamEntry.EventId)
        {
            case nameof(AddAsync):
                {
                    Inner.PeerSwarms = [.. Inner.PeerSwarms, arg.Id];
                    ItemsAdded?.Invoke(this, [arg]);
                    break;
                }
            case nameof(RemoveAsync):
                {
                    var targetPeer = Inner.PeerSwarms.FirstOrDefault(x => x == arg.Id);
                    if (targetPeer is not null)
                    {
                        Inner.PeerSwarms = Inner.PeerSwarms.Where(x => x != targetPeer).ToArray();
                        ItemsRemoved?.Invoke(this, [arg]);
                    }
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
        var localPublish = this.PublishLocalAsync<ModifiablePeerSwarmTracker, ValueUpdateEvent>(cancellationToken);
        var roamingPublish = this.PublishRoamingAsync<ModifiablePeerSwarmTracker, ValueUpdateEvent, Models.PeerSwarmTracker>(cancellationToken);

        return Task.WhenAll(localPublish, roamingPublish);
    }
}