using System;
using LightImage.Networking.Services;
using NetMQ;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Utility methods for NetMQ sockets.
    /// </summary>
    public static class NetMQExtensions
    {
        /// <summary>
        /// Sends an integer value as a single frame.
        /// </summary>
        /// <param name="socket">Socket on which to send the value.</param>
        /// <param name="value">Integer value to be sent.</param>
        /// <param name="more">Value indicating whether more frames will follow.</param>
        /// <returns>The outgoing socket.</returns>
        public static IOutgoingSocket SendFrame(this IOutgoingSocket socket, int value, bool more = false)
        {
            var bytes = new byte[4];
            NetworkOrderBitsConverter.PutInt32(value, bytes);
            socket.SendFrame(bytes, more);
            return socket;
        }
    }
}