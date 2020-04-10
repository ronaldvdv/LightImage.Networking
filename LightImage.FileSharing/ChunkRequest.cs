using System;

namespace LightImage.FileSharing
{
    /// <summary>
    /// Description of a GET request, to allow for detecting time-outs. This structure is used
    /// both by the server and client half of the protocol.
    /// </summary>
    public class ChunkRequest : IEquatable<ChunkRequest>
    {
        public ChunkRequest(FileDescriptor descriptor, ChunkRange chunk, Guid peer, TimeSpan expiry)
        {
            Descriptor = descriptor;
            Chunk = chunk;
            Peer = peer;
            Created = DateTime.Now;
            Attempts = 1;
            Expiry = Created + expiry;
        }

        /// <summary>
        /// Number of attempts
        /// </summary>
        public int Attempts { get; private set; }

        /// <summary>
        /// Offset of the requested chunk within the file
        /// </summary>
        public ChunkRange Chunk { get; }

        /// <summary>
        /// Timestamp when the request was received
        /// </summary>
        public DateTime Created { get; }

        /// <summary>
        /// Descriptor of the file being requested
        /// </summary>
        public FileDescriptor Descriptor { get; }

        /// <summary>
        /// Date/time at which the latest attempt is assumed to have failed
        /// </summary>
        public DateTime Expiry { get; private set; }

        /// <summary>
        /// Peer from/by which the file is requested
        /// </summary>
        public Guid Peer { get; }

        public bool Equals(ChunkRequest other)
        {
            return Descriptor.Equals(other.Descriptor)
                && Chunk.Equals(other.Chunk)
                && Peer == other.Peer;
        }

        public override bool Equals(object obj)
        {
            if (obj is ChunkRequest request)
                return Equals(request);
            return false;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            unchecked
            {
                hash = hash * 23 + Descriptor.GetHashCode();
                hash = hash * 23 + Chunk.GetHashCode();
                hash = hash * 23 + Peer.GetHashCode();
            }
            return hash;
        }

        internal void AddAttempt(DateTime expiry)
        {
            Attempts++;
            Expiry = expiry;
        }
    }
}