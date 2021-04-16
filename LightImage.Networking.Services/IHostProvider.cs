namespace LightImage.Networking.Services
{
    /// <summary>
    /// Service for picking a host from all available network adapters.
    /// </summary>
    public interface IHostProvider
    {
        /// <summary>
        /// Picks a host from the available network adapters.
        /// </summary>
        /// <returns>Host (IP address).</returns>
        string GetHost();
    }
}