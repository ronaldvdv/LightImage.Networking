using System;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Abstract base class for all classes describing nodes.
    /// </summary>
    public abstract class AbstractNode : INode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractNode"/> class.
        /// </summary>
        /// <param name="id">Unique identifier.</param>
        /// <param name="name">Descriptive name.</param>
        /// <param name="type">Technical component type.</param>
        /// <param name="host">Host name.</param>
        /// <param name="port">Port number.</param>
        protected AbstractNode(Guid id, string name, string type, string host, int port)
        {
            Id = id;
            Name = name;
            Type = type;
            Host = host;
            Port = port;
        }

        /// <inheritdoc/>
        public string Host { get; internal set; }

        /// <inheritdoc/>
        public Guid Id { get; }

        /// <inheritdoc/>
        public string Name { get; protected set; }

        /// <inheritdoc/>
        public int Port { get; protected set; }

        /// <inheritdoc/>
        public string Type { get; protected set; }
    }
}