using System;
using System.Collections.Generic;

namespace Core
{
    public interface IIndex<K, V>
    {
        // Create new entry this index that maps key K to value V
        void Insert(K Key, V Value);

        // Find an entry by key
        Tuple<K, V> Get(K Key);

        // Find all endtries that contain a key larger than or equal to specified key
        IEnumerable<Tuple<K, V>> LargerThanOrEqualTo(K Key);

        // Find all entries that contain a key larger than specified key
        IEnumerable<Tuple<K, V>> LargerThan(T Key);

        // Find all entries that contain a key less than or equal specified key
        IEnumerable<Tuple<K, V>> LessThanOrEqaulTo(K key);

        // Find all entries that contain a key less than specified key
        IEnumerable<Tuple<K, V>> LessThan(K key);

        // Delete an entry from this inde, optionally use specified IComparer to compare values
        bool Delete(K key, V value, IComparer<V> valueComparer = null);

        // Delete all entries of given key
        bool Delete(K key);
    }
}
