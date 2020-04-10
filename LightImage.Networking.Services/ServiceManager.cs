using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace LightImage.Networking.Services
{
    /// <inheritdoc/>
    public class ServiceManager : IServiceManager
    {
        private readonly Logger<ServiceManager> _logger;
        private readonly Dictionary<string, IService> _services = new Dictionary<string, IService>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceManager"/> class.
        /// </summary>
        /// <param name="services">Services to be managed.</param>
        /// <param name="logger">Logger for writing diagnostics.</param>
        public ServiceManager(IEnumerable<IService> services, Logger<ServiceManager> logger = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _services = services.ToDictionary(service => service.Name);
            _logger = logger;

            foreach (var service in services)
            {
                service.PeerHeartbeat += HandlePeerHeartbeat;
            }
        }

        /// <inheritdoc/>
        public event EventHandler<ServicePeerHeartbeatEventArgs> PeerHeartbeat;

        /// <inheritdoc/>
        public bool IsRunning { get; private set; }

        /// <inheritdoc/>
        public void Connect(Guid peer, string service, string host, int[] ports, string role)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Cannot connect services without starting them.");
            }

            if (!_services.TryGetValue(service, out var own))
            {
                return;
            }

            _logger?.LogTrace(ServiceEvents.Connect, "Connecting {peer} to {service} at {host}:{ports}, role {role}", peer, service, host, ports, role);
            own.Add(peer, host, role, ports);
        }

        /// <inheritdoc/>
        public void Disconnect(Guid peer, string service)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Cannot connect services without starting them.");
            }

            if (!_services.TryGetValue(service, out var own))
            {
                return;
            }

            _logger?.LogTrace(ServiceEvents.Disconnect, "Disconnecting {peer} from {service}", peer, service);
            own.Remove(peer);
        }

        /// <inheritdoc/>
        public IServiceDescription[] GetDescriptors()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Cannot get service descriptors without starting them.");
            }

            return _services.Values.Select(service => (IServiceDescription)service).ToArray();
        }

        /// <inheritdoc/>
        public void Remove(Guid peer)
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Cannot remove peers when services are not running.");
            }

            foreach (var service in _services.Values)
            {
                service.Remove(peer);
                _logger?.LogTrace(ServiceEvents.Disconnect, "Disconnecting {peer} from {service}", peer, service.Name);
            }
        }

        /// <inheritdoc/>
        public void Reset()
        {
            if (!IsRunning)
            {
                throw new InvalidOperationException("Cannot reset services when they are not running.");
            }

            foreach (var service in _services.Values)
            {
                _logger?.LogTrace(ServiceEvents.Reset, "Resetting service {service}", service);
                service.Reset();
            }
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (IsRunning)
            {
                return;
            }

            foreach (var service in _services.Values)
            {
                service.Start();
                _logger?.LogInformation(ServiceEvents.Start, "Started service {name} with role {role} and ports {ports}", service.Name, service.Role, service.Ports);
            }

            IsRunning = true;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!IsRunning)
            {
                return;
            }

            foreach (var service in _services.Values)
            {
                _logger?.LogTrace(ServiceEvents.Stop, "Stopping service {service}", service.Name);
                service.Stop();
            }

            IsRunning = false;
        }

        private void HandlePeerHeartbeat(object sender, ServicePeerHeartbeatEventArgs e)
        {
            PeerHeartbeat?.Invoke(this, e);
        }
    }
}