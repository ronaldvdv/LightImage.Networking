using NetMQ;
using System.Buffers;

namespace LightImage.Networking.FileSharing
{
    internal class SystemBufferPool : IBufferPool
    {
        private readonly ArrayPool<byte> _pool;

        public SystemBufferPool()
        {
            _pool = ArrayPool<byte>.Shared;
        }

        public void Dispose()
        {
        }

        public void Return(byte[] buffer)
        {
            _pool.Return(buffer);
        }

        public byte[] Take(int size)
        {
            return _pool.Rent(size);
        }
    }
}