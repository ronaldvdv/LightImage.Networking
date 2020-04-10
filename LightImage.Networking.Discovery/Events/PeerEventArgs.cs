using System;

namespace LightImage.Networking.Discovery.Events
{
    /// <summary>
    /// Base class for event arguments related to peer nodes.
    /// </summary>
    public class PeerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PeerEventArgs"/> class.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        public PeerEventArgs(Guid id)
        {
            Id = id;
        }

        /// <summary>
        /// Gets the unique identifier of the peer.
        /// </summary>
        public Guid Id { get; }
    }
}