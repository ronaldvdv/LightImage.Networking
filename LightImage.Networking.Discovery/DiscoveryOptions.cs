using System;
using System.Reflection;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Configuration for the discovery process.
    /// </summary>
    public class DiscoveryOptions
    {
        /// <summary>
        /// Configuration section for discovery settings.
        /// </summary>
        public const string C_CONFIG_SECTION = "discovery";

        /// <summary>
        /// Gets the default configuration for the discovery process.
        /// </summary>
        public static DiscoveryOptions Default { get; } = new DiscoveryOptions();

        /// <summary>
        /// Gets or sets the interval at which a beacon should be published.
        /// </summary>
        public TimeSpan BeaconInterval { get; set; } = TimeSpan.FromSeconds(0.5);

        /// <summary>
        /// Gets or sets the port at which the beacon must be published.
        /// </summary>
        /// <remarks>The setting *MUST* have the same value across all nodes to form a cluster.</remarks>
        public int BeaconPort { get; set; } = 5670;

        /// <summary>
        /// Gets or sets the interval after which, if no beacon has been received, a peer is considered evasive.
        /// </summary>
        public TimeSpan EvasiveThreshold { get; set; } = TimeSpan.FromSeconds(3.0);

        /// <summary>
        /// Gets or sets a value indicating whether the node should automatically join the session of another peer on the same host.
        /// </summary>
        public bool FollowLocally { get; set; } = false;

        /// <summary>
        /// Gets or sets the interval after which, if no beacon or PING-OK is received, a peer is considered lost.
        /// </summary>
        public TimeSpan LostThreshold { get; set; } = TimeSpan.FromSeconds(5.0);

        /// <summary>
        /// Gets or sets the interval for the timer that decides whether nodes are evasive/lost.
        /// </summary>
        public TimeSpan TimerInterval { get; set; } = TimeSpan.FromSeconds(0.5);
    }
}