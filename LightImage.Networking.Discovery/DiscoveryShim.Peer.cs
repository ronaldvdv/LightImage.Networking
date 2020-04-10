using System;
using LightImage.Networking.Services;
using NetMQ.Sockets;

namespace LightImage.Networking.Discovery
{
    /// <summary>
    /// Shim handler for the actor running the discovery process.
    /// </summary>
    internal partial class DiscoveryShim
    {
        /// <summary>
        /// Description of a connected peer in the discovery process.
        /// </summary>
        protected class Peer : AbstractNode, IDisposable
        {
            private readonly DiscoveryShim _shim;
            private string _address;
            private DealerSocket _dealer;

            /// <summary>
            /// Initializes a new instance of the <see cref="Peer"/> class.
            /// </summary>
            /// <param name="peerId">Unique ID of the peer.</param>
            /// <param name="host">Host at which the peer is listening for discovery messages.</param>
            /// <param name="port">Port at which the peer is listening for discovery messages.</param>
            /// <param name="session">Current session of the peer.</param>
            /// <param name="sequence">Sequence number for the session updates.</param>
            /// <param name="name">Descriptive name of the peer.</param>
            /// <param name="type">Technical type of the peer.</param>
            /// <param name="shim">Reference to the discovery shim.</param>
            public Peer(Guid peerId, string host, int port, int session, int sequence, string name, string type, DiscoveryShim shim)
                : base(peerId, name, type, host, port)
            {
                _shim = shim;

                _dealer = new DealerSocket();
                _dealer.Options.Identity = shim._id.ToIdentity();

                Update(host, port, session, sequence, name, type, true);
            }

            /// <summary>
            /// Gets the moment at which the last message was received from this peer.
            /// </summary>
            public DateTime LastSeen { get; private set; }

            /// <summary>
            /// Gets the sequence number for session updates.
            /// </summary>
            public int Sequence { get; private set; }

            /// <summary>
            /// Gets the currenet session of this node.
            /// </summary>
            public int Session { get; private set; }

            /// <summary>
            /// Gets the current status of this node.
            /// </summary>
            public PeerStatus Status { get; private set; }

            /// <inheritdoc/>
            public void Dispose()
            {
                Status = PeerStatus.Lost;
                if (!string.IsNullOrWhiteSpace(_address))
                {
                    _dealer.Disconnect(_address);
                }

                _dealer.Dispose();
            }

            /// <summary>
            /// Mark the peer as being alive (recently).
            /// </summary>
            internal void MarkAlive()
            {
                LastSeen = DateTime.Now;
                Status = PeerStatus.Alive;
            }

            /// <summary>
            /// Instruct the peer to join our session.
            /// </summary>
            internal void SendJoin()
            {
                _dealer.SendJoinMessage();
            }

            /// <summary>
            /// Send a PING-OK messages to the peer.
            /// </summary>
            internal void SendPingOk()
            {
                MarkAlive();
                _dealer.SendPingOkMessage();
            }

            /// <summary>
            /// Mark the peer as evasive.
            /// </summary>
            internal void SetEvasive()
            {
                if (Status != PeerStatus.Alive)
                {
                    return;
                }

                Status = PeerStatus.Evasive;
                _dealer.SendPingMessage();
            }

            /// <summary>
            /// Update the peer information.
            /// </summary>
            /// <param name="host">Host name.</param>
            /// <param name="port">Port number.</param>
            /// <param name="session">Current session.</param>
            /// <param name="sequence">Session sequence number.</param>
            /// <param name="name">Name of the node.</param>
            /// <param name="type">Type of node.</param>
            /// <param name="initializing">Value indicating whether the peer is being initialized.</param>
            /// <param name="forceHandshake">Value indicating whether a HELLO message must be sent.</param>
            /// <returns>Value indicating whether any data has changed.</returns>
            internal bool Update(string host, int port, int session, int sequence, string name = null, string type = null, bool initializing = false, bool forceHandshake = false)
            {
                LastSeen = DateTime.Now;

                bool changed = false;

                if (Sequence > sequence)
                {
                    return false;
                }
                else
                {
                    Sequence = sequence;
                }

                if (initializing || host != Host || port != Port)
                {
                    Reconnect(host, port);
                    changed = true;
                }
                else if (forceHandshake)
                {
                    _shim.SendHello(_dealer, false);
                }

                if (session != Session)
                {
                    Session = session;
                    changed = true;
                }

                if (name != Name)
                {
                    Name = name;
                    changed = true;
                }

                if (type != Type)
                {
                    Type = type;
                    changed = true;
                }

                if (Status != PeerStatus.Alive)
                {
                    Status = PeerStatus.Alive;
                    changed = true;
                }

                return changed;
            }

            private void Reconnect(string host, int port)
            {
                if (!string.IsNullOrWhiteSpace(_address))
                {
                    _dealer.Disconnect(_address);
                }

                Host = host;
                Port = port;

                if (port > 0)
                {
                    _dealer.Connect(_address = $"tcp://{host}:{port}");
                    _shim.SendHello(_dealer, true);
                }
                else
                {
                    Status = PeerStatus.Lost;
                }
            }
        }
    }
}