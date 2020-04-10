namespace LightImage.FileSharing.Tests.Shared
{
    public class File
    {
        public File(int id, string hash, long fileSize, string path)
        {
            Id = id;
            Hash = hash;
            FileSize = fileSize;
            Path = path;
        }

        public long FileSize { get; }
        public string Hash { get; }
        public int Id { get; }
        public string Path { get; }
    }
}