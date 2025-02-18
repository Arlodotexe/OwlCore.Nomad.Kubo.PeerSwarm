using CommunityToolkit.Diagnostics;
using Ipfs;
using OwlCore.ComponentModel;
using OwlCore.Nomad.Kubo.PeerSwarm;
using OwlCore.Console.Nomad.Kubo.PeerSwarm.Models.Serialization.UpdateEvents;
using OwlCore.Nomad;
using OwlCore.Nomad.Kubo;

namespace OwlCore.Console.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer that can be modified.
/// </summary>
public class ModifiablePeer : NomadKuboEventStreamHandler<PeerSwarmUpdateEvent>, IModifiablePeer, IDelegable<Models.Peer>
{
    /// <summary>
    /// Creates a new instance of <see cref="ModifiablePeer"/> from the specified handler configuration.
    /// </summary>
    /// <param name="handlerConfig">The handler configuration to use.</param>
    /// <param name="kuboOptions">The options used to read and write data to and from Kubo.</param>
    /// <param name="client">The IPFS client used to interact with the network.</param>
    /// <returns>A new instance of <see cref="ModifiablePeer"/>.</returns>
    public static ModifiablePeer FromHandlerConfig(NomadKuboEventStreamHandlerConfig<Models.Peer> handlerConfig, IKuboOptions kuboOptions, Ipfs.CoreApi.ICoreApi client)
    {
        Guard.IsNotNull(handlerConfig.RoamingValue);
        Guard.IsNotNull(handlerConfig.RoamingKey);
        Guard.IsNotNull(handlerConfig.LocalValue);
        Guard.IsNotNull(handlerConfig.LocalKey);

        return new ModifiablePeer
        {
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
    public IEnumerable<MultiAddress> Addresses => Inner.Addresses;

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
    public required Models.Peer Inner { get; init; }

    /// <inheritdoc />
    public event EventHandler<MultiAddress[]>? PeerAddressesAdded;

    /// <inheritdoc />
    public event EventHandler<MultiAddress[]>? PeerAddressesRemoved;

    /// <inheritdoc />
    public async Task AddAddressAsync(MultiAddress address, CancellationToken cancellationToken)
    {
        var addAddressEvent = new PeerSwarmPeerAddressAddEvent(EventStreamHandlerId, address);
        await ApplyEntryUpdateAsync(addAddressEvent, cancellationToken);
        await AppendNewEntryAsync(addAddressEvent, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAddressAsync(MultiAddress address, CancellationToken cancellationToken)
    {
        var removeAddressEvent = new PeerSwarmPeerAddressRemoveEvent(EventStreamHandlerId, address);
        await ApplyEntryUpdateAsync(removeAddressEvent, cancellationToken);
        await AppendNewEntryAsync(removeAddressEvent, cancellationToken);
    }

    /// <inheritdoc />
    public override Task ResetEventStreamPositionAsync(CancellationToken cancellationToken)
    {
        Inner.Addresses = [];
        EventStreamPosition = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override Task ApplyEntryUpdateAsync(PeerSwarmUpdateEvent updateEvent, CancellationToken cancellationToken)
    {
        switch (updateEvent)
        {
            case PeerSwarmPeerAddressAddEvent peerAddressAddedEvent:
                {
                    if (Inner.Addresses.All(x => x.ToString() != peerAddressAddedEvent.Address.ToString()))
                    {
                        Inner.Addresses = [.. Inner.Addresses, peerAddressAddedEvent.Address];
                        PeerAddressesAdded?.Invoke(this, [peerAddressAddedEvent.Address]);
                    }
                    break;
                }
            case PeerSwarmPeerAddressRemoveEvent peerAddressRemovedEvent:
                {
                    Inner.Addresses = [.. Inner.Addresses.Except([peerAddressRemovedEvent.Address])];
                    PeerAddressesRemoved?.Invoke(this, [peerAddressRemovedEvent.Address]);
                    break;
                }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc cref="INomadKuboEventStreamHandler{TEventEntryContent}.AppendNewEntryAsync" />
    public override async Task<EventStreamEntry<Cid>> AppendNewEntryAsync(PeerSwarmUpdateEvent updateEvent, CancellationToken cancellationToken = default)
    {
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
        var localPublish = this.PublishLocalAsync<ModifiablePeer, PeerSwarmUpdateEvent>(cancellationToken);
        var roamingPublish = this.PublishRoamingAsync<ModifiablePeer, PeerSwarmUpdateEvent, Models.Peer>(cancellationToken);
        
        return Task.WhenAll(localPublish, roamingPublish);
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(FlushAsync(CancellationToken.None));
    }
}
