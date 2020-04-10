namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Constants for peer connectivity status.
    /// </summary>
    internal enum PeerStatus
    {
        /// <summary>
        /// The peer is known to be connected.
        /// </summary>
        Alive,

        /// <summary>
        /// The peer is not responding to recent messages.
        /// </summary>
        Evasive,

        /// <summary>
        /// The peer is not connected anymore.
        /// </summary>
        Lost,
    }
}