using LightImage.Networking.FileSharing.Policies;
using System;

namespace LightImage.Networking.FileSharing.Options
{
    public interface IDownloadOptions
    {
        /// <summary>
        /// Maximum size of a single chunk, in bytes
        /// </summary>
        int ChunkSize { get; }

        /// <summary>
        /// Expiry policy for unavailability data
        /// </summary>
        RetryPolicyConfig DownloadRetryPolicy { get; }

        /// <summary>
        /// Maximum number of parallel chunk requests
        /// </summary>
        int MaxParallelChunks { get; }

        /// <summary>
        /// Maximum number of parallel chunk requests for a specific file, over all peers
        /// </summary>
        int MaxParallelPerFile { get; }

        /// <summary>
        /// Maximum number of parallel chunk requests to a specific peer, over all files
        /// </summary>
        int MaxParallelPerPeer { get; }

        /// <summary>
        /// Time after which a pending chunk request is considered timed out
        /// </summary>
        TimeSpan RequestTimeout { get; }
    }
}