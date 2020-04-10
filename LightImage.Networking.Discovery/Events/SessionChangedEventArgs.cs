using System;

namespace LightImage.Networking.Discovery.Events
{
    /// <summary>
    /// Event arguments for the <see cref="DiscoveryNode.SessionChanged"/> event.
    /// </summary>
    public class SessionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SessionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="session">Current session of the node.</param>
        public SessionChangedEventArgs(int session)
        {
            Session = session;
        }

        /// <summary>
        /// Gets the current session of the node.
        /// </summary>
        public int Session { get; }
    }
}