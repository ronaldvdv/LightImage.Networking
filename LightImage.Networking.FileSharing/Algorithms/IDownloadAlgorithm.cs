using System;

namespace LightImage.Networking.FileSharing.Algorithms
{
    public interface IDownloadAlgorithm
    {
        bool Step(IDownloadStatus status, out FileDescriptor file, out ChunkRange range, out Guid peer);
    }
}