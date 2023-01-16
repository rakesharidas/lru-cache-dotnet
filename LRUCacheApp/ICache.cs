using System;
namespace LRUCacheApp
{
    public interface ICache<TKey, TVal> where TKey : notnull
    {
        public void Put(TKey key, TVal val);

        public TVal? Get(TKey key);

        public TVal? Remove(TKey key);

        public bool Contains(TKey key);

        public void Clear();

        public int Count();

    }
}

