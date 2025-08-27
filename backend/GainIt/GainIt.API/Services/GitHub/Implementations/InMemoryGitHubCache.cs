using System.Collections.Concurrent;

namespace GainIt.API.Services.GitHub.Implementations
{
    internal static class InMemoryGitHubCache
    {
        private class CacheEntry
        {
            public object Value { get; set; } = default!;
            public DateTime ExpiresAtUtc { get; set; }
        }

        private static readonly ConcurrentDictionary<string, CacheEntry> s_cache = new();

        public static (bool HasValue, T? Value) TryGet<T>(string key)
        {
            if (s_cache.TryGetValue(key, out var entry))
            {
                if (entry.ExpiresAtUtc > DateTime.UtcNow && entry.Value is T casted)
                {
                    return (true, casted);
                }
                s_cache.TryRemove(key, out _);
            }

            return (false, default);
        }

        public static void Set<T>(string key, T value, TimeSpan ttl)
        {
            var entry = new CacheEntry
            {
                Value = value!,
                ExpiresAtUtc = DateTime.UtcNow.Add(ttl)
            };

            s_cache[key] = entry;
        }
    }
}


