using System;
using System.Collections.Generic;
using System.Threading;
using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Base class for any cluster service shim.
    /// </summary>
    /// <typeparam name="TPeer">Type of peer data.</typeparam>
    public abstract class ClusterShim<TPeer> : IShimHandler
        where TPeer : ClusterShimPeer
    {
        private readonly ILogger<ClusterShim<TPeer>> _logger;
        private readonly string _role;
        private Dictionary<Guid, TPeer> _peers = new Dictionary<Guid, TPeer>();
        private NetMQPoller _poller;
        private int _routerPort;
        private MessageQueueSender _sender;

        /// <summary>
        /// Gets the socket that can be used to communicate with the owning service.
        /// </summary>
        private PairSocket _shim;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterShim{TPeer}"/> class.
        /// </summary>
        /// <param name="id">Unique identifier of the node.</param>
        /// <param name="host">Host name.</param>
        /// <param name="role">Role of the node in the service.</param>
        /// <param name="logger">Logger instance.</param>
        public ClusterShim(Guid id, string host, string role, ILogger<ClusterShim<TPeer>> logger)
        {
            Id = id;
            Host = host;
            _role = role;
            _logger = logger;
            _logger.LogTrace(ClusterEvents.Initializing, $"Initializing shim");
        }

        /// <summary>
        /// Gets the full endpoint at which the shim can be reached.
        /// </summary>
        public string RouterEndpoint => $"tcp://{Host}:{_routerPort}";

        /// <summary>
        /// Gets a sender for communicating with the service.
        /// </summary>
        public IOutgoingSender Shim => _sender;

        /// <summary>
        /// Gets the host (IP address) at which the shim can be reached.
        /// </summary>
        protected string Host { get; }

        /// <summary>
        /// Gets the unique identifier of the node.
        /// </summary>
        protected Guid Id { get; }

        /// <summary>
        /// Gets the collection of connected peers.
        /// </summary>
        protected IEnumerable<TPeer> Peers => _peers.Values;

        /// <inheritdoc/>
        public void Run(PairSocket shim)
        {
            _logger.LogTrace(ClusterEvents.Initializing, "Shim is starting on thread {thread}", Thread.CurrentThread.ManagedThreadId);

            var router = new RouterSocket();
            _routerPort = router.BindRandomPort($"tcp://{Host}");
            router.ReceiveReady += HandleRouterReceiveReady;
            shim.ReceiveReady += HandleShimReceiveReady;
            _shim = shim;
            _sender = new MessageQueueSender(_shim);

            shim.SignalOK();
            shim.SendFrame(BitConverter.GetBytes(_routerPort));

            _poller = new NetMQPoller { router, shim, _sender };
            Setup(_poller);
            _poller.Run();
            _logger.LogTrace(ClusterEvents.Stopping, "Shim is stopping");

            DisconnectPeers();

            _logger.LogTrace(ClusterEvents.Stopping, "Shim has disconnected from all peers");

            router.Dispose();
            _sender.Dispose();
            _poller.Dispose();

            TearDown();

            _logger.LogTrace(ClusterEvents.Stopping, "Shim has stopped.");
        }

        /// <summary>
        /// Create a peer descriptor.
        /// </summary>
        /// <param name="id">Unique identifier of the peer.</param>
        /// <param name="endpoint">Endpoint (protocol, host and port) at which the peer can be reached.</param>
        /// <param name="role">Role of the peer node within the service.</param>
        /// <returns>The peer descriptor.</returns>
        protected abstract TPeer CreatePeer(Guid id, string endpoint, string role);

        /// <summary>
        /// Disconnect from all peers.
        /// </summary>
        protected void DisconnectPeers()
        {
            foreach (var peer in _peers.Values)
            {
                peer.Dispose();
            }

            _peers.Clear();
        }

        /// <summary>
        /// Retrieve an existing peer or connect to it.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="endpoint">Endpoint of the peer.</param>
        /// <param name="role">Role of the peer within the service.</param>
        /// <returns>The peer descriptor.</returns>
        protected TPeer GetOrConnectToPeer(Guid id, string endpoint, string role)
        {
            if (!_peers.TryGetValue(id, out var peer))
            {
                peer = _peers[id] = CreatePeer(id, endpoint, role);
                ClusterMessages.SendHello(peer.Dealer, RouterEndpoint, _role);
            }

            return peer;
        }

        /// <summary>
        /// Retrieve an existing peer.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <returns>The peer descriptor, or NULL if not connected.</returns>
        protected TPeer GetPeer(Guid id)
        {
            if (_peers.TryGetValue(id, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Handle an incoming message from a peer node. Subclasses should override this method and call the base implementation.
        /// </summary>
        /// <param name="cmd">Type of command received.</param>
        /// <param name="msg">The full message, including the command.</param>
        /// <param name="identity">Identity of the peer that sent the message.</param>
        protected virtual void HandleRouterMessage(string cmd, NetMQMessage msg, Guid identity)
        {
            switch (cmd)
            {
                case ClusterMessages.C_MSG_HELLO:
                    ClusterMessages.ParseHello(msg, out var endpoint, out var role);

                    /* TODO If we don't know the peer yet, we'll add it but our ClusterService will never know!
                      On the other hand, does the service need a list of peers at all!? */

                    GetOrConnectToPeer(identity, endpoint, role);
                    break;
            }
        }

        /// <summary>
        /// Handle an incoming message from the service. Subclasses should override this method and call the base implementation.
        /// </summary>
        /// <param name="cmd">Type of message received.</param>
        /// <param name="msg">The full message, including the command.</param>
        protected virtual void HandleShimMessage(string cmd, NetMQMessage msg)
        {
            switch (cmd)
            {
                case NetMQActor.EndShimMessage:
                    _poller.Stop();
                    break;

                case ClusterMessages.C_CMD_CONNECT:
                    ClusterMessages.ParseConnect(msg, out var id, out var endpoint, out var role);
                    Connect(id, endpoint, role);
                    break;

                case ClusterMessages.C_CMD_DISCONNECT:
                    ClusterMessages.ParseDisconnect(msg, out id);
                    Disconnect(id);
                    break;

                case ClusterMessages.C_CMD_RESET:
                    OnReset();
                    break;
            }
        }

        /// <summary>
        /// Callback fired when a peer has connected.
        /// </summary>
        /// <param name="peer">The peer.</param>
        protected virtual void OnConnected(TPeer peer)
        {
        }

        /// <summary>
        /// Callback fired when a peer has disconnected.
        /// </summary>
        /// <param name="peer">The peer.</param>
        protected virtual void OnDisconnected(TPeer peer)
        {
        }

        /// <summary>
        /// Callback fired when the shim resets and disconnected from all peers.
        /// </summary>
        protected virtual void OnReset()
        {
        }

        /// <summary>
        /// Callback fired when the shim is starting. Subclasses may register additional pollable instances.
        /// </summary>
        /// <param name="poller">The poller.</param>
        protected virtual void Setup(NetMQPoller poller)
        {
        }

        /// <summary>
        /// Callback fired when the shim is stopping. Subclasses may dispose other items added by <see cref="Setup(NetMQPoller)"/>.
        /// </summary>
        protected virtual void TearDown()
        {
        }

        private void Connect(Guid id, string endpoint, string role)
        {
            var peer = GetOrConnectToPeer(id, endpoint, role);
            OnConnected(peer);
        }

        private void Disconnect(Guid id)
        {
            var peer = GetPeer(id);
            if (peer == null)
            {
                return;
            }

            peer.Dispose();
            _peers.Remove(id);

            OnDisconnected(peer);
        }

        private void HandleRouterReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            var identity = msg[0].ToIdentityGuid();
            var cmd = msg[1].ConvertToString();
            ClusterMessages.SendHeartbeat(Shim, identity);
            HandleRouterMessage(cmd, msg, identity);
        }

        private void HandleShimReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            var cmd = msg[0].ConvertToString();
            HandleShimMessage(cmd, msg);
        }
    }
}