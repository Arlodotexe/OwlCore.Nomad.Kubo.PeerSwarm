using CommunityToolkit.Diagnostics;
using Ipfs;
using OwlCore.ComponentModel;
using OwlCore.Nomad.Kubo.Events;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer that can be modified.
/// </summary>
public class ModifiablePeer : NomadKuboEventStreamHandler<ValueUpdateEvent>, IModifiablePeer, IDelegable<Models.Peer>
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
        // Transform MultiAddress to DagCid
        var cid = await Client.Dag.PutAsync(address, pin: KuboOptions.ShouldPin, cancel: cancellationToken);

        var addAddressEvent = new ValueUpdateEvent(null, (DagCid)cid, false);
        EventStreamPosition = await AppendNewEntryAsync(targetId: EventStreamHandlerId, eventId: nameof(AddAddressAsync), addAddressEvent, DateTime.UtcNow, cancellationToken);
        await ApplyEntryUpdateAsync(EventStreamPosition, addAddressEvent, address, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAddressAsync(MultiAddress address, CancellationToken cancellationToken)
    {
        // Transform MultiAddress to DagCid
        var cid = await Client.Dag.PutAsync(address, pin: KuboOptions.ShouldPin, cancel: cancellationToken);

        var removeAddressEvent = new ValueUpdateEvent(null, (DagCid)cid, true);
        EventStreamPosition = await AppendNewEntryAsync(targetId: EventStreamHandlerId, eventId: nameof(RemoveAddressAsync), removeAddressEvent, DateTime.UtcNow, cancellationToken);
        await ApplyEntryUpdateAsync(EventStreamPosition, removeAddressEvent, address, cancellationToken);
    }

    /// <inheritdoc />
    public override Task ResetEventStreamPositionAsync(CancellationToken cancellationToken)
    {
        Inner.Addresses = [];
        EventStreamPosition = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task ApplyEntryUpdateAsync(EventStreamEntry<DagCid> streamEntry, ValueUpdateEvent updateEvent, CancellationToken cancellationToken)
    {
        // Transform DagCid to MultiAddress
        Guard.IsNotNull(updateEvent.Value);
        var address = await Client.Dag.GetAsync<MultiAddress>(updateEvent.Value, cancel: cancellationToken);
        
        Guard.IsNotNull(address);
        await ApplyEntryUpdateAsync(streamEntry, updateEvent, address, cancellationToken);
    }

    /// <inheritdoc />
    public Task ApplyEntryUpdateAsync(EventStreamEntry<DagCid> streamEntry, ValueUpdateEvent updateEvent, MultiAddress address, CancellationToken cancellationToken)
    {
        switch (streamEntry.EventId)
        {
            case nameof(AddAddressAsync):
            {
                if (Inner.Addresses.All(x => x.ToString() != address))
                {
                    Inner.Addresses = [.. Inner.Addresses, address];
                    PeerAddressesAdded?.Invoke(this, [address]);
                }
                break;
            }
            case nameof(RemoveAddressAsync):
            {
                Inner.Addresses = [.. Inner.Addresses.Except([address])];
                PeerAddressesRemoved?.Invoke(this, [address]);
                break;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Flushes the changes to the underlying device.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the ongoing operation.</param>
    public Task FlushAsync(CancellationToken cancellationToken)
    {
        var localPublish = this.PublishLocalAsync<ModifiablePeer, ValueUpdateEvent>(cancellationToken);
        var roamingPublish = this.PublishRoamingAsync<ModifiablePeer, ValueUpdateEvent, Models.Peer>(cancellationToken);
        
        return Task.WhenAll(localPublish, roamingPublish);
    }

    public ValueTask DisposeAsync()
    {
        return new ValueTask(FlushAsync(CancellationToken.None));
    }
}
