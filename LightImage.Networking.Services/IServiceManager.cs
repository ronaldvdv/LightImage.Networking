using System;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Manager for starting, stopping, connecting and disconnecting a collection of services.
    /// </summary>
    public interface IServiceManager
    {
        /// <summary>
        /// Event fired when data has been received from a peer.
        /// </summary>
        event EventHandler<ServicePeerHeartbeatEventArgs> PeerHeartbeat;

        /// <summary>
        /// Gets a value indicating whether the services are running or not.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Connect a service with a peer.
        /// </summary>
        /// <param name="peer">Unique ID of the peer.</param>
        /// <param name="service">Name of the service.</param>
        /// <param name="host">Host where the peer can be found.</param>
        /// <param name="ports">Ports where the peer is listening.</param>
        /// <param name="role">Role of the remote service.</param>
        void Connect(Guid peer, string service, string host, int[] ports, string role);

        /// <summary>
        /// Disconnect a service from a peer.
        /// </summary>
        /// <param name="peer">Unique ID of the peer.</param>
        /// <param name="service">Name of the service.</param>
        void Disconnect(Guid peer, string service);

        /// <summary>
        /// Gets descriptions of all services.
        /// </summary>
        /// <returns>Collection of services.</returns>
        IServiceDescription[] GetDescriptors();

        /// <summary>
        /// Remove a peer from all services.
        /// </summary>
        /// <param name="id">Unique identifier of the peer.</param>
        void Remove(Guid id);

        /// <summary>
        /// Reset the state of all services, disconnecting all their peers.
        /// </summary>
        /// <param name="includeGlobalServices">Value indicating whether services with <see cref="IServiceDescription.ClusterBehaviour"/> <see cref="ServiceClusterBehaviour.Global"/> should also reset.</param>
        void Reset(bool includeGlobalServices = false);

        /// <summary>
        /// Start all services.
        /// </summary>
        void Start();

        /// <summary>
        /// Stop all services.
        /// </summary>
        void Stop();
    }
}