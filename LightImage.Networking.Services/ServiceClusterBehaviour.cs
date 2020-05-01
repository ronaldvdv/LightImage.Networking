namespace LightImage.Networking.Services
{
    /// <summary>
    /// Clustering behaviour options.
    /// </summary>
    public enum ServiceClusterBehaviour
    {
        /// <summary>
        /// Connects to every peer in the network.
        /// </summary>
        Always = 1,

        /// <summary>
        /// Only connects to peers in the same session.
        /// </summary>
        Session = 0,
    }
}