using System.IO;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Static descriptor of a service available at a node. This class is used for exchanging
    /// information between shim and service, about available services at peers.
    /// </summary>
    public class ServiceData : IServiceDescription
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceData"/> class.
        /// </summary>
        /// <param name="name">Name of the service.</param>
        /// <param name="role">Role of the node in this service.</param>
        /// <param name="ports">Ports at which the service is running.</param>
        /// <param name="clusterBehaviour">Clustering behaviour of the service.</param>
        public ServiceData(string name, string role, int[] ports, ServiceClusterBehaviour clusterBehaviour)
        {
            Name = name ?? string.Empty;
            Role = role ?? string.Empty;
            Ports = ports ?? new int[] { };
            ClusterBehaviour = clusterBehaviour;
        }

        /// <inheritdoc/>
        public ServiceClusterBehaviour ClusterBehaviour { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public int[] Ports { get; }

        /// <inheritdoc/>
        public string Role { get; }

        /// <summary>
        /// Parses the service data from a byte array.
        /// </summary>
        /// <param name="data">Encoded service data.</param>
        /// <returns>The parsed service data.</returns>
        public static ServiceData Parse(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var name = reader.ReadString();
                    var role = reader.ReadString();
                    var clusterBehaviour = (ServiceClusterBehaviour)reader.ReadInt32();
                    var portCount = reader.ReadByte();
                    var ports = new int[portCount];
                    for (var i = 0; i < portCount; i++)
                    {
                        ports[i] = reader.ReadInt32();
                    }

                    return new ServiceData(name, role, ports, clusterBehaviour);
                }
            }
        }

        /// <summary>
        /// Converts the service data to a byte array.
        /// </summary>
        /// <returns>The encoded service data.</returns>
        public byte[] ToByteArray()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(Name);
                    writer.Write(Role);
                    writer.Write((int)ClusterBehaviour);
                    writer.Write((byte)Ports.Length);
                    foreach (var port in Ports)
                    {
                        writer.Write(port);
                    }

                    return stream.ToArray();
                }
            }
        }
    }
}