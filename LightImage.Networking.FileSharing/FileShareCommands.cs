using LightImage.Networking.Services;
using NetMQ;

namespace LightImage.Networking.FileSharing
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

        public static void SendAdd(this IOutgoingSocket socket, FileDescriptor descriptor, string path)
        {
            socket.SendMoreFrame(C_CMD_ADD);
            socket.SendMoreFrame(descriptor.ToByteArray());
            socket.SendFrame(path);
        }

        public static void SendAdd(IOutgoingSender sender, FileDescriptor descriptor, string path)
        {
            sender.Send(socket => socket.SendAdd(descriptor, path));
        }

        public static void SendCancel(IOutgoingSender sender, FileDescriptor descriptor)
        {
            sender.Send(socket => socket.SendCancel(descriptor));
        }

        public static void SendCancel(this IOutgoingSocket socket, FileDescriptor descriptor)
        {
            socket.SendMoreFrame(C_CMD_CANCEL);
            socket.SendFrame(descriptor.ToByteArray());
        }

        public static void SendRemove(IOutgoingSender sender, FileDescriptor descriptor)
        {
            sender.Send(socket => socket.SendRemove(descriptor));
        }

        public static void SendRemove(this IOutgoingSocket socket, FileDescriptor descriptor)
        {
            socket.SendMoreFrame(C_CMD_REMOVE);
            socket.SendFrame(descriptor.ToByteArray());
        }

        public static void SendRequest(IOutgoingSender sender, FileDescriptor descriptor, string path)
        {
            sender.Send(socket => socket.SendRequest(descriptor, path));
        }

        public static void SendRequest(this IOutgoingSocket socket, FileDescriptor descriptor, string path)
        {
            socket.SendMoreFrame(C_CMD_REQUEST);
            socket.SendMoreFrame(descriptor.ToByteArray());
            socket.SendFrame(path);
        }
    }
}