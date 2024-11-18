using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StepUpTableTennis.DataManagement.Core.Interfaces;
using StepUpTableTennis.DataManagement.Core.Models.Session;
using StepUpTableTennis.DataManagement.Core.Models.Shot;
using StepUpTableTennis.DataManagement.Core.Models.TimeSeries;
using StepUpTableTennis.DataManagement.Core.Utils;
using UnityEngine;

namespace StepUpTableTennis.DataManagement.Storage.FileSystem
{
    public class FileSystemRepository : IDataRepository
    {
        private const string SESSION_DIR = "Sessions";
        private const string SHOT_DIR = "Shots";
        private const string MOTION_DIR = "MotionData";
        private readonly string baseDirectory;
        private readonly IDataCompression compression;

        public FileSystemRepository(string baseDirectory, IDataCompression compression)
        {
            this.baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            this.compression = compression ?? throw new ArgumentNullException(nameof(compression));
            InitializeDirectories();
        }

        public async Task<bool> SaveSessionAsync(SessionData session)
        {
            try
            {
                var filePath = GetSessionFilePath(session.Id);
                await SerializationHelper.SaveToFileAsync(session, filePath);
                Debug.Log($"Saved session {session.Id} to {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save session {session.Id}: {e.Message}");
                return false;
            }
        }

        public async Task<SessionData?> GetSessionAsync(SessionId sessionId)
        {
            try
            {
                var filePath = GetSessionFilePath(sessionId);
                if (!File.Exists(filePath))
                {
                    Debug.LogWarning($"Session file not found: {filePath}");
                    return null;
                }

                return await SerializationHelper.LoadFromFileAsync<SessionData>(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load session {sessionId}: {e.Message}");
                return null;
            }
        }

        public async Task<IReadOnlyList<SessionData>> GetSessionsInRangeAsync(DateTime start, DateTime end)
        {
            var sessions = new List<SessionData>();
            var sessionDir = Path.Combine(baseDirectory, SESSION_DIR);

            try
            {
                foreach (var file in Directory.GetFiles(sessionDir, "*.json"))
                {
                    var session = await SerializationHelper.LoadFromFileAsync<SessionData>(file);
                    if (session.Timestamp >= start && session.Timestamp <= end) sessions.Add(session);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load sessions in range: {e.Message}");
            }

            return sessions;
        }

        public async Task<bool> UpdateSessionResultsAsync(SessionId sessionId, SessionResults results)
        {
            try
            {
                var session = await GetSessionAsync(sessionId);
                if (session == null) return false;

                session.SetResults(results);
                return await SaveSessionAsync(session);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to update session results {sessionId}: {e.Message}");
                return false;
            }
        }

        public async Task<bool> SaveShotResultAsync(ShotResult result)
        {
            try
            {
                var filePath = GetShotFilePath(result.ShotId);
                await SerializationHelper.SaveToFileAsync(result, filePath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save shot result {result.ShotId}: {e.Message}");
                return false;
            }
        }

        public async Task<ShotResult?> GetShotResultAsync(ShotId shotId)
        {
            try
            {
                var filePath = GetShotFilePath(shotId);
                if (!File.Exists(filePath)) return null;

                return await SerializationHelper.LoadFromFileAsync<ShotResult>(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load shot result {shotId}: {e.Message}");
                return null;
            }
        }

        public async Task<IReadOnlyList<ShotResult>> GetShotResultsBySessionAsync(SessionId sessionId)
        {
            var results = new List<ShotResult>();
            var session = await GetSessionAsync(sessionId);
            if (session == null) return results;

            foreach (var plannedShot in session.PlannedShots)
            {
                var result = await GetShotResultAsync(plannedShot.Id);
                if (result != null) results.Add(result);
            }

            return results;
        }

        public async Task<bool> SaveMotionDataAsync(MotionData data)
        {
            try
            {
                var filePath = GetMotionFilePath(data.ShotId, data.Type);
                var compressedData = compression.CompressMotionData(data);
                await File.WriteAllBytesAsync(filePath, compressedData);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save motion data for shot {data.ShotId}: {e.Message}");
                return false;
            }
        }

        public async Task<MotionData?> GetMotionDataAsync(ShotId shotId, MotionData.TimeSeriesDataType type)
        {
            try
            {
                var filePath = GetMotionFilePath(shotId, type);
                if (!File.Exists(filePath)) return null;

                var compressedData = await File.ReadAllBytesAsync(filePath);
                return compression.DecompressMotionData(compressedData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load motion data for shot {shotId}: {e.Message}");
                return null;
            }
        }

        private void InitializeDirectories()
        {
            var directories = new[]
            {
                Path.Combine(baseDirectory, SESSION_DIR),
                Path.Combine(baseDirectory, SHOT_DIR),
                Path.Combine(baseDirectory, MOTION_DIR)
            };

            foreach (var dir in directories)
                try
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to create directory {dir}: {e.Message}");
                    throw;
                }
        }

        private string GetSessionFilePath(SessionId sessionId)
        {
            return Path.Combine(baseDirectory, SESSION_DIR, $"{sessionId.Value}.json");
        }

        private string GetShotFilePath(ShotId shotId)
        {
            return Path.Combine(baseDirectory, SHOT_DIR, $"{shotId.Value}.json");
        }

        private string GetMotionFilePath(ShotId shotId, MotionData.TimeSeriesDataType type)
        {
            return Path.Combine(baseDirectory, MOTION_DIR, $"{shotId.Value}_{type}.bin");
        }
    }
}