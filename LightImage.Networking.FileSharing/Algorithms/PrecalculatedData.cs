using System;
using System.Collections.Generic;
using System.Linq;

namespace LightImage.Networking.FileSharing.Algorithms
{
    public class PrecalculatedData : IPrecalculatedData
    {
        private readonly Dictionary<FileDescriptor, double> _availabilities = new Dictionary<FileDescriptor, double>();
        private readonly long _maxSmallSize;
        private readonly Dictionary<FileDescriptor, int> _pendingPerFile = new Dictionary<FileDescriptor, int>();
        private readonly Dictionary<Guid, int> _pendingPerPeer = new Dictionary<Guid, int>();
        private readonly Dictionary<FileDescriptor, double> _progress = new Dictionary<FileDescriptor, double>();

        public PrecalculatedData(long maxSmallSize)
        {
            _maxSmallSize = maxSmallSize;
        }

        public IReadOnlyDictionary<FileDescriptor, double> Availabilities => _availabilities;
        public IReadOnlyDictionary<FileDescriptor, int> PendingPerFile => _pendingPerFile;

        public IReadOnlyDictionary<Guid, int> PendingPerPeer => _pendingPerPeer;

        public int PendingSmall { get; private set; }

        public int PendingTotal { get; private set; }

        public IReadOnlyDictionary<FileDescriptor, double> Progress => _progress;

        public double GetAvailabilityScore(Availability availability)
        {
            switch (availability)
            {
                case Availability.Available:
                    return 0.5;

                case Availability.AvailabilityExpired:
                    return 1.0;

                case Availability.Unavailable:
                default:
                    return 0.0;
            }
        }

        public void Update(IDownloadStatus status)
        {
            _pendingPerFile.Clear();
            _pendingPerPeer.Clear();
            _availabilities.Clear();
            _progress.Clear();

            foreach (var file in status.Files)
            {
                _progress[file] = status.GetProgress(file);
                _availabilities[file] = GetAvailabilityFraction(file, status);
                _pendingPerFile[file] = 0;
            }

            foreach (var peer in status.Peers)
                _pendingPerPeer[peer] = 0;

            PendingTotal = 0;
            PendingSmall = 0;

            foreach (var requests in status.PendingRequests)
            {
                _pendingPerFile[requests.Descriptor]++;
                _pendingPerPeer[requests.Peer]++;
                PendingTotal++;
                if (requests.Descriptor.FileSize <= _maxSmallSize)
                    PendingSmall++;
            }
        }

        private double GetAvailabilityFraction(FileDescriptor file, IDownloadStatus status)
        {
            if (!status.Peers.Any())
                return 0;
            return status.Peers.Average(peer => GetAvailabilityScore(status.GetAvailability(file, peer)));
        }
    }
}