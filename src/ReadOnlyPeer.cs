using CommunityToolkit.Diagnostics;
using Ipfs;
using OwlCore.ComponentModel;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

public class ReadOnlyPeer : IReadOnlyPeer, IDelegable<Models.Peer>
{
    /// <summary>
    /// Creates a new instance of <see cref="ReadOnlyPeer"/> from the given handler configuration.
    /// </summary>
    /// <param name="handlerConfig">The handler configuration to use.</param>
    /// <returns>A new instance of <see cref="ReadOnlyPeer"/>.</returns>
    public static ReadOnlyPeer FromHandlerConfig(NomadKuboEventStreamHandlerConfig<Models.Peer> handlerConfig)
    {
        Guard.IsNotNull(handlerConfig.RoamingValue);
        Guard.IsNotNull(handlerConfig.RoamingId);

        return new ReadOnlyPeer
        {
            Inner = handlerConfig.RoamingValue,
            Id = handlerConfig.RoamingId,
        };
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ReadOnlyPeer"/> without event support.
    /// </summary>
    public ReadOnlyPeer() { }

    /// <summary>
    /// The published roaming data for this peer.
    /// </summary>
    public required Models.Peer Inner { get; init; }

    /// <inheritdoc />
    public IEnumerable<MultiAddress> Addresses => Inner.Addresses;

    /// <inheritdoc />
    public required string Id { get; init; }

    /// <inheritdoc />
    // TODO: Implement this
    public event EventHandler<MultiAddress[]>? PeerAddressesAdded;

    /// <inheritdoc />
    // TODO: Implement this
    public event EventHandler<MultiAddress[]>? PeerAddressesRemoved;
}
