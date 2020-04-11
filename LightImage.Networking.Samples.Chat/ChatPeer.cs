using LightImage.Networking.Services;
using System;

namespace LightImage.Networking.Samples.Chat
{
    public class ChatPeer : ClusterShimPeer
    {
        public ChatPeer(Guid id, string endpoint, Guid me, string role)
            : base(id, endpoint, me, role)
        {
        }
    }
}