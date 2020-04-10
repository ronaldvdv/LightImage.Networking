using System;
using System.Threading;
using System.Threading.Tasks;
using LightImage.Networking.Discovery.Events;
using Nito.AsyncEx;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Extension methods for <see cref="IDiscoveryNode"/>.
    /// </summary>
    public static class DiscoveryExtensions
    {
        /// <summary>
        /// Wait for the node to join a specific session.
        /// </summary>
        /// <param name="node">Discovery node.</param>
        /// <param name="session">Session that should be joined.</param>
        /// <param name="ct">Cancellation token for the wait.</param>
        /// <returns>Task representing the join action.</returns>
        public static async Task JoinAsync(this IDiscoveryNode node, int session, CancellationToken ct = default)
        {
            var mre = new AsyncManualResetEvent();
            var eh = new EventHandler<SessionChangedEventArgs>((_, args) =>
            {
                if (args.Session == session)
                {
                    mre.Set();
                }
            });
            node.SessionChanged += eh;
            node.Join(session);
            await mre.WaitAsync(ct);
            node.SessionChanged -= eh;
        }

        /// <summary>
        /// Leave the current session.
        /// </summary>
        /// <param name="node">Discovery node.</param>
        public static void Leave(this IDiscoveryNode node)
        {
            node.Join(DiscoveryNode.C_NO_SESSION);
        }

        /// <summary>
        /// Wait for the node to leave its session.
        /// </summary>
        /// <param name="node">Discovery node.</param>
        /// <param name="ct">Cancellation token for the wait.</param>
        /// <returns>Task representing the leave action.</returns>
        public static Task LeaveAsync(this IDiscoveryNode node, CancellationToken ct = default)
        {
            return JoinAsync(node, DiscoveryNode.C_NO_SESSION, ct);
        }
    }
}