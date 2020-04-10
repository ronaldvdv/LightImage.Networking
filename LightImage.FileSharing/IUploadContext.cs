using System;

namespace LightImage.FileSharing
{
    public interface IUploadContext
    {
        void SendChunk(FileDescriptor descriptor, ChunkRange chunk, Guid peer, byte[] data);

        void SendMissing(FileDescriptor descriptor, ChunkRange chunk, Guid peer);
    }
}