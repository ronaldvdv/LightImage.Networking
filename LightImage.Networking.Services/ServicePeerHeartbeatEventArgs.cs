using System;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Event arguments for the <see cref="IService.PeerHeartbeat"/> event.
    /// </summary>
    public class ServicePeerHeartbeatEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePeerHeartbeatEventArgs"/> class.
        /// </summary>
        /// <param name="serviceName">Name of the service.</param>
        /// <param name="peerId">Unique identifier of the peer.</param>
        public ServicePeerHeartbeatEventArgs(string serviceName, Guid peerId)
        {
            ServiceName = serviceName;
            PeerId = peerId;
        }

        /// <summary>
        /// Gets the unique identifier of the peer that sent the heartbeat.
        /// </summary>
        public Guid PeerId { get; }

        /// <summary>
        /// Gets the name of the service that fired the event.
        /// </summary>
        public string ServiceName { get; }
    }
}