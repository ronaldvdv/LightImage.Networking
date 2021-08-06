using System.Runtime.CompilerServices;
using Autofac;
using Microsoft.Extensions.Configuration;

[assembly: InternalsVisibleTo("LightImage.Networking.Discovery.Tests")]

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Autofac module for the discovery process.
    /// </summary>
    public class DiscoveryModule : Module
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryModule"/> class.
        /// </summary>
        /// <param name="config">Configuration.</param>
        public DiscoveryModule(IConfiguration config)
        {
            _config = config;
        }

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);
            builder.RegisterType<DiscoveryNode>().As<IDiscoveryNode>().SingleInstance();
            builder.Configure<DiscoveryOptions>(_config, DiscoveryOptions.C_CONFIG_SECTION);
        }
    }
}