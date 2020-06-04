using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;
using NetMQ;
using NetMQ.Sockets;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Shim handler for the actor running the discovery process.
    /// </summary>
    public partial class DiscoveryShim : IShimHandler
    {
        private readonly ILogger<DiscoveryShim> _logger;

        /// <summary>
        /// Descriptive name for this node (typically the software component).
        /// </summary>
        private readonly string _name;

        private readonly DiscoveryOptions _options;

        /// <summary>
        /// Cache of currently connected peers.
        /// </summary>
        private readonly Dictionary<Guid, Peer> _peers = new Dictionary<Guid, Peer>();

        /// <summary>
        /// Descriptions of services.
        /// </summary>
        private readonly IServiceDescription[] _services;

        /// <summary>
        /// Type of this node.
        /// </summary>
        private readonly string _type;

        /// <summary>
        /// UDP beacon publishing our router port and unique ID.
        /// </summary>
        private NetMQBeacon _beacon;

        /// <summary>
        /// Sequence number for the beacon, so old beacons can be ignored.
        /// </summary>
        private int _beaconSequence = 0;

        /// <summary>
        /// Unique identifier for the node.
        /// </summary>
        private Guid _id;

        /// <summary>
        /// Poller orchestrating incoming messages.
        /// </summary>
        private NetMQPoller _poller;

        /// <summary>
        /// Router that listens to incoming messages from peers.
        /// </summary>
        private RouterSocket _router;

        /// <summary>
        /// The port on which the Router is listening.
        /// </summary>
        private int _routerPort;

        private MessageQueueSender _sender;

        /// <summary>
        /// Identifier for our current session.
        /// </summary>
        private int _session;

        private PairSocket _shim;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryShim"/> class.
        /// </summary>
        /// <param name="id">Unique identifier of the node.</param>
        /// <param name="name">Descriptive name of the node.</param>
        /// <param name="type">Type of the node.</param>
        /// <param name="services">Description of services to be published.</param>
        /// <param name="logger">Logger for the disovery shim.</param>
        /// <param name="options">Discovery configuration.</param>
        public DiscoveryShim(Guid id, string name, string type, IServiceDescription[] services, ILogger<DiscoveryShim> logger, DiscoveryOptions options = null)
        {
            _id = id;
            _name = name;
            _type = type;
            _services = services ?? new IServiceDescription[] { };
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? DiscoveryOptions.Default;
        }

        /// <summary>
        /// Gets the host to which the beacon is bound.
        /// </summary>
        public string Host => _beacon.BoundTo;

        /// <inheritdoc/>
        public void Run(PairSocket shim)
        {
            _shim = shim;

            _logger.LogTrace(DiscoveryEvents.Initialize, "Shim is starting on thread {thread}", Thread.CurrentThread.ManagedThreadId);

            _router = new RouterSocket();

            // TODO Use NetworkOptions.Host here
            _routerPort = _router.BindRandomPort("tcp://*");
            _router.ReceiveReady += HandleRouterReceiveReady;

            _beacon = new NetMQBeacon();
            _beacon.Configure(_options.BeaconPort); // TODO Use interface from NetworkOptions.Host
            _beacon.Subscribe(string.Empty);
            _beacon.ReceiveReady += HandleBeaconReceived;
            PublishBeacon();

            var timer = new NetMQTimer(_options.TimerInterval);
            timer.Elapsed += HandleTimerElapsed;

            shim.ReceiveReady += HandleShimReceiveReady;
            shim.SignalOK();

            _sender = new MessageQueueSender(_shim);

            _poller = new NetMQPoller { _router, _beacon, _shim, _sender, timer };

            _sender.SendInitEvent(Host, _routerPort);

            _poller.Run();

            DisconnectPeers();
            _poller.Dispose();
            _sender.Dispose();
            _beacon.Dispose();
            _router.Dispose();
        }

        private void AddPeer(Guid id)
        {
            var peer = _peers[id];
            peer.SendJoin();
        }

        private void ConsiderFollow(Peer peer)
        {
            if (_session > 0)
            {
                return;
            }

            if (!_options.FollowLocally)
            {
                return;
            }

            if (peer.Session <= 0)
            {
                return;
            }

            if (!string.Equals(peer.Host, Host, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            _sender.SendJoinEvent(peer.Session);
        }

        private void Disconnect(Peer peer)
        {
            _peers.Remove(peer.Id);
            peer.Dispose();
            _sender.SendPeerStatusEvent(new PeerStatusData(peer.Id, null, 0, null, null, DiscoveryNode.C_NO_SESSION, PeerStatus.Lost));
        }

        private void DisconnectPeers()
        {
            foreach (var peer in _peers.Values)
            {
                peer.Dispose();
            }
        }

        private Peer FindPeer(NetMQFrame identity)
        {
            var buffer = new byte[16];
            Array.Copy(identity.Buffer, 1, buffer, 0, 16);
            var id = new Guid(buffer);
            return FindPeer(id);
        }

        private Peer FindPeer(Guid id)
        {
            if (_peers.TryGetValue(id, out var peer))
            {
                return peer;
            }

            return null;
        }

        private byte[] GetBeaconData()
        {
            return new BeaconData(_id, _routerPort, _session, _beaconSequence).ToByteArray();
        }

        private void GotoSession(int session)
        {
            if (_session == session)
            {
                return;
            }

            _session = session;
            _beaconSequence++;
            PublishBeacon();
        }

        private void HandleBeaconReceived(object sender, NetMQBeaconEventArgs e)
        {
            var msg = _beacon.Receive();
            var data = BeaconData.Parse(msg.Bytes);
            if (data.Id == _id)
            {
                return;
            }

            ProcessBeacon(data, msg.PeerHost);
        }

        private void HandleRouterReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            var identity = msg[0];
            var peer = FindPeer(identity);

            switch (msg[1].ConvertToString())
            {
                // Make sure we've registered this peer; respond with PONG to show we're still alive
                case DiscoveryMessages.C_MSG_PING:
                    if (peer != null)
                    {
                        peer.SendPingOk();
                    }

                    break;

                // Make sure we've registered this peer
                case DiscoveryMessages.C_MSG_HELLO:
                    var beacon = BeaconData.Parse(msg[2].Buffer);
                    var name = msg[3].ConvertToString();
                    var host = msg[4].ConvertToString();
                    var type = msg[5].ConvertToString();
                    var forceHandshake = BitConverter.ToInt32(msg[6].Buffer, 0) == 1;
                    peer = ProcessBeacon(beacon, host, name, type, forceHandshake);

                    var services = msg.Skip(7).ToArray();
                    _sender.SendServicesEvent(peer.Id, services);
                    break;

                // Join a session
                case DiscoveryMessages.C_MSG_JOIN:
                    if (peer == null)
                    {
                        throw new InvalidOperationException($"{DiscoveryMessages.C_MSG_JOIN} from unknown peer");
                    }

                    var session = peer.Session;
                    if (session != DiscoveryNode.C_NO_SESSION)
                    {
                        _sender.SendJoinEvent(session);
                    }

                    break;
            }
        }

        private void HandleShimReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            var msg = e.Socket.ReceiveMultipartMessage();
            switch (msg[0].ConvertToString())
            {
                // Quit actor shim
                case NetMQActor.EndShimMessage:
                    _poller.Stop();
                    break;

                // Replace session ID and start broadcasting updated beacon
                case DiscoveryMessages.C_CMD_SESSION:
                    GotoSession(msg[1].ConvertToInt32());
                    break;

                // Add another node to the session
                case DiscoveryMessages.C_CMD_ADD:
                    var id = msg[1].Buffer.ToGuid();
                    AddPeer(id);
                    break;

                // Stop gracefully, notifying peers
                case DiscoveryMessages.C_CMD_STOP:
                    Stop();
                    break;

                // Mark peer alive
                case DiscoveryMessages.C_CMD_HEARTBEAT:
                    id = msg[1].Buffer.ToGuid();
                    MarkPeerAlive(id);
                    break;
            }
        }

        private void HandleTimerElapsed(object sender, NetMQTimerEventArgs e)
        {
            DateTime now = DateTime.Now;

            foreach (var peer in _peers.Values.ToArray())
            {
                if (peer.LastSeen < now - _options.LostThreshold)
                {
                    Disconnect(peer);
                }
                else if (peer.LastSeen < now - _options.EvasiveThreshold && peer.Status == PeerStatus.Alive)
                {
                    peer.SetEvasive();
                    _sender.SendPeerStatusEvent(new PeerStatusData(peer.Id, peer.Host, peer.Port, peer.Name, peer.Type, peer.Session, PeerStatus.Evasive));
                }
            }
        }

        private void MarkPeerAlive(Guid id)
        {
            var peer = FindPeer(id);
            if (peer != null)
            {
                var evasive = peer.Status == PeerStatus.Evasive;
                peer.MarkAlive();
                if (evasive)
                {
                    _sender.SendPeerStatusEvent(new PeerStatusData(peer.Id, peer.Host, peer.Port, peer.Name, peer.Type, peer.Session, PeerStatus.Alive));
                }
            }
        }

        private Peer ProcessBeacon(BeaconData data, string host, string name = null, string type = null, bool forceHandshake = false)
        {
            var peer = FindPeer(data.Id);
            var sendEvent = false;
            if (peer != null)
            {
                if (data.Port > 0)
                {
                    ConsiderFollow(peer);
                    sendEvent = peer.Update(host, data.Port, data.Session, data.Sequence, name ?? peer.Name, type ?? peer.Type, forceHandshake: forceHandshake);
                }
                else
                {
                    Disconnect(peer);
                }
            }
            else if (data.Port > 0)
            {
                peer = _peers[data.Id] = new Peer(data.Id, host, data.Port, data.Session, data.Sequence, name, type, this);
                ConsiderFollow(peer);
                sendEvent = true;
            }

            if (sendEvent)
            {
                _sender.SendPeerStatusEvent(new PeerStatusData(data.Id, host, data.Port, peer.Name, peer.Type, data.Session, PeerStatus.Alive));
            }

            return peer;
        }

        private void PublishBeacon()
        {
            var data = GetBeaconData();
            _beacon.Publish(data, _options.BeaconInterval);
        }

        private void SendHello(IOutgoingSocket socket, bool forceHandshake)
        {
            DiscoveryMessages.SendHelloMessage(socket, GetBeaconData(), _name, Host, _type, forceHandshake, _services);
        }

        private void Stop()
        {
            _routerPort = 0;
            PublishBeacon();
            Thread.Sleep(1);
            _poller.Stop();
        }
    }
}