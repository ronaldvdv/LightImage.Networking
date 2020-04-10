using LightImage.Util.Polly;
using System;

namespace LightImage.FileSharing.Options
{
    public class FileShareOptions : IDownloadOptions, IUploadOptions
    {
        public const string C_CONFIG_SECTION = "filetransfer";

        public int ChunkSize { get; set; } = 1024 * 1024;
        public RetryPolicy DownloadRetryPolicy { get; set; } = RetryPolicy.Exponential(3, TimeSpan.FromSeconds(1), 1.5, TimeSpan.FromSeconds(10));
        public int MaxParallelChunks { get; set; } = 10;
        public int MaxParallelPerFile { get; set; } = 5;
        public int MaxParallelPerPeer { get; set; } = 5;
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(2);
        public RetryPolicy UploadRetryPolicy { get; set; } = RetryPolicy.Constant(5, TimeSpan.FromSeconds(2));
    }
}