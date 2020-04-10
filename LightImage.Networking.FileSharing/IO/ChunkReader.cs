using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;

namespace LightImage.Networking.FileSharing.IO
{
    public class ChunkReader : IChunkReader
    {
        private static MD5 _hasher = MD5.Create();
        private readonly ILogger<ChunkReader> _logger;

        public ChunkReader(ILogger<ChunkReader> logger)
        {
            _logger = logger;
        }

        public int Read(string path, long offset, int count, byte[] data)
        {
            using (var stream = File.OpenRead(path))
            {
                stream.Seek(offset, SeekOrigin.Begin);
                var read = stream.Read(data, 0, count);

                var hash = BitConverter.ToString(_hasher.ComputeHash(data, 0, read)).Replace("-", "");
                _logger.LogTrace("Read data from {path} at offset {offset} count {count}; hash {hash}; buffer size {size}; read {read} bytes", path, offset, count, hash, data.Length, read);

                return read;
            }
        }
    }
}