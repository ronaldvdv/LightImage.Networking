using System.Buffers;
using NetMQ;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// NetMQ buffer pool that uses the <see cref="ArrayPool{T}"/> class from <see cref="System.Buffers"/>.
    /// </summary>
    public class ArrayPoolBufferPool : IBufferPool
    {
        private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public void Return(byte[] buffer)
        {
            _pool.Return(buffer);
        }

        /// <inheritdoc/>
        public byte[] Take(int size)
        {
            return _pool.Rent(size);
        }
    }
}