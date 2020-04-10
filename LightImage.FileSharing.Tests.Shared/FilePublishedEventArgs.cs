using System;

namespace LightImage.FileSharing.Tests.Shared
{
    public class FilePublishedEventArgs : EventArgs
    {
        public FilePublishedEventArgs(FileDescriptor descriptor, string name)
        {
            Descriptor = descriptor;
            Name = name;
        }

        public FileDescriptor Descriptor { get; }
        public string Name { get; }
    }
}