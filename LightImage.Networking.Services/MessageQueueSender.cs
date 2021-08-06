using System;
using System.Collections.Generic;
using NetMQ;

namespace LightImage.Networking.Services
{
    public class MessageQueueSender : IOutgoingSender, ISocketPollable, IDisposable
    {
        private readonly object _gate = new object();
        private readonly NetMQQueue<NetMQMessage> _queue = new NetMQQueue<NetMQMessage>();
        private readonly Sender _sender = new Sender();
        private readonly IOutgoingSocket _socket;

        public MessageQueueSender(IOutgoingSocket socket)
        {
            _socket = socket;
            _queue.ReceiveReady += HandleQueue_ReceiveReady;
        }

        /// <inheritdoc/>
        bool ISocketPollable.IsDisposed => _queue.IsDisposed;

        /// <inheritdoc/>
        NetMQSocket ISocketPollable.Socket => ((ISocketPollable)_queue).Socket;

        /// <inheritdoc/>
        public void Dispose()
        {
            _queue.Dispose();
        }

        public void Send(Action<IOutgoingSocket> action)
        {
            lock (_gate)
            {
                action(_sender);
                _sender.Queue(_queue);
            }
        }

        private void HandleQueue_ReceiveReady(object sender, NetMQQueueEventArgs<NetMQMessage> e)
        {
            while (_queue.TryDequeue(out var msg, TimeSpan.Zero))
            {
                _socket.SendMultipartMessage(msg);
                foreach (var frame in msg)
                {
                    BufferPool.Return(frame.Buffer);
                }
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

                var bytes = BufferPool.Take(msg.Size);
                msg.Slice(0, msg.Size).CopyTo(bytes.AsSpan());
                _frames.Add(new NetMQFrame(bytes, msg.Size));
                _more = more;
                return true;
            }

            internal void Queue(NetMQQueue<NetMQMessage> queue)
            {
                if (_more)
                {
                    throw new InvalidOperationException();
                }

                var msg = new NetMQMessage(_frames);
                queue.Enqueue(msg);
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