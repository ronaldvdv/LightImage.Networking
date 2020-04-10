using System;

namespace LightImage.Networking.Discovery.Events
{
    /// <summary>
    /// Event arguments for <see cref="DiscoveryNode.PeerDiscovered"/> event.
    /// </summary>
    public class PeerDiscoveredEventArgs : PeerEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PeerDiscoveredEventArgs"/> class.
        /// </summary>
        /// <param name="id">Unique peer identifier.</param>
        /// <param name="host">Host of the peer.</param>
        /// <param name="port">Port number of the peer discovery service.</param>
        /// <param name="name">Descriptive name of the peer.</param>
        /// <param name="type">Technical type of the peer.</param>
        /// <param name="session">Session number.</param>
        public PeerDiscoveredEventArgs(Guid id, string host, int port, string name, string type, int session)
            : base(id)
        {
            Name = name;
            Type = type;
            Session = session;
            Host = host;
            Port = port;
        }

        /// <summary>
        /// Gets the host of the peer.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the descriptive name of the peer.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the port number of the discovery service at the peer.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the current session number of the peer.
        /// </summary>
        public int Session { get; }

        /// <summary>
        /// Gets the technical type of the peer.
        /// </summary>
        public string Type { get; }
    }
}