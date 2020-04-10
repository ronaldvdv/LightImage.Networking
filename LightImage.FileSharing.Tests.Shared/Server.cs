using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LightImage.FileSharing.Tests.Shared
{
    public class Server
    {
        private readonly ObservableCollection<File> _files = new ObservableCollection<File>();
        private readonly ListPublisherService _publisher;
        private readonly FileShareService _service;
        private int _counter = 0;

        public Server(ListPublisherService publisher, FileShareService service)
        {
            Files = new ReadOnlyObservableCollection<File>(_files);
            _publisher = publisher;
            _service = service;
        }

        public ReadOnlyObservableCollection<File> Files { get; }

        public static string HashToString(byte[] hash)
        {
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }

        public void Add(string path)
        {
            int id = ++_counter;
            var hash = HashToString(MD5.Create().ComputeHash(Encoding.Default.GetBytes(path)));
            long size = new FileInfo(path).Length;
            var descriptor = new FileDescriptor(id, hash, size);
            _service.Add(descriptor, path);
            string name = Path.GetFileName(path);
            _publisher.Add(id, hash, size, name);
            _files.Add(new File(id, hash, size, path));
        }
    }
}