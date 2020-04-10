using System;
using NetMQ;
using NetMQ.Sockets;

namespace System
{
    /// <summary>
    /// Utilities for sending GUIDs over NetMQ.
    /// </summary>
    public static class GuidExtensions
    {
        /// <summary>
        /// Convert (part of) a byte array to NetMQ.
        /// </summary>
        /// <param name="bytes">Byte array.</param>
        /// <param name="index">Starting index of the Guid in the array.</param>
        /// <returns>Decoded GUID.</returns>
        public static Guid ToGuid(this byte[] bytes, int index = 0)
        {
            var buffer = new byte[16];
            Array.Copy(bytes, index, buffer, 0, 16);
            return new Guid(buffer);
        }

        /// <summary>
        /// Converts a GUID to a NetMQ socket identity.
        /// </summary>
        /// <param name="id">GUID to be converted.</param>
        /// <returns>Identity for use in a socket.</returns>
        /// <remarks>Identities may not start with a 0, so we prepend a 1.</remarks>
        public static byte[] ToIdentity(this Guid id)
        {
            var identity = new byte[17];
            identity[0] = 1;
            id.ToByteArray().CopyTo(identity, 1);
            return identity;
        }

        /// <summary>
        /// Converts a GUID to a NetMQ frame.
        /// </summary>
        /// <param name="guid">GUID to be converted.</param>
        /// <returns>NetMQFrame containing the encoded GUID.</returns>
        public static NetMQFrame ToIdentityFrame(this Guid guid)
        {
            return new NetMQFrame(guid.ToIdentity());
        }

        /// <summary>
        /// Converts the first NetMQFrame in a message from a <see cref="RouterSocket"/> to a GUID.
        /// </summary>
        /// <param name="frame">Frame to be decoded.</param>
        /// <returns>Decoded GUID.</returns>
        /// <remarks>Since the <see cref="RouterSocket"/> includes the identity as the first frame, we can decode the GUID by skipping the first byte.</remarks>
        public static Guid ToIdentityGuid(this NetMQFrame frame) => frame.Buffer.ToGuid(1);
    }
}