using System;
using LightImage.Networking.Discovery.Events;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// The discovery node manages the discovery and clustering process of services.
    /// </summary>
    public interface IDiscoveryNode : INode, IDisposable
    {
        /// <summary>
        /// Indicates that the node has finished initialization.
        /// </summary>
        event EventHandler<EventArgs> Initialized;

        /// <summary>
        /// Indicates that a new peer was discovered.
        /// </summary>
        event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;

        /// <summary>
        /// Indicates that a peer is considered evasive since it does not publish daemons frequent enough.
        /// </summary>
        event EventHandler<PeerEventArgs> PeerEvasive;

        /// <summary>
        /// Indicates the a peer is considered lost since it does not respond to PING messages.
        /// </summary>
        event EventHandler<PeerEventArgs> PeerLost;

        /// <summary>
        /// Indicates that the information about a peer has been updated.
        /// </summary>
        event EventHandler<PeerRenamedEventArgs> PeerRenamed;

        /// <summary>
        /// Indicates that an evasive peer has reappeared.
        /// </summary>
        event EventHandler<PeerEventArgs> PeerReturned;

        /// <summary>
        /// Indicates that the session of a specific peer has changed.
        /// </summary>
        event EventHandler<PeerSessionChangedEventArgs> PeerSessionChanged;

        /// <summary>
        /// Indicates that a peer has published service information.
        /// </summary>
        event EventHandler<ServiceDiscoveredEventArgs> ServiceDiscovered;

        /// <summary>
        /// Indicates that the node has changed session.
        /// </summary>
        event EventHandler<SessionChangedEventArgs> SessionChanged;

        /// <summary>
        /// Add a specific peer to the current session.
        /// </summary>
        /// <param name="peer">Unique identifier of the peer.</param>
        void Add(Guid peer);

        /// <summary>
        /// Join a particular session.
        /// </summary>
        /// <param name="session">Session number.</param>
        void Join(int session);
    }
}