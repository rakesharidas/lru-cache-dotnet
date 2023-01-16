using System;
namespace LRUCacheApp
{
    public class ConcurrentLRUCache<TKey, TVal> : ICache<TKey, TVal> where TKey : notnull
    {
        private readonly IDictionary<TKey, Node<TKey, TVal>> dict = new Dictionary<TKey, Node<TKey, TVal>>();

        private readonly LinkedNode<TKey, TVal> linkedNode = new();

        private readonly int maxCapacity;

        private readonly static object instanceLock = new();

        private static ConcurrentLRUCache<TKey, TVal>? concurrentLRUCache;

        private ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

        private ConcurrentLRUCache(int MaxCapacity)
        {
            this.maxCapacity = MaxCapacity;
            Console.WriteLine(int.MinValue);

        }


        public static ConcurrentLRUCache<TKey, TVal> Create(int maxCapacity)
        {
            if (maxCapacity < 1)
            {
                throw new ArgumentException("Max capacity shouldn't be less than 1");
            }
            lock (instanceLock)
            {
                concurrentLRUCache ??= new ConcurrentLRUCache<TKey, TVal>(maxCapacity);
            }
            return concurrentLRUCache;

        }

        public static void InvalidateCache()
        {
            lock (instanceLock)
            {
                concurrentLRUCache?.Clear();
                concurrentLRUCache = null;
            }
        }


        void ICache<TKey, TVal>.Put(TKey key, TVal val)
        {
            cacheLock.EnterWriteLock();
            try
            {
                if (dict.Count == maxCapacity)
                {
                    Node<TKey, TVal>? node = this.linkedNode.RemoveLastNode();
                    if (node != null)
                    {
                        dict.Remove(node.Key);
                    }

                }
                if (dict.ContainsKey(key))
                {
                    Node<TKey, TVal> existingNode = dict[key];
                    Node<TKey, TVal> newNode = new(key, val, existingNode.Prev, existingNode.Next);
                    linkedNode.RemoveNode(existingNode);
                    linkedNode.AddFirstNode(newNode);
                    dict[key] = newNode;

                }
                else
                {
                    Node<TKey, TVal> newNode = new Node<TKey, TVal>(key, val);
                    linkedNode.AddFirstNode(newNode);
                    dict[key] = newNode;

                }
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }

        }

        TVal? ICache<TKey, TVal>.Get(TKey key)
        {
            cacheLock.EnterReadLock();
            try
            {
                Node<TKey, TVal> node = this.dict[key];
                if (node != null)
                {
                    this.linkedNode.MoveToTop(node);
                    return node.Val;
                }
                return default;
            }
            finally
            {
                cacheLock.ExitReadLock();

            }

        }

        TVal? ICache<TKey, TVal>.Remove(TKey key)
        {
            cacheLock.EnterWriteLock();
            try
            {
                if (dict.ContainsKey(key))
                {
                    Node<TKey, TVal> node = dict[key];
                    this.linkedNode.RemoveNode(node);
                    this.dict.Remove(key);
                    return node.Val;
                }
                return default;
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }

        }

        public bool Contains(TKey key)
        {
            cacheLock.EnterReadLock();
            try
            {
                return this.dict.ContainsKey(key);
            }
            finally
            {
                cacheLock.ExitReadLock();
            }
        }

        public void Clear()
        {
            cacheLock.EnterWriteLock();
            try
            {
                this.dict.Clear();
                this.linkedNode.Reset();
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }

        }

        public int Count()
        {
            cacheLock.EnterReadLock();
            try
            {
                return this.dict.Count;
            }
            finally
            {
                cacheLock.ExitReadLock();
            }

        }
    }

    class Node<TKey, TVal> where TKey : notnull
    {
        internal TKey Key;
        internal TVal Val;
        internal Node<TKey, TVal>? Prev;
        internal Node<TKey, TVal>? Next;

        internal Node(TKey Key, TVal Val) : this(Key, Val, null, null)
        {

        }

        internal Node(TKey Key, TVal Val, Node<TKey, TVal>? Prev, Node<TKey, TVal>? Next)
        {
            this.Key = Key;
            this.Val = Val;
            this.Prev = Prev;
            this.Next = Next;
        }

    }

    class LinkedNode<TKey, TVal> where TKey : notnull
    {
        internal Node<TKey, TVal>? First;
        internal Node<TKey, TVal>? Last;

        internal void AddFirstNode(Node<TKey, TVal> Node)
        {
            Node.Prev = null;
            Node.Next = First;
            if (First != null)
            {
                First.Prev = Node;
            }
            else
            {
                MarkNodeAsLast(Node);
            }
            First = Node;
        }

        internal void RemoveNode(Node<TKey, TVal> node)
        {
            if (IsLastNode(node))
            {
                RemoveLastNode();
            }
            else
            {
                LinkedNode<TKey, TVal>.RemoveMidNode(node);
            }
        }


        internal Node<TKey, TVal>? RemoveLastNode()
        {
            Node<TKey, TVal>? curLastNode = this.Last;
            if (curLastNode != null)
            {
                Node<TKey, TVal>? NewLastNode = curLastNode?.Prev;
                MarkNodeAsLast(NewLastNode);
                LinkedNode<TKey, TVal>.DelinkNode(curLastNode);
            }
            return curLastNode;
        }

        internal static void RemoveMidNode(Node<TKey, TVal> Node)
        {
            Node<TKey, TVal>? PrevNode = Node.Prev;
            Node<TKey, TVal>? NextNode = Node.Next;
            if (PrevNode != null)
            {
                PrevNode.Next = NextNode;
            }
            if (NextNode != null)
            {
                NextNode.Prev = PrevNode;
            }
            LinkedNode<TKey, TVal>.DelinkNode(Node);
        }

        internal void MoveToTop(Node<TKey, TVal> Node)
        {
            RemoveNode(Node);
            AddFirstNode(Node);
        }

        internal void Reset()
        {
            this.First = null;
            this.Last = null;
        }

        private void MarkNodeAsLast(Node<TKey, TVal>? node)
        {
            this.Last = node;
            if (node != null)
            {
                node.Next = null;
            }
        }

        private static void DelinkNode(Node<TKey, TVal>? Node)
        {
            if (Node != null)
            {
                Node.Next = null;
                Node.Prev = null;
            }

        }

        private bool IsLastNode(Node<TKey, TVal> node)
        {
            if (this.Last == null || node == null || node.Next != null)
            {
                return false;
            }
            else
            {
                return Last.Key.Equals(node.Key);
            }
        }

    }
}

