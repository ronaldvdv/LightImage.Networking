using LightImage.Networking.Services;
using NetMQ;
using System;
using System.Collections.Generic;

namespace LightImage.FileSharing
{
    public static class FileShareCommands
    {
        public const string C_CMD_ADD = "ADD";
        public const string C_CMD_CANCEL = "CANCEL";
        public const string C_CMD_REMOVE = "REMOVE";
        public const string C_CMD_REQUEST = "REQUEST";

        public static void ParseAdd(NetMQMessage message, out FileDescriptor descriptor, out string path, int offset = 0)
        {
            descriptor = FileDescriptor.Parse(message[offset + 1]);
            path = message[offset + 2].ConvertToString();
        }

        public static void ParseCancel(NetMQMessage message, out FileDescriptor descriptor, int offset = 0)
        {
            descriptor = FileDescriptor.Parse(message[offset + 1]);
        }

        public static void ParseRemove(NetMQMessage message, out FileDescriptor descriptor, int offset = 0)
        {
            descriptor = FileDescriptor.Parse(message[offset + 1]);
        }

        public static void ParseRequest(NetMQMessage message, out FileDescriptor descriptor, out string path, int offset = 0)
        {
            descriptor = FileDescriptor.Parse(message[offset + 1]);
            path = message[offset + 2].ConvertToString();
        }

        public static void SendAdd(IOutgoingSocket socket, FileDescriptor descriptor, string path)
        {
            var message = NetMQTools.Message(
                new NetMQFrame(C_CMD_ADD),
                new NetMQFrame(descriptor.ToByteArray()),
                new NetMQFrame(path)
            );
            socket.SendMultipartMessage(message);
        }

        public static void SendCancel(IOutgoingSocket socket, FileDescriptor descriptor)
        {
            var message = NetMQTools.Message(
                new NetMQFrame(C_CMD_CANCEL),
                new NetMQFrame(descriptor.ToByteArray())
            );
            socket.SendMultipartMessage(message);
        }

        public static void SendRemove(IOutgoingSocket socket, FileDescriptor descriptor)
        {
            var message = NetMQTools.Message(
                new NetMQFrame(C_CMD_REMOVE),
                new NetMQFrame(descriptor.ToByteArray())
            );
            socket.SendMultipartMessage(message);
        }

        public static void SendRequest(IOutgoingSocket socket, FileDescriptor descriptor, string path)
        {
            var message = NetMQTools.Message(
                new NetMQFrame(C_CMD_REQUEST),
                new NetMQFrame(descriptor.ToByteArray()),
                new NetMQFrame(path)
            );
            socket.SendMultipartMessage(message);
        }
    }
}