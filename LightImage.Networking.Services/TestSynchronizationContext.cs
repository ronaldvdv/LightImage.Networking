using System.Threading;

namespace LightImage.Networking.Services
{
    /// <summary>
    /// Synchronization context for testing.
    /// </summary>
    public sealed class TestSynchronizationContext : SynchronizationContext
    {
        /// <inheritdoc/>
        public override void Post(SendOrPostCallback d, object state) => d(state);

        /// <inheritdoc/>
        public override void Send(SendOrPostCallback d, object state) => d(state);
    }
}