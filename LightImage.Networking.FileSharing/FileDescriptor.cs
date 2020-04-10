using NetMQ;
using System;
using System.IO;

namespace LightImage.Networking.FileSharing
{
    public readonly struct FileDescriptor : IEquatable<FileDescriptor>
    {
        public const string C_EMPTY_HASH = "";

        public readonly int Checksum;
        public readonly long FileSize;
        public readonly string Hash;
        public readonly int Id;

        public FileDescriptor(int id, string hash, long size)
        {
            Id = id;
            Hash = hash;
            Checksum = CalcChecksum(id, size, hash);
            FileSize = size;
        }

        public bool Equals(FileDescriptor other)
        {
            return Id == other.Id && Checksum == other.Checksum && Hash == other.Hash;
        }

        public override bool Equals(object obj)
        {
            if (obj is FileDescriptor other)
                return Equals(other);
            return false;
        }

        public override int GetHashCode()
        {
            return Checksum;
        }

        public override string ToString()
        {
            return $"{Id}:{Hash}:{FileSize}";
        }

        internal static FileDescriptor Parse(NetMQFrame frame)
        {
            using (var stream = new MemoryStream(frame.ToByteArray()))
            using (var reader = new BinaryReader(stream))
                return Parse(reader);
        }

        internal static FileDescriptor Parse(BinaryReader reader)
        {
            var id = reader.ReadInt32();
            string hash = reader.ReadString();
            long size = reader.ReadInt64();
            var descriptor = new FileDescriptor(id, hash, size);
            if (hash == "")
                hash = null;
            return descriptor;
        }

        internal byte[] ToByteArray()
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Id);
                writer.Write(Hash ?? "");
                writer.Write(FileSize);
                return stream.ToArray();
            }
        }

        private static int CalcChecksum(int id, long fileSize, string hash)
        {
            int result = 17;
            unchecked
            {
                result = result * 23 + id;
                result = result * 23 + fileSize.GetHashCode();
                if (hash != null)
                    result = result * 23 + hash.GetHashCode();
            }
            return result;
        }
    }
}