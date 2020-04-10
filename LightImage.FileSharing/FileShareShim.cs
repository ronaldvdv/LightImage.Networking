using System;
using LightImage.FileSharing.Managers;
using LightImage.FileSharing.Options;
using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;
using NetMQ;

namespace LightImage.FileSharing
{
    /// <summary>
    /// Actor shim handler for a file sharing service. Incoming commands and messages are delegated
    /// to implementations of IDownloadManager and IUploadManager. The shim exposes itself to these
    /// managers as IDownloadContext and IUploadContext so the managers can respond by sending messages
    /// themselves.
    /// </summary>
    public class FileShareShim : ClusterShim<FileSharePeer>, IDownloadContext, IUploadContext
    {
        private readonly IDownloadManager _downloads;
        private readonly FileShareOptions _options;
        private readonly IUploadManager _uploads;
        private NetMQTimer _timer;

        public FileShareShim(Guid id, string host, FileShareOptions options, IDownloadManager downloads, IUploadManager uploads, ILogger<FileShareShim> logger)
            : base(id, host, FileShareService.C_SERVICE_ROLE, logger)
        {
            _options = options;
            _downloads = downloads;
            _uploads = uploads;

            _downloads.DownloadCompleted += HandleDownloads_DownloadCompleted;
            _downloads.DownloadFailed += HandleDownloads_DownloadFailed;
            _downloads.DownloadProgress += HandleDownloads_DownloadProgress;
        }

        public delegate FileShareShim Factory(Guid id, string host);

        protected override FileSharePeer CreatePeer(Guid id, string endpoint, string role)
        {
            return new FileSharePeer(id, endpoint, Id, role);
        }

        protected override void Setup(NetMQPoller poller)
        {
            _timer = new NetMQTimer(_options.RequestTimeout);
            _timer.Elapsed += HandleTimerElapsed;
            poller.Add(_timer);
            base.Setup(poller);
        }

        private void HandleDownloads_DownloadCompleted(object sender, FileDescriptorEventArgs e)
        {
            SendFinishedEvent(DownloadOutcome.Success, e.Descriptor, e.Path);
        }

        private void HandleDownloads_DownloadFailed(object sender, FileDescriptorEventArgs e)
        {
            SendFinishedEvent(DownloadOutcome.Failure, e.Descriptor);
        }

        private void HandleDownloads_DownloadProgress(object sender, Managers.DownloadProgressEventArgs e)
        {
            SendProgressEvent(e.Descriptor, e.BytesReceived);
        }

        private void HandleTimerElapsed(object sender, NetMQTimerEventArgs e)
        {
            _downloads.HandleTimer(this);
            _uploads.HandleTimer(this);
        }

        #region Command handling

        protected override void HandleShimMessage(string cmd, NetMQMessage msg)
        {
            switch (cmd)
            {
                case FileShareCommands.C_CMD_ADD:
                    HandleCommandAdd(msg);
                    break;

                case FileShareCommands.C_CMD_REMOVE:
                    HandleCommandRemove(msg);
                    break;

                case FileShareCommands.C_CMD_REQUEST:
                    HandleCommandRequest(msg);
                    break;

                case FileShareCommands.C_CMD_CANCEL:
                    HandleCommandCancel(msg);
                    break;

                default:
                    base.HandleShimMessage(cmd, msg);
                    break;
            }
        }

        protected override void OnConnected(FileSharePeer peer)
        {
            _downloads.AddPeer(peer.Id, this);
        }

        protected override void OnDisconnected(FileSharePeer peer)
        {
            _downloads.RemovePeer(peer.Id);
        }

        private void HandleCommandAdd(NetMQMessage message)
        {
            FileShareCommands.ParseAdd(message, out var descriptor, out var path);
            _uploads.Add(descriptor, path);
        }

        private void HandleCommandCancel(NetMQMessage message)
        {
            FileShareCommands.ParseCancel(message, out var descriptor);
            _downloads.Cancel(descriptor, this);
            SendFinishedEvent(DownloadOutcome.Canceled, descriptor, null);
        }

        private void HandleCommandRemove(NetMQMessage message)
        {
            FileShareCommands.ParseRemove(message, out var descriptor);
            _uploads.Remove(descriptor, this);
        }

        private void HandleCommandRequest(NetMQMessage message)
        {
            FileShareCommands.ParseRequest(message, out var descriptor, out var path);
            _downloads.Request(descriptor, path, this);
        }

        #endregion Command handling

        #region Message handling

        protected override void HandleRouterMessage(string cmd, NetMQMessage msg, Guid identity)
        {
            switch (cmd)
            {
                case FileShareMessages.C_MSG_CHUNK:
                    HandleMessageChunk(msg, identity);
                    break;

                case FileShareMessages.C_MSG_MISSING:
                    HandleMessageMissing(msg, identity);
                    break;

                case FileShareMessages.C_MSG_GET:
                    HandleMessageGet(msg, identity);
                    break;

                default:
                    base.HandleRouterMessage(cmd, msg, identity);
                    break;
            }
        }

        private void HandleMessageChunk(NetMQMessage message, Guid peer)
        {
            FileShareMessages.ParseChunk(message, out var descriptor, out var chunk, out var data);
            _downloads.HandleChunk(descriptor, chunk, data, peer, this);
        }

        private void HandleMessageGet(NetMQMessage message, Guid peer)
        {
            FileShareMessages.ParseGet(message, out var descriptor, out var chunk);
            _uploads.HandleGet(descriptor, chunk, peer, this);
        }

        private void HandleMessageMissing(NetMQMessage message, Guid peer)
        {
            FileShareMessages.ParseMissing(message, out var descriptor, out var chunk);
            _downloads.HandleMissing(descriptor, chunk, peer, this);
        }

        #endregion Message handling

        #region Context implementations

        void IUploadContext.SendChunk(FileDescriptor descriptor, ChunkRange chunk, Guid peerId, byte[] data)
        {
            var peer = GetPeer(peerId);
            FileShareMessages.SendChunk(peer.Dealer, descriptor, chunk, data);
        }

        void IUploadContext.SendMissing(FileDescriptor descriptor, ChunkRange chunk, Guid peerId)
        {
            var peer = GetPeer(peerId);
            FileShareMessages.SendMissing(peer.Dealer, descriptor, chunk);
        }

        void IDownloadContext.SendRequest(FileDescriptor descriptor, ChunkRange range, Guid peerId)
        {
            var peer = GetPeer(peerId);
            FileShareMessages.SendGet(peer.Dealer, descriptor, range);
        }

        #endregion Context implementations

        private void SendFinishedEvent(DownloadOutcome outcome, FileDescriptor descriptor, string path = null)
        {
            FileShareMessages.SendFinished(Shim, outcome, descriptor, path);
        }

        private void SendProgressEvent(FileDescriptor descriptor, long bytesReceived)
        {
            FileShareMessages.SendProgress(Shim, descriptor, bytesReceived);
        }
    }
}