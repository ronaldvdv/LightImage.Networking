using System;
using System.Collections.Generic;

namespace LightImage.Util
{
    /// <summary>
    /// A least-recently-used cache stored like a dictionary.
    /// </summary>
    /// <typeparam name="TKey">
    /// The type of the key to the cached item
    /// </typeparam>
    /// <typeparam name="TValue">
    /// The type of the cached item.
    /// </typeparam>
    /// <remarks>
    /// From https://stackoverflow.com/a/43266537/989129
    /// Derived from https://stackoverflow.com/a/3719378/240845
    /// </remarks>
    public class LruCache<TKey, TValue>
    {
        public const int C_DEFAULT_CAPACITY = 32;

        private readonly Action<TValue> _dispose;
        private readonly LinkedList<LruCacheItem> _list = new LinkedList<LruCacheItem>();
        private readonly Dictionary<TKey, LinkedListNode<LruCacheItem>> _map = new Dictionary<TKey, LinkedListNode<LruCacheItem>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="LruCache{TKey, TValue}"/>
        /// class.
        /// </summary>
        /// <param name="capacity">
        /// Maximum number of elements to cache.
        /// </param>
        /// <param name="dispose">
        /// When elements cycle out of the cache, disposes them. May be null.
        /// </param>
        public LruCache(int capacity = C_DEFAULT_CAPACITY, Action<TValue> dispose = null)
        {
            Capacity = capacity;
            _dispose = dispose;
        }

        /// <summary>
        /// Gets the capacity of the cache.
        /// </summary>
        public int Capacity { get; }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        /// <param name="key">
        /// The key of the element to add.
        /// </param>
        /// <param name="value">
        /// The value of the element to add. The value can be null for reference types.
        /// </param>
        public void Add(TKey key, TValue value)
        {
            lock (_map)
            {
                if (_map.Count >= Capacity)
                {
                    RemoveFirst();
                }

                var cacheItem = new LruCacheItem(key, value);
                var node =
                    new LinkedListNode<LruCacheItem>(cacheItem);
                _list.AddLast(node);
                _map.Add(key, node);
            }
        }

        /// <summary>
        /// Looks for a value for the matching <paramref name="key"/>. If not found,
        /// calls <paramref name="valueGenerator"/> to retrieve the value and add it to
        /// the cache.
        /// </summary>
        /// <param name="key">
        /// The key of the value to look up.
        /// </param>
        /// <param name="valueGenerator">
        /// Generates a value if one isn't found.
        /// </param>
        /// <returns>
        /// The requested value.
        /// </returns>
        public TValue Get(TKey key, Func<TKey, TValue> valueGenerator)
        {
            lock (_map)
            {
                LinkedListNode<LruCacheItem> node;
                TValue value;
                if (_map.TryGetValue(key, out node))
                {
                    value = node.Value.Value;
                    _list.Remove(node);
                    _list.AddLast(node);
                }
                else
                {
                    value = valueGenerator(key);
                    if (_map.Count >= Capacity)
                    {
                        RemoveFirst();
                    }

                    var cacheItem = new LruCacheItem(key, value);
                    node = new LinkedListNode<LruCacheItem>(cacheItem);
                    _list.AddLast(node);
                    _map.Add(key, node);
                }

                return value;
            }
        }

        /// <summary>Gets the value associated with the specified key.</summary>
        /// <param name="key">
        /// The key of the value to get.
        /// </param>
        /// <param name="value">
        /// When this method returns, contains the value associated with the specified
        /// key, if the key is found; otherwise, the default value for the type of the
        /// <paramref name="value" /> parameter. This parameter is passed
        /// uninitialized.
        /// </param>
        /// <returns>
        /// true if the <see cref="T:System.Collections.Generic.Dictionary`2" />
        /// contains an element with the specified key; otherwise, false.
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_map)
            {
                LinkedListNode<LruCacheItem> node;
                if (_map.TryGetValue(key, out node))
                {
                    value = node.Value.Value;
                    _list.Remove(node);
                    _list.AddLast(node);
                    return true;
                }

                value = default;
                return false;
            }
        }

        private void RemoveFirst()
        {
            // Remove from LRUPriority
            LinkedListNode<LruCacheItem> node = _list.First;
            _list.RemoveFirst();

            // Remove from cache
            _map.Remove(node.Value.Key);

            // dispose
            _dispose?.Invoke(node.Value.Value);
        }

        private class LruCacheItem
        {
            public LruCacheItem(TKey k, TValue v)
            {
                Key = k;
                Value = v;
            }

            public TKey Key { get; }

            public TValue Value { get; }
        }
    }
}