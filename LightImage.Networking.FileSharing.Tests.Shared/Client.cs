using System;
using System.Collections.ObjectModel;
using System.IO;

namespace LightImage.Networking.FileSharing.Tests.Shared
{
    public class Client
    {
        private readonly DirectoryInfo _directory;
        private readonly ObservableCollection<File> _files = new ObservableCollection<File>();
        private readonly FileShareService _service;

        public Client(ListSubscriberService subscriber, FileShareService node)
        {
            string dir = DateTime.Now.ToString("yy-MM-dd-H-mm-ss");
            _directory = Directory.CreateDirectory(dir);

            Files = new ReadOnlyObservableCollection<File>(_files);
            subscriber.FilePublished += HandleSubscriber_FilePublished;
            _service = node;
        }

        public ReadOnlyObservableCollection<File> Files { get; }

        private void HandleSubscriber_FilePublished(object sender, FilePublishedEventArgs e)
        {
            string path = Path.Combine(_directory.FullName, e.Name);
            var d = e.Descriptor;
            _files.Add(new File(d.Id, d.Hash, d.FileSize, path));
            _service.Request(e.Descriptor, path);
        }
    }
}