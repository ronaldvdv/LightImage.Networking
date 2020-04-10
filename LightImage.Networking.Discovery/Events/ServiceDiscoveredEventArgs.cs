using System;
using LightImage.Networking.Services;

namespace LightImage.Networking.Discovery.Events
{
    /// <summary>
    /// Event arguments for the <see cref="DiscoveryNode.ServiceDiscovered"/> event.
    /// </summary>
    public class ServiceDiscoveredEventArgs : PeerEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceDiscoveredEventArgs"/> class.
        /// </summary>
        /// <param name="id">Unique identifier of the peer.</param>
        /// <param name="service">Description of the service exposed by the peer.</param>
        public ServiceDiscoveredEventArgs(Guid id, IServiceDescription service)
            : base(id)
        {
            Service = service;
        }

        /// <summary>
        /// Gets the description of the service exposed by the peer.
        /// </summary>
        public IServiceDescription Service { get; }
    }
}