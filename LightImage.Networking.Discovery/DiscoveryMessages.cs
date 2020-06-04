using System;
using System.Linq;
using LightImage.Networking.Services;
using LightImage.Util;
using LightImage.Util.Collections;
using NetMQ;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Utility class for sending messages for the discovery protocol.
    /// </summary>
    internal static class DiscoveryMessages
    {
        /// <summary>
        /// Add another node to the session.
        /// </summary>
        public const string C_CMD_ADD = "ADD";

        /// <summary>
        /// Mark peer as alive recently.
        /// </summary>
        public const string C_CMD_HEARTBEAT = "HEARTBEAT";

        /// <summary>
        /// Set current session.
        /// </summary>
        public const string C_CMD_SESSION = "SESSION";

        /// <summary>
        /// Graceful exit.
        /// </summary>
        public const string C_CMD_STOP = "STOP";

        /// <summary>
        /// Initialization has finished.
        /// </summary>
        public const string C_EVT_INIT = "INIT";

        /// <summary>
        /// Instruction to the node to join a session.
        /// </summary>
        /// <remarks>
        /// Only sent when another node has sent <see cref="C_MSG_JOIN"/> or when automatically following a peer.
        /// </remarks>
        public const string C_EVT_JOIN = "JOIN";

        /// <summary>
        /// Updated general status for a peer (found, evasive, lost).
        /// </summary>
        public const string C_EVT_PEER = "PEER";

        /// <summary>
        /// Received services available at peer.
        /// </summary>
        public const string C_EVT_SERVICES = "SERVICES";

        /// <summary>
        /// Please connect to me.
        /// </summary>
        public const string C_MSG_HELLO = "HELLO";

        /// <summary>
        /// Please join my session.
        /// </summary>
        public const string C_MSG_JOIN = "JOIN";

        /// <summary>
        /// Ping, please reply with PONG to show alive.
        /// </summary>
        public const string C_MSG_PING = "PING";

        /// <summary>
        /// Reply to PING.
        /// </summary>
        public const string C_MSG_PING_OK = "PING-OK";

        /// <summary>
        /// Private cache of <c>Guid</c> values encoded as byte arrays.
        /// </summary>
        private static readonly LruCache<Guid, byte[]> _guidFrames = new LruCache<Guid, byte[]>();

        /// <summary>
        /// Instruct the shim to add an existing peer to the session.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="peer">Unique identifier of the peer to be added.</param>
        public static void SendAddCommand(this IOutgoingSocket socket, Guid peer)
        {
            var peerBytes = _guidFrames.Get(peer, p => p.ToByteArray());
            socket.SendMoreFrame(C_CMD_ADD).SendFrame(peerBytes);
        }

        /// <summary>
        /// Instruct the shim to add an existing peer to the session.
        /// </summary>
        /// <param name="sender">Shim socket sender.</param>
        /// <param name="peer">Unique identifier of the peer to be added.</param>
        public static void SendAddCommand(this IOutgoingSender sender, Guid peer)
        {
            sender.Send(socket => socket.SendAddCommand(peer));
        }

        /// <summary>
        /// Instruct the shim to mark a peer as alive recently.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="peer">Unique identifier of the peer for which a heartbeat was received.</param>
        public static void SendHeartbeatCommand(this IOutgoingSocket socket, Guid peer)
        {
            var peerBytes = _guidFrames.Get(peer, p => p.ToByteArray());
            socket.SendMoreFrame(C_CMD_HEARTBEAT).SendFrame(peerBytes);
        }

        /// <summary>
        /// Instruct the shim to mark a peer as alive recently.
        /// </summary>
        /// <param name="sender">Shim socket sender.</param>
        /// <param name="peer">Unique identifier of the peer for which a heartbeat was received.</param>
        public static void SendHeartbeatCommand(this IOutgoingSender sender, Guid peer)
        {
            sender.Send(socket => socket.SendHeartbeatCommand(peer));
        }

        /// <summary>
        /// Send a HELLO message to a new peer.
        /// </summary>
        /// <param name="socket">Socket for sending peer messges to the receiver.</param>
        /// <param name="beaconData">Encoded beacon data of the sender.</param>
        /// <param name="name">Name of the sender.</param>
        /// <param name="host">Host of the sender.</param>
        /// <param name="type">Type of the sender.</param>
        /// <param name="forceHandshake">Value indicating whether a HELLO back shall be requested.</param>
        /// <param name="services">Services exposed by the sender.</param>
        public static void SendHelloMessage(this IOutgoingSocket socket, byte[] beaconData, string name, string host, string type, bool forceHandshake, IServiceDescription[] services)
        {
            socket.SendMoreFrame(C_MSG_HELLO);
            socket.SendMoreFrame(beaconData);
            socket.SendMoreFrame(name);
            socket.SendMoreFrame(host);
            socket.SendMoreFrame(type);
            socket.SendFrame(BitConverter.GetBytes(forceHandshake ? 1 : 0), services.Any());

            for (int i = services.Length - 1; i >= 0; i--)
            {
                var service = services[i];
                socket.SendFrame(new ServiceData(service.Name, service.Role, service.Ports, service.ClusterBehaviour).ToByteArray(), i > 0);
            }
        }

        /// <summary>
        /// Inform the service that the shim has finished initialization.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="host">Host to which the daemon is bound.</param>
        /// <param name="port">Port number where the service is listening for peers.</param>
        public static void SendInitEvent(this IOutgoingSocket socket, string host, int port)
        {
            socket.SendMoreFrame(C_EVT_INIT).SendMoreFrame(host).SendFrame(port);
        }

        /// <summary>
        /// Inform the service that the shim wants to join a session.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="session">Desired new session number.</param>
        /// <remarks>
        /// This event is typically raised because of FollowLocally behaviour. The service is in charge of choosing the
        /// session and needs to know the latest session at all times. Therefore, the shim raises this event which leads
        /// the service to send the SESSION command back to the shim and update its internal state simultaneously.
        /// </remarks>
        public static void SendJoinEvent(this IOutgoingSocket socket, int session)
        {
            socket.SendMoreFrame(C_EVT_JOIN).SendFrame(session);
        }

        /// <summary>
        /// Instructs a peer to join our session.
        /// </summary>
        /// <param name="socket">Peer socket.</param>
        public static void SendJoinMessage(this IOutgoingSocket socket)
        {
            socket.SendFrame(C_MSG_JOIN);
        }

        /// <summary>
        /// Informs the service that the status of a peer has changed.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="data">Data describing the peer status.</param>
        public static void SendPeerStatusEvent(this IOutgoingSocket socket, PeerStatusData data)
        {
            socket.SendMoreFrame(C_EVT_PEER).SendFrame(data.ToByteArray());
        }

        /// <summary>
        /// Send a PING message to a peer to check if it is still alive.
        /// </summary>
        /// <param name="socket">Peer socket.</param>
        public static void SendPingMessage(this IOutgoingSocket socket)
        {
            socket.SendFrame(C_MSG_PING);
        }

        /// <summary>
        /// Send a PING-OK message to a peer to inform that it's still alive.
        /// </summary>
        /// <param name="socket">Peer socket.</param>
        public static void SendPingOkMessage(this IOutgoingSocket socket)
        {
            socket.SendFrame(C_MSG_PING_OK);
        }

        /// <summary>
        /// Inform the service that a peer has sent information about available services.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="peer">Unique identifier of the peer.</param>
        /// <param name="services">Encoded service descriptions.</param>
        public static void SendServicesEvent(this IOutgoingSocket socket, Guid peer, NetMQFrame[] services)
        {
            var peerBytes = _guidFrames.Get(peer, p => p.ToByteArray());
            socket.SendMoreFrame(C_EVT_SERVICES).SendMoreFrame(peerBytes);
            for (int i = 0; i < services.Length; i++)
            {
                socket.SendFrame(services[i].ToByteArray(), i < services.Length - 1);
            }
        }

        /// <summary>
        /// Instruct the shim to change its session and update its beacon.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="session">New session number.</param>
        public static void SendSessionCommand(this IOutgoingSocket socket, int session)
        {
            socket.SendMoreFrame(C_CMD_SESSION).SendFrame(session);
        }

        /// <summary>
        /// Instruct the shim to change its session and update its beacon.
        /// </summary>
        /// <param name="sender">Shim socket sender.</param>
        /// <param name="session">New session number.</param>
        public static void SendSessionCommand(this IOutgoingSender sender, int session)
        {
            sender.Send(socket => socket.SendSessionCommand(session));
        }

        /// <summary>
        /// Instruct the shim to shut down.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        public static void SendStopCommand(this IOutgoingSocket socket)
        {
            socket.SendFrame(C_CMD_STOP);
        }
    }
}