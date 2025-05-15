using Ipfs;
using OwlCore.Nomad.Kubo.Events;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// A repository for managing the lifecycle of Nomad peer swarm event stream handlers via Kubo.
/// </summary>
public class PeerSwarmRepository : NomadKuboRepository<ModifiablePeerSwarm, IReadOnlyPeerSwarm, Models.PeerSwarm, ValueUpdateEvent>
{
    /// <summary>
    /// The prefix used to build local and roaming key names
    /// </summary>
    public string KeyNamePrefix { get; set; } = "Nomad.Kubo.PeerSwarm";
    
    /// <summary>
    /// The repository used to create readonly or modifiable peer instances.
    /// </summary>
    public required PeerRepository PeerRepository { get; init; }
    
    /// <inheritdoc/> 
    public override IReadOnlyPeerSwarm ReadOnlyFromHandlerConfig(NomadKuboEventStreamHandlerConfig<Models.PeerSwarm> handlerConfig) => ReadOnlyPeerSwarm.FromHandlerConfig(handlerConfig, PeerRepository, KuboOptions, Client);

    /// <inheritdoc/> 
    public override ModifiablePeerSwarm ModifiableFromHandlerConfig(NomadKuboEventStreamHandlerConfig<Models.PeerSwarm> handlerConfig) => ModifiablePeerSwarm.FromHandlerConfig(handlerConfig, PeerRepository, KuboOptions, Client);

    /// <inheritdoc/> 
    protected override NomadKuboEventStreamHandlerConfig<Models.PeerSwarm> GetEmptyConfig() => new();

    /// <inheritdoc/> 
    public override Task<(string LocalKeyName, string RoamingKeyName)?> GetExistingKeyNamesAsync(string roamingId, CancellationToken cancellationToken)
    {
        var existingRoamingKey = ManagedKeys.FirstOrDefault(x=> $"{x.Id}" == $"{roamingId}");
        if (existingRoamingKey is null)
            return Task.FromResult<(string LocalKeyName, string RoamingKeyName)?>(null);
        
        // Transform roaming key name into local key name
        // This repository implementation doesn't do anything fancy for this,
        // the names are basically hardcoded to the KeyNamePrefix and roaming vs local.
        var localKeyName = existingRoamingKey.Name.Replace(".Roaming", ".Local");
        return Task.FromResult<(string LocalKeyName, string RoamingKeyName)?>((localKeyName, existingRoamingKey.Name));
    }

    /// <inheritdoc/> 
    public override (string LocalKeyName, string RoamingKeyName) GetNewKeyNames()
    {
        // Get unique key names for the given folder name
        return (LocalKeyName: $"{KeyNamePrefix}.Local", RoamingKeyName: $"{KeyNamePrefix}.Roaming");
    }
    
    /// <inheritdoc/> 
    public override string GetNewEventStreamLabel(IKey roamingKey, IKey localKey) => "Peer Swarm";
    
    /// <inheritdoc/> 
    public override Models.PeerSwarm GetInitialRoamingValue(IKey roamingKey, IKey localKey) => new()
    {
        Id = roamingKey.Id,
        Sources = [localKey.Id],
    };
}