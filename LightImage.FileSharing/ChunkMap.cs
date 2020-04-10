using System.Collections;
using System.Collections.Generic;

namespace LightImage.FileSharing
{
    /// <summary>
    /// Map for tracking the state of individual chunks in a file
    /// </summary>
    public class ChunkMap
    {
        /// <summary>
        /// Collection of chunks that are currently pending (have been requested without a reply)
        /// </summary>
        private HashSet<int> _pending = new HashSet<int>();

        /// <summary>
        /// Whether each chunk was succesfully received or not
        /// </summary>
        private BitArray _received;

        /// <summary>
        /// Number of chunks at the start of the file that have been received. This number is strictly
        /// increasing only and can be used to efficiently find the first chunks that have not been received
        /// yet. See <see cref="FindWaitingChunks(int)"/> for more information.
        /// </summary>
        private int _skip = 0;

        public ChunkMap(int chunks)
        {
            _received = new BitArray(chunks);
            Count = chunks;
        }

        /// <summary>
        /// Total number of chunks
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Test if the file has been received completely
        /// </summary>
        public bool IsComplete => Received == Count;

        /// <summary>
        /// Number of chunks that are currently pending
        /// </summary>
        public int Pending => _pending.Count;

        /// <summary>
        /// Number of chunks that have been received succesfully
        /// </summary>
        public int Received { get; private set; }

        /// <summary>
        /// Number of chunks that are neither received nor pending
        /// </summary>
        public int Waiting => Count - (Pending + Received);

        /// <summary>
        /// Get/set the state of a specific chunk
        /// </summary>
        /// <param name="chunk">Chunk index</param>
        /// <returns>State of the chunk</returns>
        public ChunkState this[int chunk] {
            get => Get(chunk);
            set => Set(chunk, value);
        }

        /// <summary>
        /// Get the first N waiting chunks
        /// </summary>
        /// <param name="count">Maximum number of chunks to return</param>
        /// <returns>Collection of the first chunks that have status Waiting</returns>
        public IEnumerable<int> FindWaitingChunks(int count = 1)
        {
            int pos = _skip;
            while (count > 0 && pos < Count)
            {
                if (!_received[pos] && !_pending.Contains(pos))
                {
                    count--;
                    yield return pos;
                }
                pos++;
            }
        }

        /// <summary>
        /// Get the current state of a specific chunk
        /// </summary>
        /// <param name="chunk">Chunk index</param>
        /// <returns>State of the chunk</returns>
        public ChunkState Get(int chunk)
        {
            if (_received[chunk])
                return ChunkState.Received;
            if (_pending.Contains(chunk))
                return ChunkState.Pending;
            return ChunkState.Waiting;
        }

        /// <summary>
        /// Set the state for a specific chunk
        /// </summary>
        /// <param name="chunk">Chunk index</param>
        /// <param name="state">State for the chunk</param>
        public void Set(int chunk, ChunkState state)
        {
            switch (state)
            {
                case ChunkState.Pending:
                    SetPending(chunk);
                    break;

                case ChunkState.Waiting:
                    SetWaiting(chunk);
                    break;

                case ChunkState.Received:
                    SetReceived(chunk);
                    break;
            }
        }

        /// <summary>
        /// Mark a chunk as "pending"
        /// </summary>
        /// <param name="chunk">Chunk index</param>
        /// <exception cref="InvalidChunkTransitionException"/>
        private void SetPending(int chunk)
        {
            if (_received[chunk])
                throw new InvalidChunkTransitionException(chunk, ChunkState.Received, ChunkState.Pending);
            if (!_pending.Add(chunk))
                return;
        }

        /// <summary>
        /// Mark a chunk as "received"
        /// </summary>
        /// <param name="chunk">Chunk index</param>
        /// <exception cref="InvalidChunkTransitionException"/>
        private void SetReceived(int chunk)
        {
            if (_received[chunk])
                return;
            if (!_pending.Remove(chunk))
                throw new InvalidChunkTransitionException(chunk, ChunkState.Waiting, ChunkState.Received);
            _received[chunk] = true;
            Received++;
            UpdateHead();
        }

        /// <summary>
        /// Mark a chunk as "waiting"
        /// </summary>
        /// <param name="chunk">Chunk index</param>
        /// <exception cref="InvalidChunkTransitionException"/>
        private void SetWaiting(int chunk)
        {
            if (_received[chunk])
                throw new InvalidChunkTransitionException(chunk, ChunkState.Received, ChunkState.Waiting);
            _pending.Remove(chunk);
        }

        private void UpdateHead()
        {
            while (_skip < Count && _received[_skip])
                _skip++;
        }
    }
}