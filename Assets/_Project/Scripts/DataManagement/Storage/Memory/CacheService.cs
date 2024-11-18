using System;
using System.Collections.Concurrent;
using System.Linq;
using StepUpTableTennis.DataManagement.Core.Models.Session;
using StepUpTableTennis.DataManagement.Core.Models.Shot;
using StepUpTableTennis.DataManagement.Core.Models.TimeSeries;
using UnityEngine;

namespace StepUpTableTennis.DataManagement.Storage.Memory
{
    public class CacheService
    {
        private readonly int maxCacheSize;
        private readonly ConcurrentDictionary<string, MotionData> motionDataCache;
        private readonly ConcurrentDictionary<string, SessionData> sessionCache;
        private readonly ConcurrentDictionary<string, ShotResult> shotResultCache;

        public CacheService(int maxCacheSize = 100)
        {
            this.maxCacheSize = maxCacheSize;
            sessionCache = new ConcurrentDictionary<string, SessionData>();
            shotResultCache = new ConcurrentDictionary<string, ShotResult>();
            motionDataCache = new ConcurrentDictionary<string, MotionData>();
        }

        public void CacheSession(SessionData session)
        {
            if (sessionCache.Count >= maxCacheSize)
            {
                // 最も古いセッションを削除
                var oldest = sessionCache.OrderBy(x => x.Value.Timestamp).First();
                sessionCache.TryRemove(oldest.Key, out _);
            }

            sessionCache.AddOrUpdate(
                session.Id.Value,
                session,
                (_, _) => session
            );
        }

        public SessionData? GetCachedSession(SessionId sessionId)
        {
            return sessionCache.TryGetValue(sessionId.Value, out var session) ? session : null;
        }

        public void CacheShotResult(ShotResult result)
        {
            if (shotResultCache.Count >= maxCacheSize)
            {
                var oldest = shotResultCache.OrderBy(x => x.Value.Timestamp).First();
                shotResultCache.TryRemove(oldest.Key, out _);
            }

            shotResultCache.AddOrUpdate(
                result.ShotId.Value,
                result,
                (_, _) => result
            );
        }

        public ShotResult? GetCachedShotResult(ShotId shotId)
        {
            return shotResultCache.TryGetValue(shotId.Value, out var result) ? result : null;
        }

        public void CacheMotionData(MotionData data)
        {
            var key = GetMotionDataKey(data.ShotId, data.Type);

            if (motionDataCache.Count >= maxCacheSize)
            {
                var oldestKey = motionDataCache.Keys.First();
                motionDataCache.TryRemove(oldestKey, out _);
            }

            motionDataCache.AddOrUpdate(
                key,
                data,
                (_, _) => data
            );
        }

        public MotionData? GetCachedMotionData(ShotId shotId, MotionData.TimeSeriesDataType type)
        {
            var key = GetMotionDataKey(shotId, type);
            return motionDataCache.TryGetValue(key, out var data) ? data : null;
        }

        public void ClearCache()
        {
            sessionCache.Clear();
            shotResultCache.Clear();
            motionDataCache.Clear();
            Debug.Log("Cache cleared");
        }

        private static string GetMotionDataKey(ShotId shotId, MotionData.TimeSeriesDataType type)
        {
            return $"{shotId.Value}_{type}";
        }

        public CacheStats GetStats()
        {
            return new CacheStats
            {
                SessionCount = sessionCache.Count,
                ShotResultCount = shotResultCache.Count,
                MotionDataCount = motionDataCache.Count,
                MaxSize = maxCacheSize,
                OldestSessionTimestamp = sessionCache.Values
                    .Select(x => x.Timestamp)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Min(),
                NewestSessionTimestamp = sessionCache.Values
                    .Select(x => x.Timestamp)
                    .DefaultIfEmpty(DateTime.MinValue)
                    .Max()
            };
        }
    }

    public class CacheStats
    {
        public int SessionCount { get; set; }
        public int ShotResultCount { get; set; }
        public int MotionDataCount { get; set; }
        public int MaxSize { get; set; }
        public DateTime OldestSessionTimestamp { get; set; }
        public DateTime NewestSessionTimestamp { get; set; }
    }
}