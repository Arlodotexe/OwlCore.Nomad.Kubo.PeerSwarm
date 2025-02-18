using Ipfs;
using OwlCore.ComponentModel;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer that can be read but not modified.
/// </summary>
public interface IReadOnlyPeer : IHasId
{
    /// <summary>
    /// Yields the <see cref="MultiAddress"/>es where this peer can be contacted.
    /// </summary>
    public IEnumerable<MultiAddress> Addresses { get; }

    /// <summary>
    /// Raised when an address is added to the peer.
    /// </summary>
    public event EventHandler<MultiAddress[]>? PeerAddressesAdded;

    /// <summary>
    /// Raised when an address is removed from the peer.
    /// </summary>
    public event EventHandler<MultiAddress[]>? PeerAddressesRemoved;
}
