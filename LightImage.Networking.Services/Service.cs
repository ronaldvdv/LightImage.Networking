using System;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Base class for any service managed by the discovery process.
    /// </summary>
    public abstract class Service : IService, IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Service"/> class.
        /// </summary>
        /// <param name="name">Name of the service.</param>
        /// <param name="role">Role of the node within this service.</param>
        public Service(string name, string role)
        {
            Name = name;
            Role = role;
        }

        /// <inheritdoc/>
        public event EventHandler<ServicePeerHeartbeatEventArgs> PeerHeartbeat;

        /// <summary>
        /// Gets the name of this service.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the ports at which this service is running.
        /// </summary>
        public int[] Ports { get; private set; }

        /// <summary>
        /// Gets the role of the node in this service.
        /// </summary>
        public string Role { get; }

        /// <inheritdoc/>
        public abstract void Add(Guid id, string host, string role, int[] ports);

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            Stop();
        }

        /// <inheritdoc/>
        public abstract void Remove(Guid id);

        /// <inheritdoc/>
        public abstract void Reset();

        /// <inheritdoc/>
        public abstract void Start();

        /// <inheritdoc/>
        public abstract void Stop();

        /// <summary>
        /// Fire the <see cref="PeerHeartbeat"/> event.
        /// </summary>
        /// <param name="id">Unique identifier of the peer.</param>
        protected void OnPeerHeartbeat(Guid id)
        {
            PeerHeartbeat?.Invoke(this, new ServicePeerHeartbeatEventArgs(Name, id));
        }

        /// <summary>
        /// Initializes the port numbers. An exception is thrown if this method is called more than once.
        /// </summary>
        /// <param name="ports">Port numbers.</param>
        protected void SetPorts(params int[] ports)
        {
            if (Ports != null)
            {
                throw new InvalidOperationException($"Ports can only be defined once");
            }

            Ports = ports;
        }
    }
}