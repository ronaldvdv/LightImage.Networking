using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;
using NetMQ;
using System;

namespace LightImage.Networking.Samples.Chat
{
    public class ChatService : ClusterService<ChatShim, ChatPeer>
    {
        public const string C_ROLE = "Member";
        public const string C_SERVICE_NAME = "Chat";
        private readonly ChatShim.Factory _shimFactory;

        public ChatService(NetworkOptions network, ChatShim.Factory shimFactory, ILogger<ChatService> logger)
            : this(network.Id, network.Host, C_SERVICE_NAME, C_ROLE, logger)
        {
            _shimFactory = shimFactory;
        }

        public ChatService(Guid id, string host, string name, string role, ILogger<ClusterService<ChatShim, ChatPeer>> logger) : base(id, host, name, role, logger)
        {
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public void Send(string message)
        {
            Actor.Send(socket =>
            {
                socket.SendMoreFrame(ChatShim.C_CMD_SEND);
                socket.SendFrame(message);
            });
        }

        protected override ChatShim CreateShim()
        {
            return _shimFactory(Id, Host);
        }

        protected override void HandleActorEvent(string cmd, NetMQMessage msg)
        {
            switch (cmd)
            {
                case ChatShim.C_EVT_RECEIVED:
                    var message = msg[1].ConvertToString();
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));
                    break;

                default:
                    base.HandleActorEvent(cmd, msg);
                    break;
            }
        }
    }
}