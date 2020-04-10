using System;

namespace LightImage.FileSharing.Managers
{
    public class FileDescriptorEventArgs : EventArgs
    {
        public FileDescriptorEventArgs(FileDescriptor descriptor, string path)
        {
            Descriptor = descriptor;
            Path = path;
        }

        public FileDescriptor Descriptor { get; }
        public string Path { get; }
    }
}