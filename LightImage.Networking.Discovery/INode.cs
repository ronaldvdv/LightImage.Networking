using System;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Generic description of a node.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Gets the host to which the discovery beacon is bound.
        /// </summary>
        string Host { get; }

        /// <summary>
        /// Gets the unique identifier of the node.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets the descriptive name of the node.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the port where the discovery protocol is running.
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Gets the type of node.
        /// </summary>
        string Type { get; }
    }
}