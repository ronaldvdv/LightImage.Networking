using LightImage.FileSharing.Options;
using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;
using NetMQ;
using System;

namespace LightImage.FileSharing
{
    public class FileShareService : ClusterService<FileShareShim, FileSharePeer>, IService, IFileShareService
    {
        public const string C_SERVICE_NAME = "FileShare";
        public const string C_SERVICE_ROLE = "Member";
        private readonly FileShareOptions _options;
        private readonly FileShareShim.Factory _shimFactory;

        public FileShareService(NetworkOptions network, FileShareOptions options, FileShareShim.Factory shimFactory, ILogger<FileShareService> logger, string name = C_SERVICE_NAME)
            : this(network.Id, network.Host, options, shimFactory, logger, name) { }

        public FileShareService(Guid id, string host, FileShareOptions options, FileShareShim.Factory shimFactory, ILogger<FileShareService> logger, string name = C_SERVICE_NAME) : base(id, host, name, C_SERVICE_ROLE, logger)
        {
            _options = options;
            _shimFactory = shimFactory;
        }

        public event EventHandler<DownloadFinishedEventArgs> DownloadFinished;

        public event EventHandler<DownloadProgressEventArgs> DownloadProgress;

        public void Add(FileDescriptor descriptor, string path)
        {
            FileShareCommands.SendAdd(Actor, descriptor, path);
        }

        public void Cancel(FileDescriptor descriptor)
        {
            FileShareCommands.SendCancel(Actor, descriptor);
        }

        public void Remove(FileDescriptor descriptor)
        {
            FileShareCommands.SendRemove(Actor, descriptor);
        }

        public void Request(FileDescriptor descriptor, string path)
        {
            FileShareCommands.SendRequest(Actor, descriptor, path);
        }

        protected override FileShareShim CreateShim()
        {
            return _shimFactory(Id, Host);
        }

        protected override void HandleActorEvent(string cmd, NetMQMessage msg)
        {
            switch (cmd)
            {
                case FileShareMessages.C_EVT_FINISHED:
                    FileShareMessages.ParseFinished(msg, out var outcome, out var descriptor, out var path);
                    DownloadFinished?.Invoke(this, new DownloadFinishedEventArgs(outcome, descriptor, path));
                    break;

                case FileShareMessages.C_EVT_PROGRESS:
                    FileShareMessages.ParseProgress(msg, out descriptor, out long bytesReceived);
                    DownloadProgress?.Invoke(this, new DownloadProgressEventArgs(descriptor, bytesReceived));
                    break;

                default:
                    base.HandleActorEvent(cmd, msg);
                    break;
            }
        }
    }
}