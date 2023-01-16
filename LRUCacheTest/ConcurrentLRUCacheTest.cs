using LRUCacheApp;
namespace LRUCacheTest;

public class ConcurrentLRUCacheTest : IDisposable
{

    [Fact]
    public void TestPut()
    {
        Console.WriteLine(" Running testPut ");
        ICache<String, String> cache = ConcurrentLRUCache<String, String>.Create(3);

        cache.Put("a", "abc");
        cache.Put("b", "bbc");
        cache.Put("c", "cbc");
        cache.Put("d", "dbc");

        Assert.True(cache.Contains("b"));
        Assert.True(cache.Contains("c"));
        Assert.True(cache.Contains("d"));
        Assert.False(cache.Contains("a"));

        // b is the new lru, but put a different value that should keep d still in cache.
        cache.Put("b", "bbc-x");
        cache.Put("a", "abc"); // a is back too.
        Assert.True(cache.Contains("b"));
        Assert.True(cache.Contains("a"));
        Assert.Equal("bbc-x", cache.Get("b"));

    }

    [Fact]
    public void TestGetAndPut()
    {
        ICache<String, String> cache = ConcurrentLRUCache<String, String>.Create(3);

        cache.Put("a", "abc");
        cache.Put("b", "bbc");
        cache.Put("c", "cbc");
        String? val = cache.Get("a");
        cache.Put("d", "dbc");

        Assert.Equal(3, cache.Count());

        Assert.False(cache.Contains("b"));
        Assert.True(cache.Contains("a"));
        Assert.Equal("abc", val);

        cache.Put("a", "abc-x");
        Assert.Equal("abc-x", cache.Get("a"));


    }

    [Fact]
    public void testEdgeCases()
    {
        Assert.Throws<ArgumentException>(() => ConcurrentLRUCache<String, String>.Create(0));

        ICache<String, String> cache = ConcurrentLRUCache<String, String>.Create(1);
        cache.Put("a", "abc");
        cache.Put("b", "bbc");
        Assert.Equal(1, cache.Count());

        Assert.True(cache.Contains("b"));
    }

    [Fact]
    public void testRemove()
    {
        ICache<String, String> cache = ConcurrentLRUCache<String, String>.Create(3);

        cache.Put("a", "abc");
        cache.Put("b", "bbc");
        cache.Put("c", "cbc");
        cache.Remove("b");
        cache.Put("d", "dbc");

        Assert.False(cache.Contains("b"));
        Assert.True(cache.Contains("d"));

    }

    public void Dispose()
    {
        Console.WriteLine(" Invalidating cache ");
        ConcurrentLRUCache<String, String>.InvalidateCache();
    }


}

