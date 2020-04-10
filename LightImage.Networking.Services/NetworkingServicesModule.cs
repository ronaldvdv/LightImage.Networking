using Autofac;
using Microsoft.Extensions.Configuration;
using NetMQ;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Autofac module for the services functionality.
    /// </summary>
    public class NetworkingServicesModule : Module
    {
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkingServicesModule"/> class.
        /// </summary>
        /// <param name="config">Configuration.</param>
        public NetworkingServicesModule(IConfiguration config)
        {
            _config = config;

            // Use an ArrayPool as the IBufferPool for NetMQ.
            var bufferPool = new ArrayPoolBufferPool();
            BufferPool.SetCustomBufferPool(bufferPool);
        }

        /// <inheritdoc/>
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ServiceManager>().As<IServiceManager>().SingleInstance();
            builder.Configure<NetworkOptions>(_config, NetworkOptions.C_CONFIG_SECTION);
        }
    }
}