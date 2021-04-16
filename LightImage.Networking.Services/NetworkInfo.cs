using System;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// General information about the network.
    /// </summary>
    public class NetworkInfo
    {
        private static readonly Regex _ipAddressPattern = new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
        private readonly IHostProvider _hostProvider;
        private string _host = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkInfo"/> class.
        /// </summary>
        /// <param name="options">Fixed network configuration.</param>
        /// <param name="hostProvider">Service for determining the host.</param>
        public NetworkInfo(NetworkOptions options, IHostProvider hostProvider)
        {
            Options = options;
            _hostProvider = hostProvider;
        }

        /// <summary>
        /// Gets the unique identifier of this node.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        /// <summary>
        /// Gets the host name for this node.
        /// </summary>
        public string Host
        {
            get
            {
                if (_host == null)
                {
                    _host = _hostProvider.GetHost();
                }

                return _host;
            }
        }


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
        /// Gets the network configuration.
        /// </summary>
        public NetworkOptions Options { get; }


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
    }
}