using System;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Services are building blocks that let multiple nodes within a session cooperate on a specific task.
    /// </summary>
    public interface IService : IServiceDescription
    {
        /// <summary>
        /// Event fired when data has been received from a peer.
        /// </summary>
        event EventHandler<ServicePeerHeartbeatEventArgs> PeerHeartbeat;

        /// <summary>
        /// Include a peer that just joined the same session.
        /// </summary>
        /// <param name="id">Unique identifier for the node.</param>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="role">Role of the peer within the service.</param>
        /// <param name="ports">Sorted collection of ports used by the peer for running the service.</param>
        void Add(Guid id, string host, string role, int[] ports);

        /// <summary>
        /// Remove a peer that left the session.
        /// </summary>
        /// <param name="id">Unique ID of the peer.</param>
        void Remove(Guid id);

        /// <summary>
        /// Remove all peers and reset service state when switching session.
        /// </summary>
        void Reset();

        /// <summary>
        /// Start the service and define port number(s).
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the service.
        /// </summary>
        void Stop();
    }
}