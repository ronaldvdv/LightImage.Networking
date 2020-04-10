namespace LightImage.Networking.Services
{
    /// <summary>
    /// Logging events for clustering services.
    /// </summary>
    public static class ClusterEvents
    {
        /// <summary>
        /// The service connects to a peer service.
        /// </summary>
        public const int Connect = 2;

        /// <summary>
        /// The service disconnects from a peer service.
        /// </summary>
        public const int Disconnect = 3;

        /// <summary>
        /// The service is initializing.
        /// </summary>
        public const int Initializing = 1;

        /// <summary>
        /// The service is stopping.
        /// </summary>
        public const int Stopping = 4;
    }
}