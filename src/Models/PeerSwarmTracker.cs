using Ipfs;
using OwlCore.ComponentModel;

namespace OwlCore.Nomad.Kubo.PeerSwarm.Models;

public record PeerSwarmTracker : ISources<Cid>
{
    public required string Id { get; init; }
    public Cid[] PeerSwarms { get; set; } = [];
    public ICollection<Cid> Sources { get; init; } = [];
}
