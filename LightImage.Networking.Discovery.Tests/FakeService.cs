using LightImage.Networking.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LightImage.Networking.Discovery.Tests
{
    internal class FakeService : IService
    {
        private readonly List<Peer> _peers = new List<Peer>();

        public FakeService(string name, string role, int[] ports)
        {
            Name = name;
            Role = role;
            Ports = ports;
            Peers = _peers.AsReadOnly();
        }

#pragma warning disable 67

        public event EventHandler<ServicePeerHeartbeatEventArgs> PeerHeartbeat;

#pragma warning restore 67

        public string Name { get; }

        public ReadOnlyCollection<Peer> Peers { get; }
        public int[] Ports { get; }
        public string Role { get; }

        public void Add(Guid id, string host, string role, int[] ports)
        {
            _peers.Add(new Peer(id, host, role, ports));
        }

        public void Dispose()
        {
        }

        public void Remove(Guid id)
        {
            var peer = _peers.FirstOrDefault(p => p.Id == id);
            _peers.Remove(peer);
        }

        public void Reset()
        {
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public readonly struct Peer
        {
            public readonly string Host;
            public readonly Guid Id;
            public readonly int[] Ports;
            public readonly string Role;

            public Peer(Guid id, string host, string role, int[] ports)
            {
                Id = id;
                Host = host;
                Ports = ports;
                Role = role;
            }
        }
    }
}