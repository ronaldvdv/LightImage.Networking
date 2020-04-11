# LightImage.Networking

This repository contains basic networking functionality for software components that need to discover each other, exchange information about available services and connect via these services. All logic is built on top of the awesome [NetMQ library](https://github.com/zeromq/netmq) for low-level networking.

## Introduction

### Services

The repository supports the creation of connected software components called *nodes* that run one or more *services*. These nodes automatically find each other and exchange information about their services and how to connect those, through a *disovery* protocol. Each node may join a *session*; its services then connect with all corresponding services in other nodes in that same session. Services may be symmetrical (e.g. all nodes with this service behave the same) or have different *roles*. These roles can further determine which nodes connect to each other for that particular service.

### Actors

Each service borrows some concepts from the *actor* model to perform its communication with other nodes and maintain state. The *service* is the class responsible to the outside world that exposes *events* for incoming data and *commands* for sending data to connected peers. Internally it creates a *shim (handler)* which performs the actual work and communication with the outside world. This handler does not share any data with the service, as it runs on a separate thread. Communication between the two takes place via NetMQ sockets.

## Getting started

### Implementing a service

*TODO* Which classes to subclass / methods to implement

### Running the service

*TODO* Which modules to register in the container, how to start discovery etc.

## Packages

The libraries are distributed as NuGet packages.

| Package | NuGet |
|---------|-------|
| LightImage.Networking.Services | [![Services](https://img.shields.io/nuget/v/lightimage.networking.services)](https://www.nuget.org/packages/LightImage.Networking.Services/) |
| LightImage.Networking.Discovery | [![Discovery](https://img.shields.io/nuget/v/lightimage.networking.discovery)](https://www.nuget.org/packages/LightImage.Networking.Discovery/) |
| LightImage.Networking.FileSharing | [![Discovery](https://img.shields.io/nuget/v/lightimage.networking.filesharing)](https://www.nuget.org/packages/LightImage.Networking.FileSharing/) |

