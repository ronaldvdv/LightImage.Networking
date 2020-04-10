using System;
using System.IO;
using System.Linq;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Data for the discovery beacon.
    /// </summary>
    internal class BeaconData
    {
        private const byte C_VERSION = 1;
        private static readonly char[] C_HEADER = new char[] { 'G', 'R', 'P', 'H' };

        /// <summary>
        /// Initializes a new instance of the <see cref="BeaconData"/> class.
        /// </summary>
        /// <param name="id">Unique identifier of the node.</param>
        /// <param name="port">Port where the node is listening for incoming messages.</param>
        /// <param name="session">Current session of the node.</param>
        /// <param name="sequence">Monotonically increasing sequence number which is used as a version of the beacon.</param>
        public BeaconData(Guid id, int port, int session, int sequence)
        {
            Id = id;
            Port = port;
            Session = session;
            Sequence = sequence;
        }

        /// <summary>
        /// Gets the unique identifier of the node.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// Gets the port number where the node is listening for incoming messages.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the monotonically increasing sequence number which is used as a version of the beacon.
        /// </summary>
        public int Sequence { get; }

        /// <summary>
        /// Gets the current session of the node.
        /// </summary>
        public int Session { get; }

        /// <summary>
        /// Parses beacon data from a byte array.
        /// </summary>
        /// <param name="data">Data to be parsed.</param>
        /// <returns>Parsed beacon data.</returns>
        public static BeaconData Parse(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(stream))
                {
                    if (!C_HEADER.SequenceEqual(reader.ReadChars(C_HEADER.Length)))
                    {
                        throw new InvalidOperationException();
                    }

                    if (reader.ReadByte() != C_VERSION)
                    {
                        throw new InvalidOperationException();
                    }

                    var id = new Guid(reader.ReadBytes(16));
                    int port = reader.ReadInt32();
                    var session = reader.ReadInt32();
                    int sequence = reader.ReadInt32();
                    return new BeaconData(id, port, session, sequence);
                }
            }
        }

        /// <summary>
        /// Converts the beacon data to a byte array.
        /// </summary>
        /// <returns>Byte array containing all beacon data.</returns>
        public byte[] ToByteArray()
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(C_HEADER);
                    writer.Write(C_VERSION);
                    writer.Write(Id.ToByteArray());
                    writer.Write(Port);
                    writer.Write(Session);
                    writer.Write(Sequence);
                    return stream.ToArray();
                }
            }
        }
    }
}