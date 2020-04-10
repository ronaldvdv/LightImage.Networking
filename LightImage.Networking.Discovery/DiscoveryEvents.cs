using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LightImage.Networking.Discovery.Tests")]

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Logging events for the discovery process.
    /// </summary>
    public static class DiscoveryEvents
    {
        /// <summary>
        /// A peer was added.
        /// </summary>
        public const int AddPeer = 1;

        /// <summary>
        /// The node is initializing.
        /// </summary>
        public const int Initialize = 4;

        /// <summary>
        /// A peer entered the current session.
        /// </summary>
        public const int PeerEnteredSession = 7;

        /// <summary>
        /// A peer left the current session.
        /// </summary>
        public const int PeerLeftSession = 8;

        /// <summary>
        /// Peer metadata or status has changed.
        /// </summary>
        public const int PeerUpdated = 6;

        /// <summary>
        /// A peer was removed.
        /// </summary>
        public const int RemovePeer = 2;

        /// <summary>
        /// A service was received from a peer.
        /// </summary>
        public const int ServiceReceived = 3;

        /// <summary>
        /// The node is running.
        /// </summary>
        public const int Started = 5;
    }
}