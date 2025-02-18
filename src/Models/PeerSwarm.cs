using Ipfs;
using OwlCore.ComponentModel;

namespace OwlCore.Console.Nomad.Kubo.PeerSwarm.Models;

public record PeerSwarm : ISources<Cid>
{
    public required string Id { get; init; }
    public Cid[] Peers { get; set; } = [];
    public ICollection<Cid> Sources { get; init; } = [];
}
