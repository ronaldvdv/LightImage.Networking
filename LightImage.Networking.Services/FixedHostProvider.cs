using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Provider that always returns the same predetermined host.
    /// </summary>
    public class FixedHostProvider : IHostProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FixedHostProvider"/> class.
        /// </summary>
        /// <param name="host">The host name.</param>
        public FixedHostProvider(string host)
        {
            Host = host;
        }

        /// <summary>
        /// Gets the host name.
        /// </summary>
        public string Host { get; }

        /// <inheritdoc/>
        public string GetHost()
        {
            return Host;
        }
    }
}