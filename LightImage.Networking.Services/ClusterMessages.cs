using System;
using NetMQ;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Message sending and parsing for clusters.
    /// </summary>
    public static class ClusterMessages
    {
        /// <summary>
        /// Instruct the shim to connect to a particular peer.
        /// </summary>
        public const string C_CMD_CONNECT = "CONNECT";

        /// <summary>
        /// Instruct the shim to disconnect from a particular peer.
        /// </summary>
        public const string C_CMD_DISCONNECT = "DISCONNECT";

        /// <summary>
        /// Instruct the shim to reset its state and disconnect all peers.
        /// </summary>
        public const string C_CMD_RESET = "RESET";

        /// <summary>
        /// Inform the service that a peer is alive.
        /// </summary>
        public const string C_EVT_HEARTBEAT = "HEARTBEAT";

        /// <summary>
        /// Send a HELLO message to a peer.
        /// </summary>
        public const string C_MSG_HELLO = "HELLO";

        /// <summary>
        /// Parse a CONNECT message.
        /// </summary>
        /// <param name="msg">Message to be parsed.</param>
        /// <param name="id">Unique identifier of the peer.</param>
        /// <param name="endpoint">Endpoint of the peer.</param>
        /// <param name="role">Role of the peer within the service.</param>
        /// <param name="offset">Index of the CONNECT command frame within the message.</param>
        public static void ParseConnect(NetMQMessage msg, out Guid id, out string endpoint, out string role, int offset = 0)
        {
            id = new Guid(msg[offset + 1].Buffer);
            endpoint = msg[offset + 2].ConvertToString();
            role = msg[offset + 3].ConvertToString();
        }

        /// <summary>
        /// Parse a DISCONNECT message.
        /// </summary>
        /// <param name="msg">Message to be parsed.</param>
        /// <param name="id">Unique identifier of the peer.</param>
        /// <param name="offset">Index of the DISCONNECT command frame within the message.</param>
        public static void ParseDisconnect(NetMQMessage msg, out Guid id, int offset = 0)
        {
            id = new Guid(msg[offset + 1].Buffer);
        }

        /// <summary>
        /// Parse a HEARTBEAT message.
        /// </summary>
        /// <param name="msg">Message to be parsed.</param>
        /// <param name="nodeId">Unique identifier of the peer.</param>
        /// <param name="offset">Index of the HEARTBEAT event frame within the message.</param>
        public static void ParseHeartbeat(NetMQMessage msg, out Guid nodeId, int offset = 0)
        {
            nodeId = new Guid(msg[offset + 1].Buffer);
        }

        /// <summary>
        /// Parse a HELLO message.
        /// </summary>
        /// <param name="msg">Message to be parsed.</param>
        /// <param name="endpoint">Endpoint of the peer.</param>
        /// <param name="role">Role of the peer within the service.</param>
        /// <param name="offset">Index of the HELLO command frame within the message.</param>
        public static void ParseHello(NetMQMessage msg, out string endpoint, out string role, int offset = 1)
        {
            endpoint = msg[offset + 1].ConvertToString();
            role = msg[offset + 2].ConvertToString();
        }

        /// <summary>
        /// Send a CONNECT message to the shim.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="id">Unique identifier of the peer.</param>
        /// <param name="endpoint">Endpoint of the peer.</param>
        /// <param name="role">Role of the peer within the service.</param>
        public static void SendConnect(IOutgoingSocket socket, Guid id, string endpoint, string role)
        {
            socket.SendMoreFrame(C_CMD_CONNECT);
            socket.SendMoreFrame(id.ToByteArray());
            socket.SendMoreFrame(endpoint);
            socket.SendFrame(role ?? string.Empty);
        }

        /// <summary>
        /// Send a DISCONNECT message to the shim.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="id">Unique identifier of the peer.</param>
        public static void SendDisconnect(IOutgoingSocket socket, Guid id)
        {
            socket.SendMoreFrame(C_CMD_DISCONNECT);
            socket.SendFrame(id.ToByteArray());
        }

        /// <summary>
        /// Send a HEARTBEAT event to the service.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        /// <param name="nodeId">Unique identifier of the peer node.</param>
        public static void SendHeartbeat(IOutgoingSocket socket, Guid nodeId)
        {
            socket.SendMoreFrame(C_EVT_HEARTBEAT);
            socket.SendFrame(nodeId.ToByteArray());
        }

        /// <summary>
        /// Send a HELLO message to a peer.
        /// </summary>
        /// <param name="socket">Peer socket.</param>
        /// <param name="endpoint">Endpoint of the owning (sending) node.</param>
        /// <param name="role">Role of the owning (sending) node.</param>
        public static void SendHello(IOutgoingSocket socket, string endpoint, string role)
        {
            socket.SendMoreFrame(C_MSG_HELLO);
            socket.SendMoreFrame(endpoint);
            socket.SendFrame(role);
        }

        /// <summary>
        /// Send a RESET message to the shim.
        /// </summary>
        /// <param name="socket">Shim socket.</param>
        public static void SendReset(IOutgoingSocket socket)
        {
            socket.SendFrame(C_CMD_RESET);
        }
    }
}