using System;

namespace LightImage.Networking.Samples.Chat
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public MessageReceivedEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }
}