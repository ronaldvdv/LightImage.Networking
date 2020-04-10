using LightImage.FileSharing.Options;
using System;

namespace LightImage.FileSharing.Algorithms
{
    public class DownloadAlgorithm : DownloadAlgorithmBase<PrecalculatedData>
    {
        /// <summary>
        /// Fraction of channels (MaxParallelChunks) for which small files should get a bonus score
        /// </summary>
        public const double C_SMALL_CHANNEL_FRACTION = 0.25;

        /// <summary>
        /// Threshold for small files, in bytes
        /// </summary>
        public const long C_SMALL_THRESHOLD = 5 * 1024 * 1024;

        /// <summary>
        /// Maximum score for files that are almost finished
        /// </summary>
        private const double C_WEIGHT_ALMOST_FINISHED = 1;

        /// <summary>
        /// Weight for availability score (0 for unavailable, 1 for expired, 0.5 for available)
        /// </summary>
        private const double C_WEIGHT_AVAILABILITY = 1;

        /// <summary>
        /// Maximum score for files which are relatively hard to find
        /// </summary>
        private const double C_WEIGHT_HARD_TO_FIND = 2;

        /// <summary>
        /// Weight for relatively small files (as defined by <see cref="C_SMALL_THRESHOLD"/>,
        /// if less than <see cref="C_SMALL_CHANNEL_FRACTION"/> of the maximum chunks are being
        /// used for small files. This ensures small files will never be completely blocked by
        /// large files.
        /// </summary>
        private const double C_WEIGHT_SMALL_CHANNELS = 10;

        private readonly IDownloadOptions _options;
        private readonly int _smallChannels;

        public DownloadAlgorithm(IDownloadOptions options) : base(options.ChunkSize)
        {
            _options = options;
            _smallChannels = (int)Math.Ceiling(options.MaxParallelChunks * C_SMALL_CHANNEL_FRACTION);
        }

        protected override bool Accept(Option option, IDownloadStatus status)
        {
            return _data.PendingTotal < _options.MaxParallelChunks
                && _data.PendingPerFile[option.File] < _options.MaxParallelPerFile
                && _data.PendingPerPeer[option.Peer] < _options.MaxParallelPerPeer
                && status.GetAvailability(option.File, option.Peer) != Availability.Unavailable;
        }

        protected override PrecalculatedData CreatePrecalculatedData()
        {
            return new PrecalculatedData(C_SMALL_THRESHOLD);
        }

        protected override double Score(Option option, IDownloadStatus status)
        {
            double scoreSmall = (option.File.FileSize <= C_SMALL_THRESHOLD && _data.PendingSmall < _smallChannels) ? C_WEIGHT_SMALL_CHANNELS : 0;
            double scoreFinished = C_WEIGHT_ALMOST_FINISHED * _data.Progress[option.File];
            double scoreHardToFind = C_WEIGHT_HARD_TO_FIND * (1 - _data.Availabilities[option.File]);
            double scoreAvailability = C_WEIGHT_AVAILABILITY * _data.GetAvailabilityScore(status.GetAvailability(option.File, option.Peer));
            return scoreSmall + scoreFinished + scoreHardToFind + scoreAvailability;
        }
    }
}