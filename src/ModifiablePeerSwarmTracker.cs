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
/// Represents a peer swarm tracker that can be modified.
/// </summary>
public class ModifiablePeerSwarmTracker : NomadKuboEventStreamHandler<PeerSwarmTrackerUpdateEvent>, INomadKuboEventStreamHandler<PeerSwarmTrackerUpdateEvent>, IModifiablePeerSwarmTracker, IDelegable<Models.PeerSwarmTracker>
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
        var addEvent = new PeerSwarmTrackerAddEvent(peerSwarm.Id, peerSwarm.Id);
        await ApplyEntryUpdateAsync(addEvent, peerSwarm, cancellationToken);
        EventStreamPosition = await AppendNewEntryAsync(addEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(IReadOnlyPeerSwarm peerSwarm, CancellationToken cancellationToken)
    {
        var removeEvent = new PeerSwarmTrackerRemoveEvent(peerSwarm.Id, peerSwarm.Id);
        await ApplyEntryUpdateAsync(removeEvent, peerSwarm, cancellationToken);
        EventStreamPosition = await AppendNewEntryAsync(removeEvent, cancellationToken);
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
    public async Task ApplyEntryUpdateAsync(PeerSwarmTrackerUpdateEvent updateEvent, IReadOnlyPeerSwarm arg, CancellationToken cancellationToken)
    {
        switch (updateEvent)
        {
            case PeerSwarmTrackerAddEvent peerAddedEvent:
                {
                    Inner.PeerSwarms = [.. Inner.PeerSwarms, peerAddedEvent.PeerSwarmId];
                    ItemsAdded?.Invoke(this, [arg]);
                    break;
                }
            case PeerSwarmTrackerRemoveEvent peerRemovedEvent:
                {
                    var targetPeer = Inner.PeerSwarms.FirstOrDefault(x => x == peerRemovedEvent.PeerSwarmId);
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
    }

    /// <inheritdoc />
    public override async Task ApplyEntryUpdateAsync(PeerSwarmTrackerUpdateEvent updateEvent, CancellationToken cancellationToken)
    {
        switch (updateEvent)
        {
            case PeerSwarmTrackerAddEvent peerAddedEvent:
                {
                    Inner.PeerSwarms = [.. Inner.PeerSwarms, peerAddedEvent.PeerSwarmId];
                    var arg = await GetAsync(peerAddedEvent.PeerSwarmId, cancellationToken);
                    ItemsAdded?.Invoke(this, [arg]);
                    break;
                }
            case PeerSwarmTrackerRemoveEvent peerRemovedEvent:
                {
                    var targetPeer = Inner.PeerSwarms.FirstOrDefault(x => x == peerRemovedEvent.PeerSwarmId);
                    if (targetPeer is not null)
                    {
                        Inner.PeerSwarms = Inner.PeerSwarms.Where(x => x != targetPeer).ToArray();
                        var arg = await GetAsync(peerRemovedEvent.PeerSwarmId, cancellationToken);
                        ItemsRemoved?.Invoke(this, [arg]);
                    }
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(updateEvent), updateEvent, null);
        }
    }

    /// <inheritdoc cref="INomadKuboEventStreamHandler{TEventEntryContent}.AppendNewEntryAsync" />
    public override async Task<EventStreamEntry<Cid>> AppendNewEntryAsync(PeerSwarmTrackerUpdateEvent updateEvent, CancellationToken cancellationToken = default)
    {
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
        var localPublish = this.PublishLocalAsync<ModifiablePeerSwarmTracker, PeerSwarmTrackerUpdateEvent>(cancellationToken);
        var roamingPublish = this.PublishRoamingAsync<ModifiablePeerSwarmTracker, PeerSwarmTrackerUpdateEvent, Models.PeerSwarmTracker>(cancellationToken);

        return Task.WhenAll(localPublish, roamingPublish);
    }
}