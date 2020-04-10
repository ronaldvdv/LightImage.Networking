using System;
using LightImage.Util.Polly;

namespace LightImage.Networking.FileSharing
{
    /// <summary>
    /// Class for tracking the current state of a download request
    /// </summary>
    public class FileRequest
    {
        public FileRequest(FileDescriptor descriptor, string path, int chunkSize, RetryPolicy availabilityExpiry)
        {
            var fullChunks = descriptor.FileSize / chunkSize;
            var rest = descriptor.FileSize - (fullChunks * chunkSize);
            int extra = (rest > 0) ? 1 : 0;

            File = descriptor;
            Chunks = (int)(fullChunks + extra);
            Path = path;
            ChunkSize = chunkSize;
            Progress = new ChunkMap(Chunks);
            Availability = new AvailabilityMap(availabilityExpiry);
        }

        /// <summary>
        /// Availability of the file at our peers
        /// </summary>
        public AvailabilityMap Availability { get; }

        /// <summary>
        /// Number of chunks
        /// </summary>
        public int Chunks { get; }

        /// <summary>
        /// Size of each chunk
        /// </summary>
        public int ChunkSize { get; }

        /// <summary>
        /// File being requested
        /// </summary>
        public FileDescriptor File { get; }

        /// <summary>
        /// Path where the file should be created
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Download progress per chunk
        /// </summary>
        public ChunkMap Progress { get; }

        public void HandleChunkMissing(ChunkRange chunk, Guid peer)
        {
            Progress.Set(chunk.Index, ChunkState.Waiting);
            Availability.Set(peer, false);
        }

        public void HandleChunkReceived(ChunkRange chunk, Guid peer)
        {
            Progress.Set(chunk.Index, ChunkState.Received);
            Availability.Set(peer, true);
        }

        public void HandleChunkTimeout(ChunkRange chunk, Guid peer)
        {
            Progress.Set(chunk.Index, ChunkState.Pending);
            Availability.Set(peer, false);
        }

        internal void HandleChunkRequested(ChunkRange chunk, Guid peer)
        {
            Progress.Set(chunk.Index, ChunkState.Pending);
        }

        internal void RemovePeer(Guid peerId)
        {
            Availability.Remove(peerId);
        }
    }
}