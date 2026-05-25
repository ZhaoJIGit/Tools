using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TaskManagerNew.Services
{
    /// <summary>
    /// 进程缓存
    /// </summary>
    public class ProcessCache
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new();
        private readonly TimeSpan _defaultExpiration;

        public ProcessCache(TimeSpan defaultExpiration)
        {
            _defaultExpiration = defaultExpiration;
        }

        /// <summary>
        /// 设置缓存项
        /// </summary>
        public void Set<T>(string key, T value, TimeSpan? expiration = null)
        {
            var expirationTime = DateTime.UtcNow.Add(expiration ?? _defaultExpiration);
            var item = new CacheItem(value, expirationTime);
            _cache[key] = item;
        }

        /// <summary>
        /// 获取缓存项
        /// </summary>
        public bool TryGetValue<T>(string key, out T? value)
        {
            value = default;

            if (!_cache.TryGetValue(key, out var item))
                return false;

            if (item.ExpirationTime < DateTime.UtcNow)
            {
                _cache.TryRemove(key, out _);
                return false;
            }

            if (item.Value is T typedValue)
            {
                value = typedValue;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 移除缓存项
        /// </summary>
        public bool Remove(string key)
        {
            return _cache.TryRemove(key, out _);
        }

        /// <summary>
        /// 清除所有缓存
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// 清理过期缓存
        /// </summary>
        public void Cleanup()
        {
            var now = DateTime.UtcNow;
            var keysToRemove = new List<string>();

            foreach (var kvp in _cache)
            {
                if (kvp.Value.ExpirationTime < now)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _cache.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            var now = DateTime.UtcNow;
            int total = _cache.Count;
            int expired = 0;

            foreach (var item in _cache.Values)
            {
                if (item.ExpirationTime < now)
                    expired++;
            }

            return new CacheStatistics
            {
                TotalItems = total,
                ExpiredItems = expired,
                ValidItems = total - expired
            };
        }

        /// <summary>
        /// 缓存项
        /// </summary>
        private class CacheItem
        {
            public object? Value { get; }
            public DateTime ExpirationTime { get; }

            public CacheItem(object? value, DateTime expirationTime)
            {
                Value = value;
                ExpirationTime = expirationTime;
            }
        }
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public class CacheStatistics
    {
        public int TotalItems { get; set; }
        public int ExpiredItems { get; set; }
        public int ValidItems { get; set; }
        public double ExpirationRate => TotalItems > 0 ? (double)ExpiredItems / TotalItems : 0;
    }
}