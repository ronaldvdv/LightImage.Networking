using System;
using System.Collections.Generic;
using System.Linq;

namespace LightImage.FileSharing.Algorithms
{
    public interface IPrecalculatedData
    {
        void Update(IDownloadStatus status);
    }

    public abstract class DownloadAlgorithmBase<TPrecalculatedData> : IDownloadAlgorithm where TPrecalculatedData : IPrecalculatedData
    {
        protected readonly int _chunkSize;
        protected readonly TPrecalculatedData _data;

        public DownloadAlgorithmBase(int chunkSize)
        {
            _chunkSize = chunkSize;
            _data = CreatePrecalculatedData();
        }

        public bool Step(IDownloadStatus status, out FileDescriptor file, out ChunkRange range, out Guid peer)
        {
            _data.Update(status);
            var options = GetOptions(status).Where(option => Accept(option, status));

            if (!options.Any())
            {
                file = default;
                range = default;
                peer = default;
                return false;
            }

            Option bestOption = GetBestOption(options, status);

            file = bestOption.File;
            peer = bestOption.Peer;
            int chunk = status.GetWaitingChunks(file).First();
            long offset = chunk * (long)_chunkSize;
            range = ChunkRange.FromIndex(chunk, _chunkSize, file.FileSize);
            return true;
        }

        protected abstract bool Accept(Option option, IDownloadStatus status);

        protected abstract TPrecalculatedData CreatePrecalculatedData();

        protected abstract double Score(Option option, IDownloadStatus status);

        private bool AcceptFile(FileDescriptor file, IDownloadStatus status)
        {
            return status.HasWaitingChunks(file);
        }

        private Option GetBestOption(IEnumerable<Option> options, IDownloadStatus status)
        {
            double bestScore = double.MinValue;
            Option bestOption = default;

            foreach (var option in options)
            {
                var score = Score(option, status);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestOption = option;
                }
            }

            return bestOption;
        }

        private IEnumerable<Option> GetOptions(IDownloadStatus status)
        {
            return status.Files.Where(file => AcceptFile(file, status)).SelectMany(file => status.Peers.Select(peer => new Option(file, peer)));
        }

        public readonly struct Option
        {
            public readonly FileDescriptor File;
            public readonly Guid Peer;

            public Option(FileDescriptor file, Guid peer)
            {
                File = file;
                Peer = peer;
            }
        }
    }
}