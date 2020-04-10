using LightImage.Networking.Services;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LightImage.Networking.FileSharing.Tests.Shared
{
    public class ListSubscriberService : Service
    {
        private readonly Dictionary<Guid, string> _endpoints = new Dictionary<Guid, string>();
        private readonly SendOrPostCallback _publishedCallback;
        private readonly SynchronizationContext _sync;
        private NetMQPoller _poller;
        private SubscriberSocket _subscriber;

        public ListSubscriberService(NetworkOptions options) : this(options.Host)
        {
        }

        public ListSubscriberService(string host) : base("FileList", "Subscriber")
        {
            _sync = SynchronizationContext.Current;
            _publishedCallback = new SendOrPostCallback(args => FilePublished?.Invoke(this, (FilePublishedEventArgs)args));
        }

        public event EventHandler<FilePublishedEventArgs> FilePublished;

        public override void Add(Guid id, string host, string role, int[] ports)
        {
            if (role != "Publisher")
                return;
            string endpoint = $"tcp://{host}:{ports[0]}";
            Console.WriteLine($"Connect to: {endpoint}");
            _subscriber.Connect(endpoint);
            _subscriber.SubscribeToAnyTopic();
            _endpoints[id] = endpoint;
        }

        public override void Remove(Guid id)
        {
            string endpoint = _endpoints[id];
            _endpoints.Remove(id);
            _subscriber.Disconnect(endpoint);
        }

        public override void Reset()
        {
        }

        public override void Start()
        {
            _subscriber = new SubscriberSocket();
            _subscriber.ReceiveReady += HandleSubscriber_ReceiveReady;
            _poller = new NetMQPoller { _subscriber };
            _poller.RunAsync();
        }

        public override void Stop()
        {
            _poller.Stop();
            _poller.Dispose();
            _subscriber.Dispose();
        }

        private void HandleSubscriber_ReceiveReady(object sender, NetMQSocketEventArgs e)
        {
            using (var stream = new MemoryStream(e.Socket.ReceiveFrameBytes()))
            {
                using (var reader = new BinaryReader(stream))
                {
                    int id = reader.ReadInt32();
                    var hash = reader.ReadString();
                    long size = reader.ReadInt64();
                    string name = reader.ReadString();
                    var descriptor = new FileDescriptor(id, hash, size);
                    _sync.Send(_publishedCallback, new FilePublishedEventArgs(descriptor, name));
                }
            }
        }
    }
}