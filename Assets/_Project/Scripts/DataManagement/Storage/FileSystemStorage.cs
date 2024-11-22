using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using StepUpTableTennis.DataManagement.Core.Models;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using UnityEngine;

namespace StepUpTableTennis.DataManagement.Storage
{
    public class TrainingDataStorage
    {
        private readonly string baseDirectory;
        private readonly JsonSerializerOptions jsonOptions;

        public TrainingDataStorage(string baseDirectory)
        {
            this.baseDirectory = baseDirectory;

            jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new Vector2JsonConverter(),
                    new Vector3JsonConverter(),
                    new QuaternionJsonConverter()
                }
            };

            InitializeDirectories();
        }

        public async Task SaveSessionAsync(TrainingSession session)
        {
            var sessionDir = GetSessionDirectory(session.Id);
            var shotsDir = Path.Combine(sessionDir, "shots");

            Directory.CreateDirectory(sessionDir);
            Directory.CreateDirectory(shotsDir);

            // セッション基本情報の保存
            var sessionInfo = new StoredSessionInfo
            {
                Id = session.Id.Value,
                StartTime = session.StartTime,
                Config = session.Config,
                Statistics = session.Statistics
            };

            await SaveJsonAsync(
                Path.Combine(sessionDir, "session.json"),
                sessionInfo
            );

            // 各ショットの保存
            for (var i = 0; i < session.Shots.Count; i++)
                await SaveShotAsync(
                    Path.Combine(shotsDir, $"{i}.json"),
                    session.Shots[i]
                );
        }

        public async Task<TrainingSession> LoadSessionAsync(SessionId sessionId)
        {
            var sessionDir = GetSessionDirectory(sessionId);
            var sessionPath = Path.Combine(sessionDir, "session.json");
            var shotsDir = Path.Combine(sessionDir, "shots");

            if (!File.Exists(sessionPath))
                throw new FileNotFoundException($"Session {sessionId.Value} not found");

            // セッション基本情報の読み込み
            var sessionInfo = await LoadJsonAsync<StoredSessionInfo>(sessionPath);

            // ショットデータの読み込み
            var shots = new List<TrainingShot>();
            var shotFiles = Directory.GetFiles(shotsDir, "*.json");
            Array.Sort(shotFiles); // ファイル名で順序を保証

            foreach (var shotFile in shotFiles)
            {
                var shot = await LoadShotAsync(shotFile);
                shots.Add(shot);
            }

            // TrainingSessionの再構築
            var session = new TrainingSession(
                sessionId,
                sessionInfo.StartTime,
                sessionInfo.Config,
                shots
            );

            if (sessionInfo.Statistics != null) session.Complete(sessionInfo.Statistics);

            return session;
        }

        private async Task SaveShotAsync(string path, TrainingShot shot)
        {
            var storedShot = new StoredShot
            {
                Parameters = shot.Parameters,
                ExecutedAt = shot.ExecutedAt,
                WasSuccessful = shot.WasSuccessful,
                BallMotionData = shot.BallMotionData,
                RacketMotionData = shot.RacketMotionData,
                HeadMotionData = shot.HeadMotionData,
                GazeData = shot.GazeData
            };

            await SaveJsonAsync(path, storedShot);
        }

        private async Task<TrainingShot> LoadShotAsync(string path)
        {
            var storedShot = await LoadJsonAsync<StoredShot>(path);

            var shot = new TrainingShot(storedShot.Parameters);

            if (storedShot.ExecutedAt.HasValue)
                shot.RecordExecution(storedShot.ExecutedAt.Value, storedShot.WasSuccessful ?? false);

            // 記録データの復元
            shot.BallMotionData.AddRange(storedShot.BallMotionData);
            shot.RacketMotionData.AddRange(storedShot.RacketMotionData);
            shot.HeadMotionData.AddRange(storedShot.HeadMotionData);
            shot.GazeData.AddRange(storedShot.GazeData);

            return shot;
        }

        private async Task SaveJsonAsync<T>(string path, T data)
        {
            var json = JsonSerializer.Serialize(data, jsonOptions);
            await File.WriteAllTextAsync(path, json);
        }

        private async Task<T> LoadJsonAsync<T>(string path)
        {
            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<T>(json, jsonOptions);
        }

        private string GetSessionDirectory(SessionId sessionId)
        {
            return Path.Combine(baseDirectory, "Sessions", sessionId.Value);
        }

        private void InitializeDirectories()
        {
            Directory.CreateDirectory(Path.Combine(baseDirectory, "Sessions"));
        }

        // 保存用のデータ構造
        private class StoredSessionInfo
        {
            public string Id { get; set; }
            public DateTime StartTime { get; set; }
            public SessionConfig Config { get; set; }
            public SessionStatistics Statistics { get; set; }
        }

        private class StoredShot
        {
            public ShotParameters Parameters { get; set; }
            public DateTime? ExecutedAt { get; set; }
            public bool? WasSuccessful { get; set; }
            public List<MotionRecordData> BallMotionData { get; set; }
            public List<MotionRecordData> RacketMotionData { get; set; }
            public List<MotionRecordData> HeadMotionData { get; set; }
            public List<GazeRecordData> GazeData { get; set; }
        }
    }

    // UnityのデータTYpe用のJsonConverter
    public class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var obj = JsonSerializer.Deserialize<Dictionary<string, float>>(ref reader);
            return new Vector2(obj["x"], obj["y"]);
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteEndObject();
        }
    }

    public class Vector3JsonConverter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var obj = JsonSerializer.Deserialize<Dictionary<string, float>>(ref reader);
            return new Vector3(obj["x"], obj["y"], obj["z"]);
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteEndObject();
        }
    }

    public class QuaternionJsonConverter : JsonConverter<Quaternion>
    {
        public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var obj = JsonSerializer.Deserialize<Dictionary<string, float>>(ref reader);
            return new Quaternion(obj["x"], obj["y"], obj["z"], obj["w"]);
        }

        public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteNumber("z", value.z);
            writer.WriteNumber("w", value.w);
            writer.WriteEndObject();
        }
    }
}