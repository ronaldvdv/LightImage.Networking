using System;

namespace LightImage.FileSharing.Managers
{
    public interface IDownloadManager
    {
        event EventHandler<FileDescriptorEventArgs> DownloadCompleted;

        event EventHandler<FileDescriptorEventArgs> DownloadFailed;

        event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        void AddPeer(Guid peerId, IDownloadContext context);

        bool Cancel(FileDescriptor descriptor, IDownloadContext context);

        void HandleChunk(FileDescriptor descriptor, ChunkRange chunk, byte[] data, Guid peer, IDownloadContext context);

        void HandleMissing(FileDescriptor descriptor, ChunkRange chunk, Guid peer, IDownloadContext context);

        void HandleTimer(IDownloadContext context);

        void RemovePeer(Guid peerId);

        void Request(FileDescriptor descriptor, string path, IDownloadContext context);
    }
}