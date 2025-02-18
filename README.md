# OwlCore.Nomad.Kubo.PeerSwarm [![Version](https://img.shields.io/nuget/v/OwlCore.Nomad.Kubo.PeerSwarm.svg)](https://www.nuget.org/packages/OwlCore.Nomad.Kubo.PeerSwarm)

Track and manage a custom Kubo peer swarm across devices.

## Featuring:
- **Address tracking** for a Kubo peer as [`ModifiablePeer`](https://github.com/Arlodotexe/OwlCore.Nomad.Kubo.PeerSwarm/blob/main/src/ModifiablePeer.cs) or [`ReadOnlyPeer`](https://github.com/Arlodotexe/OwlCore.Nomad.Kubo.PeerSwarm/blob/main/src/ReadOnlyPeer.cs).
- **Multi-peer tracking**: of `ModifiablePeer` or `ReadOnlyPeer` using [`ModifiablePeerSwarm`](https://github.com/Arlodotexe/OwlCore.Nomad.Kubo.PeerSwarm/blob/main/src/ModifiablePeerSwarm.cs) or [`ReadOnlyPeerSwarm`](https://github.com/Arlodotexe/OwlCore.Nomad.Kubo.PeerSwarm/blob/main/src/ReadOnlyPeerSwarm.cs).
- **Multi-swarm tracking**: Track entire `ModifiablePeerSwarm`s and `ReadOnlyPeerSwarm`s using [`ModifiablePeerSwarmTracker`](https://github.com/Arlodotexe/OwlCore.Nomad.Kubo.PeerSwarm/blob/main/src/ModifiablePeerSwarmTracker.cs) or [`ReadOnlyPeerSwarmTracker`](https://github.com/Arlodotexe/OwlCore.Nomad.Kubo.PeerSwarm/blob/main/src/ReadOnlyPeerSwarmTracker.cs).
- **Pair, roam and update data across devices** thanks to tooling supplied by [OwlCore.Nomad.Kubo](https://github.com/Arlodotexe/OwlCore.Nomad.Kubo/)

All together, this library is designed to allow you to piggyback off the Peer Routing provided by the public [Amino DHT](https://probelab.io/ipfs/amino) for [private Content Routing](https://github.com/ipfs/kubo/blob/master/docs/experimental-features.md#private-networks).
In dotnet, the tracked addresses can be provided to the [`PrivateKuboBootstrapper`](https://github.com/Arlodotexe/OwlCore.Kubo/blob/main/src/PrivateKuboBootstrapper.cs) supplied by the [OwlCore.Kubo](https://github.com/Arlodotexe/OwlCore.Kubo/) library for easy setup.

## Install

Published releases are available on [NuGet](https://www.nuget.org/packages/OwlCore.Nomad.Kubo.PeerSwarm). To install, run the following command in the [Package Manager Console](https://docs.nuget.org/docs/start-here/using-the-package-manager-console).

    PM> Install-Package OwlCore.Nomad.Kubo.PeerSwarm
    
Or using [dotnet](https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet)

    > dotnet add package OwlCore.Nomad.Kubo.PeerSwarm

## Usage

```cs
// Coming soon
```

## Financing

We accept donations [here](https://github.com/sponsors/Arlodotexe) and [here](https://www.patreon.com/arlodotexe), and we do not have any active bug bounties.

## Versioning

Version numbering follows the Semantic versioning approach. However, if the major version is `0`, the code is considered alpha and breaking changes may occur as a minor update.

## License

All OwlCore code is licensed under the MIT License. OwlCore is licensed under the MIT License. See the [LICENSE](./src/LICENSE.txt) file for more details.
