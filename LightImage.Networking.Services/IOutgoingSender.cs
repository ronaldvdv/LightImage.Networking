using System;
using NetMQ;

namespace LightImage.Networking.Services
{
    public interface IOutgoingSender
    {
        void Send(Action<IOutgoingSocket> action);
    }
}