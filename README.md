# LightImage.Networking

This repository contains basic networking functionality for software components that need to discover each other, exchange information about available services and connect via these services. 

_Note_: This repository is still under development. Breaking changes will be introduced in upcoming versions.

All logic is built on top of the following dependencies. You will need a basic understanding of each of these libraries in order to use the networking packages.

- The awesome [NetMQ library](https://github.com/zeromq/netmq) for low-level networking and actors.
- [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging) abstractions for logging
- [Microsoft.Extensions.Configuration](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.configuration) abstractions for configuration
- [Autofac](https://autofac.org/) for dependency injection

## Introduction

### Services

The repository supports the creation of connected software components called *nodes* that run one or more *services*. These nodes automatically find each other and exchange information about their services and how to connect those, through a *disovery* protocol. Each node may join a *session*; its services then connect with all corresponding services in other nodes in that same session. Services may be symmetrical (e.g. all nodes with this service behave the same) or have different *roles*. These roles can further determine which nodes connect to each other for that particular service.

### Actors

Each service borrows some concepts from the *actor* model to perform its communication with other nodes and maintain state. The *service* is the class responsible to the outside world that exposes *events* for incoming data and *commands* for sending data to connected peers. Internally it creates a *shim (handler)* which performs the actual work and communication with the outside world. This handler does not share any data with the service, as it runs on a separate thread. Communication between the two takes place via NetMQ sockets.

## Getting started

Below are the steps to get a simple cluster of nodes to work. Have a look at the `LightImage.Networking.Samples.Chat` project for details.

### Implementing a service

- Define a subclass of `ClusterShimPeer`. In this class you may store additional state related to a specific peer.
- Define a subclass of `ClusterShim`. It should do the following:
  - Return an instance of your new peer class from method `CreatePeer`.
  - Handle incoming network messages from peer nodes in `HandleRouterMessage`.
  - Handle incoming commands from the service in `HandleShimMessage`.
  - When necessary, provide feedback to the service by sending messages to the `Shim` socket property.
- Define a subclass of `ClusterService`. It should do the following:
  - Return an instance of your shim class from method `CreateShim`.
  - Process incoming messages from the shim in `HandleActorEvent`. Expose relevant triggers to consuming classes using C# events.
  - Expose methods to control the service to its consumers. These typically send messages to the shim via the `Actor` socket property.

### Running the service

_Note_: The steps below relate to WPF desktop applications. For console applications, some more steps are involved to set up a proper `SynchronizationContext`.

- Setup an `IConfiguration` instance; you may create an empty one using `new ConfigurationBuilder().Build()`.
- Setup an AutoFac `IContainer` by registering the following components in a `ContainerBuilder`:
  - Module `NetworkingServicesModule`
  - Module `DiscoveryModule`
  - Your service as an `IService` 
  - Logging, for example using `builder.AddTestLogging()`
- Retrieve an `IDiscoveryNode` instance; you may call `node.Join(1)` to join a particular session on startup.
- Retrieve your service instance and use it.

## Packages

The libraries are distributed as NuGet packages.

| Package | NuGet |
|---------|-------|
| LightImage.Networking.Services | [![Services](https://img.shields.io/nuget/v/lightimage.networking.services)](https://www.nuget.org/packages/LightImage.Networking.Services/) |
| LightImage.Networking.Discovery | [![Discovery](https://img.shields.io/nuget/v/lightimage.networking.discovery)](https://www.nuget.org/packages/LightImage.Networking.Discovery/) |
| LightImage.Networking.FileSharing | [![Discovery](https://img.shields.io/nuget/v/lightimage.networking.filesharing)](https://www.nuget.org/packages/LightImage.Networking.FileSharing/) |

## Todo

- Simplify and document _Getting started_ for console applications.
- Document configuration options
- Replace Autofac dependency by `microsoft.Extensions.DependencyInjection`.