using System;

namespace LightImage.Networking.FileSharing.Managers
{
    public interface IUploadManager
    {
        void Add(FileDescriptor descriptor, string path);

        void HandleGet(FileDescriptor descriptor, ChunkRange chunk, Guid peer, IUploadContext context);

        void HandleTimer(IUploadContext context);

        void Remove(FileDescriptor descriptor, IUploadContext context);
    }
}