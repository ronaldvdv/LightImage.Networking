using LightImage.Util.Polly;
using System;
using System.Collections.Generic;

namespace LightImage.FileSharing
{
    /// <summary>
    /// Map storing the availability of a file at a collection of peers
    /// </summary>
    public class AvailabilityMap
    {
        private readonly Dictionary<Guid, PeerAvailability> _data = new Dictionary<Guid, PeerAvailability>();
        private readonly RetryPolicy _expiryPolicy;

        public AvailabilityMap(RetryPolicy expiryPolicy)
        {
            _expiryPolicy = expiryPolicy;
        }

        public Availability this[Guid peer] => Get(peer);

        /// <summary>
        /// Get availability at a specific peer
        /// </summary>
        /// <param name="peer">Peer identifier</param>
        /// <returns>Availability at the given peer</returns>
        public Availability Get(Guid peer)
        {
            if (_data.TryGetValue(peer, out var availability))
                return availability.Get();
            return Availability.AvailabilityExpired;
        }

        /// <summary>
        /// Remove availability data for a given peer
        /// </summary>
        /// <param name="peer">Peer identifier</param>
        public void Remove(Guid peer)
        {
            _data.Remove(peer);
        }

        /// <summary>
        /// Add or update availability for a specific peer
        /// </summary>
        /// <param name="peer">Peer identifier</param>
        /// <param name="isAvailable">Whether the file is available at the given peer</param>
        public void Set(Guid peer, bool isAvailable)
        {
            if (!_data.TryGetValue(peer, out var availability))
                availability = _data[peer] = new PeerAvailability(_expiryPolicy);
            availability.Update(isAvailable);
        }

        /// <summary>
        /// Class for tracking availability of a file at a single peer. The availability can be updated as a boolean.
        /// The number of consecutively recorded unavailabilities is counted. Unavailabilities expire a certain time
        /// after they've been recorded; this expiry interval may increase with each recorded unavaibility.
        /// </summary>
        private class PeerAvailability
        {
            private readonly RetryPolicy _policy;
            private int _unavailabilies = 0;

            public PeerAvailability(RetryPolicy policy)
            {
                _policy = policy;
            }

            public DateTime Expires { get; private set; } = DateTime.MinValue;
            public bool IsAvailable { get; private set; } = false;

            public Availability Get()
            {
                if (IsAvailable)
                    return Availability.Available;
                if (DateTime.Now > Expires)
                    return Availability.AvailabilityExpired;
                return Availability.Unavailable;
            }

            public void Update(bool available)
            {
                bool previousState = IsAvailable;
                IsAvailable = available;
                if (available)
                    _unavailabilies = 0;
                else
                {
                    var now = DateTime.Now;
                    if (previousState || now >= Expires)
                    {
                        _unavailabilies++;
                        TimeSpan interval = _policy.GetInterval(_unavailabilies - 1);
                        Expires = interval == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.Now + interval;
                    }
                }
            }
        }
    }
}