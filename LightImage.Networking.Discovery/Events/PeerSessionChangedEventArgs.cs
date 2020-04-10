using System;

namespace LightImage.Networking.Discovery.Events
{
    /// <summary>
    /// Event arguments for the <see cref="DiscoveryNode.PeerSessionChanged"/> event.
    /// </summary>
    public class PeerSessionChangedEventArgs : PeerEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PeerSessionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="id">Unique identifier of the peer.</param>
        /// <param name="session">Session of the peer.</param>
        public PeerSessionChangedEventArgs(Guid id, int session)
            : base(id)
        {
            Session = session;
        }

        /// <summary>
        /// Gets the current session of the peer.
        /// </summary>
        public int Session { get; }
    }
}