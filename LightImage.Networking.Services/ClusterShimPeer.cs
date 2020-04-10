using System;
using NetMQ.Sockets;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Base class for tracking information about a peer of a node that runs in a setting where each
    /// node connects a dedicated DealerSocket with a shared RouterSocket in another node.
    /// </summary>
    public abstract class ClusterShimPeer : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClusterShimPeer"/> class.
        /// </summary>
        /// <param name="id">Unique identifier of the peer.</param>
        /// <param name="endpoint">Endpoint of the peer service.</param>
        /// <param name="me">Identifier of the owning node, for setting up the socket identity.</param>
        /// <param name="role">Role of the peer.</param>
        public ClusterShimPeer(Guid id, string endpoint, Guid me, string role)
        {
            Id = id;
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Host = NetworkOptions.GetIPAddress(endpoint);
            Role = role ?? throw new ArgumentNullException(nameof(role));
            Me = me;

            Dealer = new DealerSocket();
            Dealer.Options.Identity = Me.ToIdentity();
            Dealer.Connect(endpoint);
        }

        /// <summary>
        /// Gets the socket for sending messages to the peer.
        /// </summary>
        public DealerSocket Dealer { get; }

        /// <summary>
        /// Gets the endpoint at which the peer can be reached.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Gets the host name extracted from the endpoint.
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// Gets the unique identifier of the peer node.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the role of the peer within the service.
        /// </summary>
        public string Role { get; }

        /// <summary>
        /// Gets the unique identifier of the service itself.
        /// </summary>
        protected Guid Me { get; }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dealer.Disconnect(Endpoint);
            Dealer.Dispose();
        }
    }
}