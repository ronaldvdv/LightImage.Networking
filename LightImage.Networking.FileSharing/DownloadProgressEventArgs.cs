using System;

namespace LightImage.Networking.FileSharing
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public DownloadProgressEventArgs(FileDescriptor descriptor, long bytesReceived)
        {
            Descriptor = descriptor;
            BytesReceived = bytesReceived;
        }

        public long BytesReceived { get; }
        public FileDescriptor Descriptor { get; }
    }
}