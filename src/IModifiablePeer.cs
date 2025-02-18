using Ipfs;
using OwlCore.ComponentModel;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// Represents a peer that can be read and modified.
/// </summary>
public interface IModifiablePeer : IReadOnlyPeer, IFlushable, IAsyncDisposable
{
    /// <summary>
    /// Saves an address where this peer can be contacted.
    /// </summary>
    /// <param name="address">The address to register.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AddAddressAsync(MultiAddress address, CancellationToken cancellationToken);

    /// <summary>
    /// Marks an address for removal from the registry.
    /// </summary>
    /// <param name="address">The address to remove from the registry.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RemoveAddressAsync(MultiAddress address, CancellationToken cancellationToken);
}