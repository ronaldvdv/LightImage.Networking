namespace LightImage.Networking.Services
{
    /// <summary>
    /// Descriptor of a service.
    /// </summary>
    public interface IServiceDescription
    {
        /// <summary>
        /// Gets the clustering behaviour of the service.
        /// </summary>
        ServiceClusterBehaviour ClusterBehaviour { get; }

        /// <summary>
        /// Gets the unique name that can be used to recognize the service.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the ports used by the service within a specific node.
        /// </summary>
        int[] Ports { get; }

        /// <summary>
        /// Gets the role of the node within the service.
        /// </summary>
        string Role { get; }
    }
}