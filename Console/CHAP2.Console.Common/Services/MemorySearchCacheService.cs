using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using CHAP2.Domain.Entities;
using CHAP2.Console.Common.Interfaces;

namespace CHAP2.Console.Common.Services;

public class MemorySearchCacheService : ISearchCacheService
{
    private class CacheEntry
    {
        public List<Chorus> Results { get; set; } = new();
        public DateTimeOffset Expiry { get; set; }
    }

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGet(string searchTerm, out List<Chorus> results)
    {
        results = new List<Chorus>();
        if (_cache.TryGetValue(searchTerm, out var entry))
        {
            if (entry.Expiry > DateTimeOffset.UtcNow)
            {
                results = entry.Results;
                return true;
            }
            else
            {
                _cache.TryRemove(searchTerm, out _);
            }
        }
        return false;
    }

    public void Set(string searchTerm, List<Chorus> results, TimeSpan duration)
    {
        var entry = new CacheEntry
        {
            Results = results,
            Expiry = DateTimeOffset.UtcNow.Add(duration)
        };
        _cache[searchTerm] = entry;
    }
} 