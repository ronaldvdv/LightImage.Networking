using LightImage.Networking.Services;
using Microsoft.Extensions.Logging;
using NetMQ;
using System;

namespace LightImage.Networking.Samples.Chat
{
    public class ChatShim : ClusterShim<ChatPeer>
    {
        public const string C_CMD_SEND = "Send";
        public const string C_EVT_RECEIVED = "Received";
        public const string C_MSG_RECEIVE = "Receive";

        public ChatShim(Guid id, string host, ILogger<ClusterShim<ChatPeer>> logger)
            : base(id, host, ChatService.C_ROLE, logger)
        {
        }

        public delegate ChatShim Factory(Guid id, string host);

        protected override ChatPeer CreatePeer(Guid id, string endpoint, string role)
        {
            return new ChatPeer(id, endpoint, Id, role);
        }

        protected override void HandleRouterMessage(string cmd, NetMQMessage msg, Guid identity)
        {
            switch (cmd)
            {
                case C_MSG_RECEIVE:
                    var message = msg[2].ConvertToString();
                    Shim.Send(socket =>
                    {
                        socket.SendMoreFrame(C_EVT_RECEIVED);
                        socket.SendFrame(message);
                    });
                    break;

                default:
                    base.HandleRouterMessage(cmd, msg, identity);
                    break;
            }
        }

        protected override void HandleShimMessage(string cmd, NetMQMessage msg)
        {
            switch (cmd)
            {
                case C_CMD_SEND:
                    var message = msg[1].ConvertToString();
                    foreach (var peer in Peers)
                    {
                        peer.Dealer.SendMoreFrame(C_MSG_RECEIVE).SendFrame(message);
                    }
                    break;

                default:
                    base.HandleShimMessage(cmd, msg);
                    break;
            }
        }
    }
}