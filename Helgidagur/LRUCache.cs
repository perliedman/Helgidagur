using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Helgidagur
{
    public class LRUCache<K, V>
    {
        private int capacity;
        private IDictionary<K, LinkedListNode<LRUCacheItem<K, V>>> cacheMap = new ConcurrentDictionary<K, LinkedListNode<LRUCacheItem<K, V>>>();
        private LinkedList<LRUCacheItem<K, V>> lruList = new LinkedList<LRUCacheItem<K, V>>();
        private Object mutex = new object();

        public LRUCache(int capacity)
        {
            this.capacity = capacity;
        }

        public V Get(K key)
        {
            LinkedListNode<LRUCacheItem<K, V>> node;
            if (cacheMap.TryGetValue(key, out node))
            {
                lock (mutex)
                {
                    lruList.Remove(node);
                    lruList.AddLast(node);
                }

                V value = node.Value.value;
                return value;
            }
            return default(V);
        }

        public void Add(K key, V val)
        {
            if (cacheMap.Count >= capacity)
            {
                RemoveFirst();
            }

            LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(key, val);
            LinkedListNode<LRUCacheItem<K, V>> node = new LinkedListNode<LRUCacheItem<K, V>>(cacheItem);

            lock (mutex)
            {
                lruList.AddLast(node);
            }
            cacheMap.Add(key, node);
        }

        private void RemoveFirst()
        {
            LinkedListNode<LRUCacheItem<K, V>> node;
            lock (mutex)
            {
                // Remove from LRUPriority
                node = lruList.First;
                lruList.RemoveFirst();
            }

            // Remove from cache
            cacheMap.Remove(node.Value.key);
        }
    }

    class LRUCacheItem<K, V>
    {
        public LRUCacheItem(K k, V v)
        {
            key = k;
            value = v;
        }
        public K key;
        public V value;
    }
}
