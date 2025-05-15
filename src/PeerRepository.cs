using Ipfs;
using OwlCore.Nomad.Kubo.Events;

namespace OwlCore.Nomad.Kubo.PeerSwarm;

/// <summary>
/// A repository for managing the lifecycle of Nomad peer swarm event stream handlers via Kubo.
/// </summary>
public class PeerRepository : NomadKuboRepository<ModifiablePeer, IReadOnlyPeer, Models.Peer, ValueUpdateEvent>
{
    /// <summary>
    /// The prefix used to build local and roaming key names
    /// </summary>
    public string KeyNamePrefix { get; set; } = "Nomad.Kubo.PeerSwarm.Peer";
    
    /// <inheritdoc/> 
    public override IReadOnlyPeer ReadOnlyFromHandlerConfig(NomadKuboEventStreamHandlerConfig<Models.Peer> handlerConfig) => ReadOnlyPeer.FromHandlerConfig(handlerConfig);

    /// <inheritdoc/> 
    public override ModifiablePeer ModifiableFromHandlerConfig(NomadKuboEventStreamHandlerConfig<Models.Peer> handlerConfig) => ModifiablePeer.FromHandlerConfig(handlerConfig, KuboOptions, Client);

    /// <inheritdoc/> 
    protected override NomadKuboEventStreamHandlerConfig<Models.Peer> GetEmptyConfig() => new();

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
        return (LocalKeyName: $"{KeyNamePrefix}.Local", RoamingKeyName: $"{KeyNamePrefix}.Roaming");
    }
    
    /// <inheritdoc/> 
    public override string GetNewEventStreamLabel(IKey roamingKey, IKey localKey) => "Peer";
    
    /// <inheritdoc/> 
    public override Models.Peer GetInitialRoamingValue(IKey roamingKey, IKey localKey) => new()
    {
        Id = roamingKey.Id,
        Sources = [localKey.Id],
    };
}