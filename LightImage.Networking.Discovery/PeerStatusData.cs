using System;
using System.IO;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Data about the current status of a peer, used for communication between <see cref="DiscoveryNode"/> and <see cref="DiscoveryShim"/>.
    /// </summary>
    internal class PeerStatusData : INode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PeerStatusData"/> class.
        /// </summary>
        /// <param name="id">Unique identifier of the peer.</param>
        /// <param name="host">Host at which the peer can be reached.</param>
        /// <param name="port">Port at which the discovery service runs at the peer.</param>
        /// <param name="name">Descriptive name of the peer.</param>
        /// <param name="type">Technical type of peer.</param>
        /// <param name="session">Current session of the peer.</param>
        /// <param name="status">Current connectivity status of the peer.</param>
        public PeerStatusData(Guid id, string host, int port, string name, string type, int session, PeerStatus status)
        {
            Id = id;
            Host = host;
            Port = port;
            Name = name;
            Type = type;
            Session = session;
            Status = status;
        }

        /// <inheritdoc/>
        public string Host { get; }

        /// <inheritdoc/>
        public Guid Id { get; }

        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public int Port { get; }

        /// <summary>
        /// Gets the current session of the peer.
        /// </summary>
        public int Session { get; }

        /// <summary>
        /// Gets the current connectivity status of the peer.
        /// </summary>
        public PeerStatus Status { get; }

        /// <inheritdoc/>
        public string Type { get; }

        /// <summary>
        /// Parses a <see cref="PeerStatusData"/> from an encoded byte array.
        /// </summary>
        /// <param name="data">Data to be decoded.</param>
        /// <returns>The peer status.</returns>
        public static PeerStatusData Parse(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(stream))
                {
                    var id = new Guid(reader.ReadBytes(16));
                    var host = reader.ReadString();
                    var port = reader.ReadInt32();
                    var name = reader.ReadString();
                    var type = reader.ReadString();
                    var session = reader.ReadInt32();
                    var status = (PeerStatus)reader.ReadInt32();
                    return new PeerStatusData(id, host, port, name, type, session, status);
                }
            }
        }

        /// <summary>
        /// Converts the data to a byte array.
        /// </summary>
        /// <returns>Peer data as a byte array.</returns>
        public byte[] ToByteArray()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(Id.ToByteArray());
                    writer.Write(Host ?? string.Empty);
                    writer.Write(Port);
                    writer.Write(Name ?? string.Empty);
                    writer.Write(Type ?? string.Empty);
                    writer.Write(Session);
                    writer.Write((int)Status);
                    return stream.ToArray();
                }
            }
        }
    }
}