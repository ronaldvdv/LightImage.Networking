using System;
using System.IO;
using NetMQ;

namespace LightImage.Networking.FileSharing
{
    public readonly struct ChunkRange : IEquatable<ChunkRange>
    {
        public readonly int Index;
        public readonly int Size;

        public ChunkRange(int index, long offset, int size)
        {
            Index = index;
            Offset = offset;
            Size = size;
        }

        public long Offset { get; }

        public static ChunkRange FromIndex(int index, int chunkSize, long fileSize)
        {
            long offset = index * (long)chunkSize;
            long remainder = fileSize - offset;
            int size = remainder > chunkSize ? chunkSize : (int)remainder;
            return new ChunkRange(index, offset, size);
        }

        public bool Equals(ChunkRange other)
        {
            return Index == other.Index && Size == other.Size && Offset == other.Offset;
        }

        public override bool Equals(object obj)
        {
            if (obj is ChunkRange other)
                return Equals(other);
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            unchecked
            {
                hash = hash * 23 + Index;
                hash = hash * 23 + Size;
                hash = hash * 23 + Offset.GetHashCode();
            }
            return hash;
        }

        public override string ToString()
        {
            return $"[{Index}:{Offset}:{Size}]";
        }

        internal static ChunkRange Parse(NetMQFrame frame)
        {
            return Parse(new BinaryReader(new MemoryStream(frame.ToByteArray())));
        }

        internal static ChunkRange Parse(BinaryReader reader)
        {
            int index = reader.ReadInt32();
            long offset = reader.ReadInt64();
            int size = reader.ReadInt32();
            return new ChunkRange(index, offset, size);
        }

        internal byte[] ToByteArray()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Index);
                writer.Write(Offset);
                writer.Write(Size);
                return stream.ToArray();
            }
        }
    }
}