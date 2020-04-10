using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// General networking options.
    /// </summary>
    public class NetworkOptions
    {
        /// <summary>
        /// Configuration section for general network settings.
        /// </summary>
        public const string C_CONFIG_SECTION = "network";

        private static readonly Regex _ipAddressPattern = new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
        private string _host = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkOptions"/> class with default host.
        /// </summary>
        public NetworkOptions()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkOptions"/> class with a specific host.
        /// </summary>
        /// <param name="host">Host name.</param>
        public NetworkOptions(string host)
        {
            _host = host;
            Component = Type = DefaultName;
        }

        /// <summary>
        /// Gets the default name for the node, which is based on the assembly name.
        /// </summary>
        public static string DefaultName
        {
            get
            {
                var assembly = Assembly.GetEntryAssembly();
                if (assembly?.GetName() == null)
                {
                    return "-";
                }

                return assembly.GetName().Name;
            }
        }

        /// <summary>
        /// Gets or sets the descriptive name of this component.
        /// </summary>
        public string Component { get; set; }

        /// <summary>
        /// Gets the host name for this node.
        /// </summary>
        public string Host
        {
            get
            {
                if (_host == null)
                {
                    _host = GetHost();
                }

                return _host;
            }
        }

        /// <summary>
        /// Gets the unique identifier of this node.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Gets the IP address of this node.
        /// </summary>
        /// <remarks>
        /// The address matches the host without any protocol or slashes.
        /// </remarks>
        public string IPAddress
        {
            get
            {
                return GetIPAddress(Host);
            }
        }

        /// <summary>
        /// Gets or sets the technical name for the type of node.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Parse the IP address part from a given host or address (e.g. protocol and/or port).
        /// </summary>
        /// <param name="endpoint">Endpoint to be parsed.</param>
        /// <returns>IP address in numeric format.</returns>
        public static string GetIPAddress(string endpoint)
        {
            var match = _ipAddressPattern.Match(endpoint);
            if (!match.Success)
            {
                return null;
            }

            return match.Value;
        }

        private string GetHost()
        {
            var adapter = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(a => a.OperationalStatus == OperationalStatus.Up);
            if (adapter == null)
            {
                throw new InvalidOperationException($"No network interface found");
            }

            var address = adapter.GetIPProperties().UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
            if (address == null)
            {
                throw new InvalidOperationException($"No IPv4 address found");
            }

            return address.Address.ToString();
        }
    }
}