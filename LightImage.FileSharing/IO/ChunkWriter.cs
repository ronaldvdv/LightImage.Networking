using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;

namespace LightImage.FileSharing.IO
{
    public class ChunkWriter : IChunkWriter
    {
        private static MD5 _hasher = MD5.Create();
        private readonly ILogger<ChunkWriter> _logger;

        public ChunkWriter(ILogger<ChunkWriter> logger)
        {
            _logger = logger;
        }

        public void Close(string path)
        {
            var temp = GetTempPath(path);
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temp, path);
            }
            catch (IOException e)
            {
                // TODO Nicely handle exception; report or retry?
                throw e;
            }
        }

        public void Write(string path, long offset, int count, byte[] data)
        {
            // TODO We may find IO errors (e.g. file locked, disk full etc)

            var hash = BitConverter.ToString(_hasher.ComputeHash(data, 0, count)).Replace("-", "");
            _logger.LogTrace("Writing data to {path} at offset {offset} count {count}; hash {hash}; buffer size {size}", path, offset, count, hash, data.Length);

            path = GetTempPath(path);
            using (var stream = File.OpenWrite(path))
            {
                stream.Seek(offset, SeekOrigin.Begin);
                stream.Write(data, 0, count);
            }
        }

        private string GetTempPath(string path)
        {
            return path + ".tmp";
        }
    }
}