using System;
using System.Collections.Generic;

namespace LightImage.FileSharing
{
    /// <summary>
    /// Contract for exposing the current status of pending downloads
    /// </summary>
    public interface IDownloadStatus
    {
        /// <summary>
        /// Set of pending downloads
        /// </summary>
        IEnumerable<FileDescriptor> Files { get; }

        /// <summary>
        /// Set of peers that may provide files
        /// </summary>
        IEnumerable<Guid> Peers { get; }

        /// <summary>
        /// Currently pending requests for chunks
        /// </summary>
        IEnumerable<ChunkRequest> PendingRequests { get; }

        /// <summary>
        /// Get the availability of a file at a peer
        /// </summary>
        /// <param name="descriptor">File descriptor</param>
        /// <param name="peer">Peer identifier</param>
        /// <returns></returns>
        Availability GetAvailability(FileDescriptor descriptor, Guid peer);

        /// <summary>
        /// Get the progress for a file as a fraction between 0 and 1
        /// </summary>
        /// <param name="file">File descriptor</param>
        /// <returns>Download progress as a number between 0 and 1</returns>
        double GetProgress(FileDescriptor file);

        /// <summary>
        /// Get the indices of the first chunks with status waiting for a specific file
        /// </summary>
        /// <param name="file">File descriptor</param>
        /// <returns>Indices of chunks with status waitng</returns>
        IEnumerable<int> GetWaitingChunks(FileDescriptor file, int count = 1);

        /// <summary>
        /// Test if there are any chunks with status 'Waiting' left for a specific file
        /// </summary>
        /// <param name="file">File descriptor</param>
        /// <returns>Whether there are any waiting chunks left</returns>
        bool HasWaitingChunks(FileDescriptor file);
    }
}