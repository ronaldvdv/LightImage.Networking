using LightImage.Networking.FileSharing.Policies;
using System;

namespace LightImage.Networking.FileSharing.Options
{
    public class FileShareOptions : IDownloadOptions, IUploadOptions
    {
        public const string C_CONFIG_SECTION = "filetransfer";

        public int ChunkSize { get; set; } = 1024 * 1024;
        public RetryPolicyConfig DownloadRetryPolicy { get; set; } = RetryPolicyConfig.Exponential(3, TimeSpan.FromSeconds(1), 1.5, TimeSpan.FromSeconds(10));
        public int MaxParallelChunks { get; set; } = 10;
        public int MaxParallelPerFile { get; set; } = 5;
        public int MaxParallelPerPeer { get; set; } = 5;
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(2);
        public RetryPolicyConfig UploadRetryPolicy { get; set; } = RetryPolicyConfig.Constant(5, TimeSpan.FromSeconds(2));
    }
}