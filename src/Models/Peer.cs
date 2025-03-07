using Ipfs;
using OwlCore.ComponentModel;

namespace OwlCore.Nomad.Kubo.PeerSwarm.Models;

public record Peer : ISources<Cid>
{
    public required string Id { get; init; }
    public MultiAddress[] Addresses { get; set; } = [];
    public ICollection<Cid> Sources { get; init; } = [];
}
