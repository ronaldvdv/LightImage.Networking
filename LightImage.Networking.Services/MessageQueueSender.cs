using System;
using System.Collections.Generic;
using NetMQ;

namespace LightImage.Networking.Services
{
    internal class MessageQueueSender : IOutgoingSender
    {
        private readonly object _gate = new object();
        private readonly NetMQQueue<NetMQMessage> _queue;
        private readonly Sender _sender = new Sender();

        internal MessageQueueSender(NetMQQueue<NetMQMessage> queue)
        {
            _queue = queue;
        }

        public void Send(Action<IOutgoingSocket> action)
        {
            lock (_gate)
            {
                action(_sender);
                _sender.Queue(_queue);
            }
        }

        private class Sender : IOutgoingSocket
        {
            private readonly List<NetMQFrame> _frames = new List<NetMQFrame>();
            private bool _more = true;

            bool IOutgoingSocket.TrySend(ref Msg msg, TimeSpan timeout, bool more)
            {
                if (!_more)
                {
                    throw new InvalidOperationException();
                }

                _frames.Add(new NetMQFrame(msg.Data, msg.Size));
                _more = more;
                return true;
            }

            internal void Queue(NetMQQueue<NetMQMessage> queue)
            {
                if (_more)
                {
                    throw new InvalidOperationException();
                }
                queue.Enqueue(new NetMQMessage(_frames));
                Reset();
            }

            internal void Reset()
            {
                _frames.Clear();
                _more = true;
            }
        }
    }
}