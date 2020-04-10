using System;

namespace LightImage.Networking.FileSharing
{
    public class DownloadFinishedEventArgs : EventArgs
    {
        public DownloadFinishedEventArgs(DownloadOutcome outcome, FileDescriptor descriptor, string path)
        {
            Outcome = outcome;
            Descriptor = descriptor;
            Path = path;
        }

        public FileDescriptor Descriptor { get; }
        public DownloadOutcome Outcome { get; }
        public string Path { get; }
    }
}