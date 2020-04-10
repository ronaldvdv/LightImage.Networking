using System;

namespace LightImage.FileSharing
{
    public interface IFileShareService
    {
        event EventHandler<DownloadFinishedEventArgs> DownloadFinished;

        event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        void Add(FileDescriptor descriptor, string path);

        void Cancel(FileDescriptor descriptor);

        void Remove(FileDescriptor descriptor);

        void Request(FileDescriptor descriptor, string path);
    }
}