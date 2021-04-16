using LightImage.Networking.Services;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.IO;

namespace LightImage.Networking.FileSharing.Tests.Shared
{
    public class ListPublisherService : Service, IDisposable
    {
        private readonly string _host;
        private int _port;
        private PublisherSocket _publisher;

        public ListPublisherService(NetworkInfo options) : this(options.Host)
        {
        }

        public ListPublisherService(string host) : base("FileList", "Publisher")
        {
            _host = host;
        }

        public void Add(int id, string hash, long size, string name)
        {
            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(id);
                    writer.Write(hash);
                    writer.Write(size);
                    writer.Write(name);
                    var bytes = ms.ToArray();
                    _publisher.SendFrame(bytes);
                }
            }
        }

        public override void Add(Guid id, string host, string role, int[] ports)
        {
            // Do nothing
        }

        public override void Dispose()
        {
            base.Dispose();
            _publisher.Dispose();
        }

        public override void Remove(Guid id)
        {
            // Do nothing
        }

        public override void Reset()
        {
            // Do nothing
        }

        public override void Start()
        {
            _publisher = new PublisherSocket();
            _port = _publisher.BindRandomPort($"tcp://{_host}");
            SetPorts(_port);
        }

        public override void Stop()
        {
            _publisher.Dispose();
        }
    }
}