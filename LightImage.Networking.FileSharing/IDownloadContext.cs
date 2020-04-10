using System;

namespace LightImage.Networking.FileSharing
{
    public interface IDownloadContext
    {
        void SendRequest(FileDescriptor descriptor, ChunkRange chunk, Guid peer);
    }
}