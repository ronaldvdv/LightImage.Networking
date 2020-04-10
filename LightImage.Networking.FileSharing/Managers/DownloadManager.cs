using LightImage.Networking.FileSharing.Algorithms;
using LightImage.Networking.FileSharing.IO;
using LightImage.Networking.FileSharing.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LightImage.Networking.FileSharing.Managers
{
    public class DownloadManager : IDownloadManager, IDownloadStatus
    {
        private readonly IDownloadAlgorithm _algorithm;

        /// <summary>
        /// Pending file downloads
        /// </summary>
        private readonly Dictionary<FileDescriptor, FileRequest> _downloads = new Dictionary<FileDescriptor, FileRequest>();

        private readonly ILogger<DownloadManager> _logger;

        /// <summary>
        /// Options that define how downloads should behave
        /// </summary>
        private readonly IDownloadOptions _options;

        /// <summary>
        /// List of peer identifiers
        /// </summary>
        private readonly List<Guid> _peers = new List<Guid>();

        /// <summary>
        /// Pending chunk requests
        /// </summary>
        private readonly List<ChunkRequest> _pending = new List<ChunkRequest>();

        /// <summary>
        /// Chunk writer
        /// </summary>
        private readonly IChunkWriter _writer;

        public DownloadManager(IDownloadOptions options, IChunkWriter writer, IDownloadAlgorithm algorithm, ILogger<DownloadManager> logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _algorithm = algorithm ?? throw new ArgumentNullException(nameof(algorithm));
            _logger = logger;
        }

        #region IDownloadManager implementation

        public event EventHandler<FileDescriptorEventArgs> DownloadCompleted;

#pragma warning disable 67

        public event EventHandler<FileDescriptorEventArgs> DownloadFailed;

#pragma warning restore 67

        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        public void AddPeer(Guid peerId, IDownloadContext context)
        {
            _peers.Add(peerId);
            Step(context);
        }

        public bool Cancel(FileDescriptor descriptor, IDownloadContext context)
        {
            if (!_downloads.Remove(descriptor))
                return false;

            _logger.LogTrace(FileShareEvents.Cancel, "Cancel download {descriptor}", descriptor);
            _pending.RemoveAll(cr => cr.Descriptor.Equals(descriptor));
            Step(context);

            return true;
        }

        public void HandleChunk(FileDescriptor descriptor, ChunkRange chunk, byte[] data, Guid peer, IDownloadContext context)
        {
            _logger.LogTrace(FileShareEvents.Chunk, "HandleÇhunk; descriptor {descriptor}, chunk {chunk}, size {size}, peer {peer}", descriptor, chunk, data.Length, peer);
            FileRequest download = null;
            try
            {
                download = _downloads[descriptor];
                _writer.Write(download.Path, chunk.Offset, chunk.Size, data);
            }
            finally
            {
                download.HandleChunkReceived(chunk, peer);
                DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(download.File, download.Path, download.Progress.Received * (long)download.ChunkSize));
                _pending.RemoveAll(cr => cr.Descriptor.Equals(descriptor) && cr.Chunk.Index == chunk.Index && cr.Peer.Equals(peer));
                CheckCompleted(download);
                Step(context);
            }
        }

        public void HandleMissing(FileDescriptor descriptor, ChunkRange chunk, Guid peer, IDownloadContext context)
        {
            var download = _downloads[descriptor];
            _logger.LogTrace(FileShareEvents.Missing, "Peer {peer} is missing file {descriptor}; remove any pending requests", peer, descriptor);
            download.HandleChunkMissing(chunk, peer);
            _pending.RemoveAll(cr => cr.Descriptor.Equals(descriptor) && cr.Peer.Equals(peer));
            Step(context);
        }

        public void HandleTimer(IDownloadContext context)
        {
            var now = DateTime.Now;
            var remove = _pending.Where(request => request.Created + _options.RequestTimeout > now).ToArray();

            foreach (var request in remove)
            {
                _logger.LogTrace(FileShareEvents.TimeOut, "Remove a timed-out chunk {chunk} for file {descriptor}", request.Chunk, request.Descriptor);
                _pending.Remove(request);
                _downloads[request.Descriptor].HandleChunkTimeout(request.Chunk, request.Peer);
            }

            Step(context);
        }

        public void RemovePeer(Guid peerId)
        {
            _peers.Remove(peerId);
            foreach (var download in _downloads.Values)
                download.RemovePeer(peerId);
            _pending.RemoveAll(cr => cr.Peer == peerId);
        }

        public void Request(FileDescriptor descriptor, string path, IDownloadContext context)
        {
            if (_downloads.ContainsKey(descriptor))
                throw new InvalidOperationException($"File has already been requested before");
            _logger.LogTrace(FileShareEvents.Request, "Request download {descriptor} to {path}", descriptor, path);
            Cancel(descriptor.Id, context);
            _downloads.Add(descriptor, new FileRequest(descriptor, path, _options.ChunkSize, _options.DownloadRetryPolicy));
            Step(context);
        }

        private void Cancel(int id, IDownloadContext context)
        {
            _logger.LogTrace(FileShareEvents.Cancel, "Cancel all downloads for asset {id}", id);
            var descriptors = _downloads.Keys.Where(d => d.Id == id).ToArray();
            foreach (var descriptor in descriptors)
                Cancel(descriptor, context);
        }

        #endregion IDownloadManager implementation

        #region IDownloadStatus implementation

        public IEnumerable<FileDescriptor> Files => _downloads.Keys;
        public IEnumerable<Guid> Peers => _peers;

        public IEnumerable<ChunkRequest> PendingRequests => _pending;

        public Availability GetAvailability(FileDescriptor descriptor, Guid peer) => _downloads[descriptor].Availability[peer];

        public double GetProgress(FileDescriptor file)
        {
            var map = _downloads[file].Progress;
            return map.Received / (double)map.Count;
        }

        public IEnumerable<int> GetWaitingChunks(FileDescriptor file, int count = 1)
        {
            return _downloads[file].Progress.FindWaitingChunks(count);
        }

        public bool HasWaitingChunks(FileDescriptor file)
        {
            return _downloads[file].Progress.Waiting > 0;
        }

        #endregion IDownloadStatus implementation

        private void CheckCompleted(FileRequest download)
        {
            if (!download.Progress.IsComplete)
                return;
            _logger.LogTrace(FileShareEvents.Completed, "File {descriptor} completed", download);
            _writer.Close(download.Path);
            _downloads.Remove(download.File);
            DownloadCompleted?.Invoke(this, new FileDescriptorEventArgs(download.File, download.Path));
        }

        private void SendRequest(FileDescriptor file, ChunkRange chunk, Guid peer, IDownloadContext context)
        {
            _logger.LogTrace(FileShareEvents.Request, "Sending a request for chunk {chunk} for file {descriptor} to {peer}", chunk, file, peer);
            context.SendRequest(file, chunk, peer);
            var request = new ChunkRequest(file, chunk, peer, _options.RequestTimeout);
            _pending.Add(request);
            _downloads[file].HandleChunkRequested(chunk, peer);
        }

        private void Step(IDownloadContext context)
        {
            while (_algorithm.Step(this, out var file, out var chunk, out var peer))
                SendRequest(file, chunk, peer, context);
        }
    }
}