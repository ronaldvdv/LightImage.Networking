using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Default implementation of <see cref="IHostProvider"/> picking a random, operational network adapter.
    /// </summary>
    public class DefaultHostProvider : IHostProvider
    {
        /// <inheritdoc/>
        public string GetHost()
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