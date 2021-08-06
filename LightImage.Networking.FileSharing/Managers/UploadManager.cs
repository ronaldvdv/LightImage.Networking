using LightImage.Networking.FileSharing.IO;
using LightImage.Networking.FileSharing.Options;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LightImage.Networking.FileSharing.Managers
{
    /// <summary>
    /// Class responsible for handling all uploads (incoming GET requests from peers)
    /// </summary>
    public class UploadManager : IUploadManager
    {
        private readonly ILogger<UploadManager> _logger;

        /// <summary>
        /// Options that define how uploads should behave
        /// </summary>
        private readonly IUploadOptions _options;

        /// <summary>
        /// Repository of files that can be shared with peers
        /// </summary>
        private readonly Dictionary<FileDescriptor, string> _paths = new Dictionary<FileDescriptor, string>();

        /// <summary>
        /// Array pool for sharing buffers
        /// </summary>
        private readonly ArrayPool<byte> _pool;

        /// <summary>
        /// Reader for file chunks
        /// </summary>
        private readonly IChunkReader _reader;

        /// <summary>
        /// List of pending upload requests that could not yet be provided
        /// </summary>
        private readonly List<ChunkRequest> _uploads = new List<ChunkRequest>();

        public UploadManager(IUploadOptions options, IChunkReader reader, ILogger<UploadManager> logger)
        {
            _pool = ArrayPool<byte>.Shared;
            _options = options;
            _reader = reader;
            _logger = logger;
        }

        public void Add(FileDescriptor descriptor, string path)
        {
            _paths[descriptor] = path;
        }

        public void HandleGet(FileDescriptor descriptor, ChunkRange chunk, Guid peer, IUploadContext context)
        {
            _logger.LogTrace(FileShareEvents.Get, "HandleGet; descriptor {descriptor}, chunk {chunk}, peer {peer}", descriptor, chunk, peer);
            if (TryGet(descriptor, chunk, peer, context))
                return;
            _uploads.Add(new ChunkRequest(descriptor, chunk, peer, _options.UploadRetryPolicy.GetInterval(0)));
        }

        public void HandleTimer(IUploadContext context)
        {
            var now = DateTime.Now;
            var selection = _uploads.Where(req => req.Expiry < now).ToArray();
            foreach (var request in selection)
                Retry(request, context);
        }

        public void Remove(FileDescriptor descriptor, IUploadContext context)
        {
            var uploads = _uploads.Where(cr => cr.Descriptor.Equals(descriptor)).ToArray();
            _paths.Remove(descriptor);

            foreach (var upload in uploads)
            {
                _uploads.Remove(upload);
                context.SendMissing(upload.Descriptor, upload.Chunk, upload.Peer);
            }
        }

        private void Retry(ChunkRequest request, IUploadContext context)
        {
            if (TryGet(request.Descriptor, request.Chunk, request.Peer, context))
                _uploads.Remove(request);
            else
            {
                var timeout = _options.UploadRetryPolicy.GetInterval(request.Attempts);
                if (timeout == TimeSpan.MaxValue)
                {
                    _uploads.Remove(request);
                    context.SendMissing(request.Descriptor, request.Chunk, request.Peer);
                }
                else
                {
                    request.AddAttempt(DateTime.Now + timeout);
                }
            }
        }

        private bool TryGet(FileDescriptor descriptor, ChunkRange chunk, Guid peer, IUploadContext context)
        {
            if (!_paths.TryGetValue(descriptor, out var path))
            {
                context.SendMissing(descriptor, chunk, peer);
                return true;
            }

            var buffer = _pool.Rent(chunk.Size);
            bool result = true;
            try
            {
                int read = _reader.Read(path, chunk.Offset, chunk.Size, buffer);
                chunk = new ChunkRange(chunk.Index, chunk.Offset, read);
                context.SendChunk(descriptor, chunk, peer, buffer);
            }
            catch (IOException)
            {
                result = false;
            }
            finally
            {
                _pool.Return(buffer);
            }
            return result;
        }
    }
}