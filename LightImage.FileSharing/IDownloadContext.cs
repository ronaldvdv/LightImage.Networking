using System;

namespace LightImage.FileSharing
{
    public interface IDownloadContext
    {
        void SendRequest(FileDescriptor descriptor, ChunkRange chunk, Guid peer);
    }
}