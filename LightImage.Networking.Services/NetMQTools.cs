using NetMQ;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// General tools for constructing NetMQ messages.
    /// </summary>
    public static class NetMQTools
    {
        /// <summary>
        /// Construct a <see cref="NetMQMessage"/> from one or more frames.
        /// </summary>
        /// <param name="frames">Frames to be combined into a message.</param>
        /// <returns>The combined message.</returns>
        public static NetMQMessage Message(params NetMQFrame[] frames)
        {
            // TODO Avoid building NetMQMessage by calling SendMoreFrame and SendFrame manually
            return new NetMQMessage(frames);
        }
    }
}