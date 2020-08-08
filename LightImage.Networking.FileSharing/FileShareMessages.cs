using LightImage.Networking.Services;
using NetMQ;
using NetMQ.Sockets;
using System;

namespace LightImage.Networking.FileSharing
{
    public static class FileShareMessages
    {
        public const string C_EVT_FINISHED = "FINISHED";
        public const string C_EVT_PROGRESS = "PROGRESS";
        public const string C_MSG_CHUNK = "CHUNK";
        public const string C_MSG_GET = "GET";
        public const string C_MSG_MISSING = "MISSING";

        public static void ParseChunk(NetMQMessage message, out FileDescriptor descriptor, out ChunkRange chunk, out byte[] data, int offset = 1)
        {
            descriptor = FileDescriptor.Parse(message[offset + 1]);
            chunk = ChunkRange.Parse(message[offset + 2]);
            data = message[offset + 3].Buffer;
        }

        public static void ParseFinished(NetMQMessage message, out DownloadOutcome outcome, out FileDescriptor descriptor, out string path, int offset = 0)
        {
            outcome = (DownloadOutcome)BitConverter.ToInt32(message[1].Buffer, 0);
            descriptor = FileDescriptor.Parse(message[offset + 2]);
            path = message[offset + 3].ConvertToString();
        }

        public static void ParseGet(NetMQMessage message, out FileDescriptor descriptor, out ChunkRange chunk, int offset = 1)
        {
            descriptor = FileDescriptor.Parse(message[offset + 1]);
            chunk = ChunkRange.Parse(message[offset + 2]);
        }

        public static void ParseMissing(NetMQMessage message, out FileDescriptor descriptor, out ChunkRange chunk, int offset = 1)
        {
            descriptor = FileDescriptor.Parse(message[offset + 1]);
            chunk = ChunkRange.Parse(message[offset + 2]);
        }

        public static void ParseProgress(NetMQMessage message, out FileDescriptor descriptor, out long bytesReceived, int offset = 0)
        {
            descriptor = FileDescriptor.Parse(message[offset + 1]);
            bytesReceived = BitConverter.ToInt64(message[offset + 2].Buffer, 0);
        }

        public static void SendChunk(IOutgoingSocket socket, FileDescriptor descriptor, ChunkRange chunk, byte[] data)
        {
            socket.SendMoreFrame(C_MSG_CHUNK);
            socket.SendMoreFrame(descriptor.ToByteArray());
            socket.SendMoreFrame(chunk.ToByteArray());
            socket.SendFrame(data, chunk.Size);
        }

        public static void SendFinished(IOutgoingSender sender, DownloadOutcome outcome, FileDescriptor descriptor, string path)
        {
            sender.Send(socket =>
            {
                socket.SendMoreFrame(C_EVT_FINISHED);
                socket.SendMoreFrame(BitConverter.GetBytes((int)outcome));
                socket.SendMoreFrame(descriptor.ToByteArray());
                socket.SendFrame(path ?? "");
            });
        }

        public static void SendGet(IOutgoingSocket socket, FileDescriptor descriptor, ChunkRange chunk)
        {
            socket.SendMoreFrame(C_MSG_GET);
            socket.SendMoreFrame(descriptor.ToByteArray());
            socket.SendFrame(chunk.ToByteArray());
        }

        public static void SendMissing(IOutgoingSocket socket, FileDescriptor descriptor, ChunkRange chunk)
        {
            socket.SendMoreFrame(C_MSG_MISSING);
            socket.SendMoreFrame(descriptor.ToByteArray());
            socket.SendFrame(chunk.ToByteArray());
        }

        public static void SendProgress(IOutgoingSender sender, FileDescriptor descriptor, long bytesReceived)
        {
            sender.Send(socket =>
            {
                socket.SendMoreFrame(C_EVT_PROGRESS);
                socket.SendMoreFrame(descriptor.ToByteArray());
                socket.SendFrame(BitConverter.GetBytes(bytesReceived));
            });
        }
    }
}