using System;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Data about a peer to which a <see cref="ClusterService{TShim, TShimPeer}"/> is connected.
    /// </summary>
    public class ServicePeer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServicePeer"/> class.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="host">Host of the peer.</param>
        /// <param name="role">Role of the peer.</param>
        /// <param name="ports">Ports exposed by the peer.</param>
        public ServicePeer(Guid id, string host, string role, int[] ports)
        {
            Id = id;
            Host = host;
            Role = role;
            Ports = ports;
        }

        /// <summary>
        /// Gets the host of the peer.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the unique identifier of the peer.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the ports at which the peer is running the service.
        /// </summary>
        public int[] Ports { get; }

        /// <summary>
        /// Gets the role of the peer within the service.
        /// </summary>
        public string Role { get; }
    }
}