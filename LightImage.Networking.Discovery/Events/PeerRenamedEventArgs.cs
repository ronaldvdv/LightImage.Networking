using System;

namespace LightImage.Networking.Discovery.Events
{
    /// <summary>
    /// Event arguments for the <see cref="DiscoveryNode.PeerRenamed"/> event.
    /// </summary>
    public class PeerRenamedEventArgs : PeerEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PeerRenamedEventArgs"/> class.
        /// </summary>
        /// <param name="id">Unique identifier of the node.</param>
        /// <param name="name">Descriptive name of the node.</param>
        /// <param name="type">Technical type of the node.</param>
        public PeerRenamedEventArgs(Guid id, string name, string type)
            : base(id)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets the name of the node.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the node.
        /// </summary>
        public string Type { get; }
    }
}