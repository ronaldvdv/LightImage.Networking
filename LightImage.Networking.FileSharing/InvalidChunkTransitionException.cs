using System;

namespace LightImage.Networking.FileSharing
{
    public class InvalidChunkTransitionException : Exception
    {
        public InvalidChunkTransitionException(int chunk, ChunkState oldState, ChunkState newState)
        {
            Chunk = chunk;
            OldState = oldState;
            NewState = newState;
        }

        public int Chunk { get; }
        public ChunkState NewState { get; }
        public ChunkState OldState { get; }
    }
}