using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Autofac;
using LightImage.Networking.Discovery.Events;
using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;
using NetMQ;

[assembly: InternalsVisibleTo("LightImage.Networking.Discovery.Tests")]

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Node that can discover other nodes and their services over a network.
    /// </summary>
    public partial class DiscoveryNode : AbstractNode, IDiscoveryNode
    {
        /// <summary>
        /// Session number to be used when the node has not joined any session.
        /// </summary>
        public const int C_NO_SESSION = 0;

        /// <summary>
        /// Actor managing the internal shim handler.
        /// </summary>
        private readonly NetMQActor _actor;

        /// <summary>
        /// Logger for this node.
        /// </summary>
        private readonly ILogger<DiscoveryNode> _logger;

        /// <summary>
        /// Cache of currently known peers.
        /// </summary>
        private readonly Dictionary<Guid, Peer> _peers = new Dictionary<Guid, Peer>();

        /// <summary>
        /// Poller used to listen for feedback from the shim.
        /// </summary>
        private readonly NetMQPoller _poller;

        /// <summary>
        /// Services exposed by the node.
        /// </summary>
        private readonly IServiceManager _services;

        /// <summary>
        /// Task factory for firing events on the owning thread.
        /// </summary>
        private readonly TaskFactory _taskFactory;

        /// <summary>
        /// Value indicating whether the node is currently running.
        /// </summary>
        private bool _isActive = false;

        /// <summary>
        /// Current session of the node.
        /// </summary>
        private int _session = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryNode"/> class using dependency injection.
        /// </summary>
        /// <param name="options">General network options to be used for the ID, component and name.</param>
        /// <param name="services">Services to be exposed and managed by the node.</param>
        /// <param name="discoveryOptions">Options specific to the discovery process.</param>
        /// <param name="logger">Logger for this node.</param>
        /// <param name="shimLogger">Logger for the <see cref="DiscoveryShim"/>.</param>
        public DiscoveryNode(NetworkOptions options, IServiceManager services, DiscoveryOptions discoveryOptions, ILogger<DiscoveryNode> logger, ILogger<DiscoveryShim> shimLogger)
            : this(options.Id, options.Component, options.Type, logger, shimLogger, services, discoveryOptions)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryNode"/> class.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="name">Descriptive name.</param>
        /// <param name="type">Type of node.</param>
        /// <param name="logger">Logger for this node.</param>
        /// <param name="shimLogger">Logger for the <see cref="DiscoveryShim"/>.</param>
        /// <param name="services">Services to be exposed and managed.</param>
        /// <param name="options">Additional configuration.</param>
        public DiscoveryNode(Guid id, string name, string type, ILogger<DiscoveryNode> logger, ILogger<DiscoveryShim> shimLogger, IServiceManager services, DiscoveryOptions options = null)
            : base(id, name, type, null, 0)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace(DiscoveryEvents.Initialize, "Initializing discovery ");

            _services.PeerHeartbeat += HandlePeerHeartbeat;
            _services.Start();

            _actor = NetMQActor.Create(new DiscoveryShim(id, name, type, services.GetDescriptors(), shimLogger, options));
            _actor.ReceiveReady += HandleActorReceiveReady;

            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            _taskFactory = new TaskFactory(scheduler);

            _logger.LogInformation(DiscoveryEvents.Initialize, "Starting discovery for {id}, component {name}, type {type}", id, name, type);

            _poller = new NetMQPoller { _actor };
            _isActive = true;
            _poller.RunAsync();
        }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> Initialized;

        /// <inheritdoc/>
        public event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;

        /// <inheritdoc/>
        public event EventHandler<PeerEventArgs> PeerEvasive;

        /// <inheritdoc/>
        public event EventHandler<PeerEventArgs> PeerLost;

        /// <inheritdoc/>
        public event EventHandler<PeerRenamedEventArgs> PeerRenamed;

        /// <inheritdoc/>
        public event EventHandler<PeerEventArgs> PeerReturned;

        /// <inheritdoc/>
        public event EventHandler<PeerSessionChangedEventArgs> PeerSessionChanged;

        /// <inheritdoc/>
        public event EventHandler<ServiceDiscoveredEventArgs> ServiceDiscovered;

        /// <inheritdoc/>
        public event EventHandler<SessionChangedEventArgs> SessionChanged;

        /// <inheritdoc/>
        public void Add(Guid peer)
        {
            if (_session == C_NO_SESSION)
            {
                throw new InvalidOperationException($"{nameof(Add)} is not valid when not part of a session");
            }

            _actor.SendAddCommand(peer);
            _logger.LogTrace(DiscoveryEvents.AddPeer, "Adding {peer}", peer);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            _actor.SendStopCommand();
            _services.Stop();
            _poller.Stop();
            _poller.Dispose();

            if (!_actor.IsDisposed)
            {
                _actor.Dispose();
            }
        }

        /// <inheritdoc/>
        public void Join(int session)
        {
            if (session == _session)
            {
                return;
            }

            _actor.SendSessionCommand(session);

            _services.Reset();

            // See which peers must be added to services since we're in the same session now
            _session = session;
            var add = _peers.Values.Where(p => p.Session == session && session != C_NO_SESSION).ToArray();
            foreach (var peer in add)
            {
                foreach (var service in peer.Services.Where(p => p.ClusterBehaviour == ServiceClusterBehaviour.Session))
                {
                    ConnectService(service, peer);
                }
            }

            OnSessionChanged(session);
        }

        private void ConnectService(IServiceDescription service, Peer peer)
        {
            _services.Connect(peer.Id, service.Name, peer.Host, service.Ports, service.Role);
        }

        private void DisconnectService(IServiceDescription service, Peer peer)
        {
            _services.Disconnect(peer.Id, service.Name);
        }

        private void HandleActorEvent(string cmd, NetMQMessage msg)
        {
            switch (cmd)
            {
                case DiscoveryMessages.C_EVT_PEER:
                    var data = PeerStatusData.Parse(msg[1].Buffer);
                    ProcessPeerStatus(data);
                    break;

                case DiscoveryMessages.C_EVT_INIT:
                    Host = msg[1].ConvertToString();
                    Port = msg[2].ConvertToInt32();
                    OnInitialized();
                    break;

                case DiscoveryMessages.C_EVT_SERVICES:
                    var id = new Guid(msg[1].Buffer);
                    var services = msg.Skip(2).Select(frame => ServiceData.Parse(frame.Buffer)).ToArray();
                    ProcessServices(id, services);
                    break;

                case DiscoveryMessages.C_EVT_JOIN:
                    var session = msg[1].ConvertToInt32();
                    Join(session);
                    break;
            }
        }

        private void HandleActorReceiveReady(object sender, NetMQActorEventArgs e)
        {
            var msg = e.Actor.ReceiveMultipartMessage();
            var cmd = msg[0].ConvertToString();
            _taskFactory.StartNew(() =>
            {
                HandleActorEvent(cmd, msg);
            });
        }

        private void HandlePeerEnteredSession(Peer peer)
        {
            _logger.LogTrace(DiscoveryEvents.PeerEnteredSession, "Peer {id} entered the current session {session}", peer.Id, _session);
            foreach (var service in peer.Services.Where(s => s.ClusterBehaviour == ServiceClusterBehaviour.Session))
            {
                ConnectService(service, peer);
            }
        }

        private void HandlePeerHeartbeat(object sender, ServicePeerHeartbeatEventArgs e)
        {
            DiscoveryMessages.SendHeartbeatCommand(_actor, e.PeerId);
        }

        private void HandlePeerLeftSession(Peer peer)
        {
            _logger.LogTrace(DiscoveryEvents.PeerLeftSession, "Peer {id} left the current session {session}", peer.Id, _session);
            foreach (var service in peer.Services.Where(s => s.ClusterBehaviour == ServiceClusterBehaviour.Session))
            {
                DisconnectService(service, peer);
            }
        }

        private void OnInitialized()
        {
            _logger.LogInformation(DiscoveryEvents.Started, "Discovery started at {host}:{port}", Host, Port);
            Initialized?.Invoke(this, EventArgs.Empty);
        }

        private void OnPeerDiscovered(PeerStatusData data)
        {
            PeerDiscovered?.Invoke(this, new PeerDiscoveredEventArgs(data.Id, data.Host, data.Port, data.Name, data.Type, data.Session));
        }

        private void OnPeerEvasive(Guid id)
        {
            PeerEvasive?.Invoke(this, new PeerEventArgs(id));
        }

        private void OnPeerLost(Guid id)
        {
            PeerLost?.Invoke(this, new PeerEventArgs(id));
        }

        private void OnPeerRenamed(Guid id, string name, string type)
        {
            PeerRenamed?.Invoke(this, new PeerRenamedEventArgs(id, name, type));
        }

        private void OnPeerReturned(Guid id)
        {
            PeerReturned?.Invoke(this, new PeerEventArgs(id));
        }

        private void OnPeerSessionChanged(Guid id, int session)
        {
            PeerSessionChanged?.Invoke(this, new PeerSessionChangedEventArgs(id, session));
        }

        private void OnServiceDiscovered(Guid id, ServiceData service)
        {
            ServiceDiscovered?.Invoke(this, new ServiceDiscoveredEventArgs(id, service));
        }

        private void OnSessionChanged(int session)
        {
            SessionChanged?.Invoke(this, new SessionChangedEventArgs(session));
        }

        private void ProcessPeerStatus(PeerStatusData data)
        {
            var exists = _peers.TryGetValue(data.Id, out var peer);
            _logger.LogTrace(DiscoveryEvents.PeerUpdated, "Peer {id} at {host} changed status to {status}, session {session}, name {name}, type {type}", data.Id, data.Host, data.Status, data.Session, data.Name, data.Type);

            switch (data.Status)
            {
                case PeerStatus.Alive:

                    if (exists)
                    {
                        if (peer.Status != PeerStatus.Alive)
                        {
                            peer.Status = PeerStatus.Alive;
                            OnPeerReturned(data.Id);
                        }

                        if (data.Session != peer.Session)
                        {
                            UpdatePeerSession(peer, data.Session);
                        }

                        peer.UpdateHost(data.Host);

                        if (data.Name != peer.Name || data.Type != peer.Type)
                        {
                            peer.Rename(data.Name, data.Type);
                            OnPeerRenamed(data.Id, peer.Name, peer.Type);
                        }
                    }
                    else
                    {
                        _peers.Add(data.Id, new Peer(data.Id, data.Name, data.Type, data.Host, data.Port, data.Session));
                        OnPeerDiscovered(data);
                    }

                    break;

                case PeerStatus.Evasive:
                    peer.Status = PeerStatus.Evasive;
                    OnPeerEvasive(data.Id);
                    break;

                case PeerStatus.Lost:
                    OnPeerLost(data.Id);
                    _services.Remove(peer.Id);
                    _peers.Remove(data.Id);
                    break;
            }
        }

        private void ProcessServices(Guid id, ServiceData[] services)
        {
            var peer = _peers[id];

            foreach (var service in services)
            {
                if (!peer.Add(service))
                {
                    continue;
                }

                _logger.LogInformation(DiscoveryEvents.ServiceReceived, "Received service {service} for {peer}; role {role}, ports {ports}", service.Name, id, service.Role, service.Ports);

                if (service.ClusterBehaviour == ServiceClusterBehaviour.Global || (_session != C_NO_SESSION && peer.Session == _session))
                {
                    ConnectService(service, peer);
                }

                OnServiceDiscovered(id, service);
            }
        }

        private void UpdatePeerSession(Peer peer, int session)
        {
            var previousSession = peer.Session;
            peer.Session = session;

            if (_session != C_NO_SESSION)
            {
                if (_session == session)
                {
                    HandlePeerEnteredSession(peer);
                }
                else if (previousSession == _session)
                {
                    HandlePeerLeftSession(peer);
                }
            }

            OnPeerSessionChanged(peer.Id, session);
        }

        private class Peer : AbstractNode
        {
            private readonly Dictionary<string, IServiceDescription> _services = new Dictionary<string, IServiceDescription>();

            public Peer(Guid id, string name, string type, string host, int port, int session)
                : base(id, name, type, host, port)
            {
                Session = session;
            }

            public IReadOnlyCollection<IServiceDescription> Services => _services.Values;

            public int Session { get; internal set; }

            public PeerStatus Status { get; internal set; } = PeerStatus.Alive;

            internal bool Add(IServiceDescription service)
            {
                if (_services.TryGetValue(service.Name, out var existing))
                {
                    if (existing.Role != service.Role || !existing.Ports.SequenceEqual(service.Ports))
                    {
                        throw new InvalidOperationException($"Received multiple instances of service data for service '${service.Name}' for the same node, with conflicting role/port information");
                    }

                    return false;
                }

                _services[service.Name] = service;
                return true;
            }

            internal void Rename(string name, string type)
            {
                Name = name;
                Type = type;
            }

            internal void UpdateHost(string host)
            {
                Host = host;
            }
        }
    }
}