using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetMQ;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Base class for any service that performs clustering.
    /// </summary>
    /// <typeparam name="TShim">Type of shim.</typeparam>
    /// <typeparam name="TShimPeer">Type of peer data.</typeparam>
    public abstract class ClusterService<TShim, TShimPeer> : Service
        where TShim : ClusterShim<TShimPeer>
        where TShimPeer : ClusterShimPeer
    {
        private readonly ILogger<ClusterService<TShim, TShimPeer>> _logger;
        private readonly Dictionary<Guid, ServicePeer> _peers = new Dictionary<Guid, ServicePeer>();
        private readonly TaskFactory _taskFactory;

        private NetMQActor _actor;

        private NetMQPoller _poller;

        private NetMQQueue<NetMQMessage> _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterService{TShim, TShimPeer}"/> class.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="host">Host name.</param>
        /// <param name="name">Name of the service.</param>
        /// <param name="role">Role of the node in this service.</param>
        /// <param name="logger">Logger instance.</param>
        public ClusterService(Guid id, string host, string name, string role, ILogger<ClusterService<TShim, TShimPeer>> logger)
            : base(name, role)
        {
            Id = id;
            Host = host;
            _logger = logger;
            logger.LogTrace(ClusterEvents.Initializing, "Initializing cluster service {name}:{role} for node {id} at {host}", name, role, id, host);
            var scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            _taskFactory = new TaskFactory(scheduler);
        }

        /// <summary>
        /// Gets the host name of the node.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the unique identifier of the node.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets a value indicating whether the service is currently running.
        /// </summary>
        public bool IsRunning { get; private set; } = false;

        /// <summary>
        /// Gets a way to send messages to the actor which runs the see <see cref="ClusterShim{TPeer}"/>.
        /// </summary>
        protected IOutgoingSender Actor { get; private set; }

        /// <inheritdoc/>
        public override void Add(Guid id, string host, string role, int[] ports)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException();
            }

            var servicePeer = new ServicePeer(id, host, role, ports);
            if (!AllowConnection(servicePeer))
            {
                return;
            }

            _peers.Add(id, servicePeer);
            Connect(servicePeer);
        }

        /// <inheritdoc/>
        public override void Remove(Guid id)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException();
            }

            if (!_peers.TryGetValue(id, out var servicePeer))
            {
                return;
            }

            Remove(servicePeer);
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException();
            }

            foreach (var peer in _peers.Values.ToArray())
            {
                Remove(peer);
            }

            ClusterMessages.SendReset(Actor);
        }

        /// <inheritdoc/>
        public override void Start()
        {
            if (IsRunning)
            {
                throw new InvalidOperationException();
            }

            var shim = CreateShim();
            _actor = NetMQActor.Create(shim);
            _queue = new NetMQQueue<NetMQMessage>();
            _actor.ReceiveReady += HandleActor_ReceiveReady;
            _queue.ReceiveReady += HandleQueue_ReceiveReady;
            Actor = new MessageQueueSender(_queue);

            var port = BitConverter.ToInt32(_actor.ReceiveFrameBytes(), 0);
            SetPorts(port);

            _poller = new NetMQPoller { _actor, _queue };
            Setup(_poller);
            _poller.RunAsync();

            IsRunning = true;
        }

        /// <inheritdoc/>
        public override void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            IsRunning = false;

            _logger.LogTrace(ClusterEvents.Stopping, "Stopping service {name}", Name);

            _poller.Stop();
            _poller.Dispose();
            _queue.Dispose();
            _actor.Dispose();
        }

        /// <summary>
        /// Decide whether the service should connect to a particular peer.
        /// </summary>
        /// <param name="servicePeer">Potential service peer.</param>
        /// <returns>Value indicating whether the service wants to connect.</returns>
        protected virtual bool AllowConnection(ServicePeer servicePeer) => true;

        /// <summary>
        /// Create a <see cref="ClusterShim{TPeer}"/>.
        /// </summary>
        /// <returns>The shim handler for this service.</returns>
        protected abstract TShim CreateShim();

        /// <summary>
        /// Handle an event received from the service shim.
        /// </summary>
        /// <param name="cmd">Type of command.</param>
        /// <param name="msg">Complete message.</param>
        protected virtual void HandleActorEvent(string cmd, NetMQMessage msg)
        {
            switch (cmd)
            {
                case ClusterMessages.C_EVT_HEARTBEAT:
                    ClusterMessages.ParseHeartbeat(msg, out Guid nodeId);
                    OnPeerHeartbeat(nodeId);
                    break;

                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Callback that fires when a peer is being connected.
        /// </summary>
        /// <param name="peer">Peer being connected.</param>
        protected virtual void OnConnecting(ServicePeer peer)
        {
        }

        /// <summary>
        /// Callback that fires when a peer is being disconnected.
        /// </summary>
        /// <param name="peer">Peer being disconnected.</param>
        protected virtual void OnDisconnecting(ServicePeer peer)
        {
        }

        /// <summary>
        /// Setup any additional pollable classes required for running the service.
        /// </summary>
        /// <param name="poller">Poller to which additional instances can be added.</param>
        protected virtual void Setup(NetMQPoller poller)
        {
        }

        private void Connect(ServicePeer peer)
        {
            var endpoint = $"tcp://{peer.Host}:{peer.Ports[0]}";
            _logger.LogTrace(ClusterEvents.Connect, "Connecting to {id} (role {role}) at {endpoint}", peer.Id, peer.Role, endpoint);
            ClusterMessages.SendConnect(Actor, peer.Id, endpoint, peer.Role);
            OnConnecting(peer);
        }

        private void Disconnect(ServicePeer peer)
        {
            _logger.LogTrace(ClusterEvents.Disconnect, "Disconnecting from {id} (role {role})", peer.Id, peer.Role);
            ClusterMessages.SendDisconnect(Actor, peer.Id);
            OnDisconnecting(peer);
        }

        private void HandleActor_ReceiveReady(object sender, NetMQActorEventArgs e)
        {
            if (!IsRunning)
            {
                return;
            }

            var msg = e.Actor.ReceiveMultipartMessage();
            var cmd = msg[0].ConvertToString();

            _taskFactory.StartNew(() =>
            {
                if (!IsRunning)
                {
                    return;
                }

                HandleActorEvent(cmd, msg);
            });
        }

        private void HandleQueue_ReceiveReady(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            var msg = _queue.Dequeue();
            _actor.SendMultipartMessage(msg);
        }

        private void Remove(ServicePeer peer)
        {
            _peers.Remove(peer.Id);
            Disconnect(peer);
        }
    }
}