namespace LightImage.FileSharing.Managers
{
    public class DownloadProgressEventArgs : FileDescriptorEventArgs
    {
        public DownloadProgressEventArgs(FileDescriptor descriptor, string path, long bytesReceived) : base(descriptor, path)
        {
            BytesReceived = bytesReceived;
        }

        public long BytesReceived { get; }
    }
}