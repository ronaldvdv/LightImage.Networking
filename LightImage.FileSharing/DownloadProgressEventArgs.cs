using System;

namespace LightImage.FileSharing
{
    public class DownloadProgressEventArgs : EventArgs
    {
        public DownloadProgressEventArgs(FileDescriptor descriptor, long bytesReceived)
        {            
            Descriptor = descriptor;
            BytesReceived = bytesReceived;
        }

        public FileDescriptor Descriptor { get; }
        public long BytesReceived { get; }
    }
}