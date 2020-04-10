namespace LightImage.Networking.Services
{
    /// <summary>
    /// General logging events for services.
    /// </summary>
    internal static class ServiceEvents
    {
        /// <summary>
        /// A service is connecting.
        /// </summary>
        public const int Connect = 3;

        /// <summary>
        /// A service is disconnecting.
        /// </summary>
        public const int Disconnect = 4;

        /// <summary>
        /// A service is resetting.
        /// </summary>
        public const int Reset = 5;

        /// <summary>
        /// A service is initializing.
        /// </summary>
        public const int Start = 1;

        /// <summary>
        /// A service is stopping.
        /// </summary>
        public const int Stop = 2;
    }
}