using System;
using System.Linq;
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
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                Converters =
                {
                    new Vector2JsonConverter(),
                    new Vector3JsonConverter(),
                    new QuaternionJsonConverter(),
                    new FloatJsonConverter()
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

            var sessionInfo = new StoredSessionInfo
            {
                Id = session.Id.Value,
                StartTime = session.StartTime,
                Config = session.Config,
                Statistics = session.Statistics
            };

            await SaveJsonAsync(Path.Combine(sessionDir, "session.json"), sessionInfo);

            for (var i = 0; i < session.Shots.Count; i++)
                await SaveShotAsync(Path.Combine(shotsDir, $"{i}.json"), session.Shots[i]);
        }

        public async Task<TrainingSession> LoadSessionAsync(SessionId sessionId)
        {
            var sessionDir = GetSessionDirectory(sessionId);
            var sessionPath = Path.Combine(sessionDir, "session.json");
            var shotsDir = Path.Combine(sessionDir, "shots");

            if (!File.Exists(sessionPath))
                throw new FileNotFoundException($"Session {sessionId.Value} not found");

            var sessionInfo = await LoadJsonAsync<StoredSessionInfo>(sessionPath);

            var shots = new List<TrainingShot>();
            var shotFiles = Directory.GetFiles(shotsDir, "*.json")
                .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                .ToArray();

            foreach (var shotFile in shotFiles)
            {
                var shot = await LoadShotAsync(shotFile);
                shots.Add(shot);
            }

            var session = new TrainingSession(
                sessionId,
                sessionInfo.StartTime,
                sessionInfo.Config,
                shots
            );

            if (sessionInfo.Statistics != null)
                session.Complete(sessionInfo.Statistics);

            return session;
        }

        private void SanitizeMotionData(MotionRecordData data)
        {
            if (float.IsInfinity(data.Velocity.x) || float.IsNaN(data.Velocity.x))
                data.Velocity = Vector3.zero;
            if (float.IsInfinity(data.AngularVelocity.x) || float.IsNaN(data.AngularVelocity.x))
                data.AngularVelocity = Vector3.zero;
        }

        private async Task SaveShotAsync(string path, TrainingShot shot)
        {
            // サニタイズ
            foreach (var motionData in shot.BallMotionData)
                SanitizeMotionData(motionData);
            foreach (var motionData in shot.RacketMotionData)
                SanitizeMotionData(motionData);

            var storedShot = new StoredShot
            {
                Parameters = shot.Parameters,
                ExecutedAt = shot.ExecutedAt,
                WasSuccessful = shot.WasSuccessful,
                // 型を MotionRecordData から BallMotionRecordData に変更
                BallMotionData = shot.BallMotionData,
                RacketMotionData = shot.RacketMotionData,
                HeadMotionData = shot.HeadMotionData,
                GazeData = shot.GazeData,
                // 衝突データを追加
                CollisionData = shot.CollisionData,
                // サッカード時のボール非表示関連データを追加
                ShouldHideBallDuringSaccade = shot.ShouldHideBallDuringSaccade,
                WasBallHiddenDuringSaccade = shot.WasBallHiddenDuringSaccade
            };

            await SaveJsonAsync(path, storedShot);
        }

        private async Task<TrainingShot> LoadShotAsync(string path)
        {
            var storedShot = await LoadJsonAsync<StoredShot>(path);

            var shot = new TrainingShot(storedShot.Parameters);

            if (storedShot.ExecutedAt.HasValue)
                shot.RecordExecution(storedShot.ExecutedAt.Value, storedShot.WasSuccessful ?? false);

            shot.BallMotionData.AddRange(storedShot.BallMotionData);
            shot.RacketMotionData.AddRange(storedShot.RacketMotionData);
            shot.HeadMotionData.AddRange(storedShot.HeadMotionData);
            shot.GazeData.AddRange(storedShot.GazeData);
            
            // 衝突データのロード
            if (storedShot.CollisionData != null)
            {
                shot.CollisionData.AddRange(storedShot.CollisionData);
            }
            
            // サッカード時のボール非表示関連データのロード
            shot.ShouldHideBallDuringSaccade = storedShot.ShouldHideBallDuringSaccade;
            shot.WasBallHiddenDuringSaccade = storedShot.WasBallHiddenDuringSaccade;

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
            // BallMotionData の型変更に合わせる
            public List<StepUpTableTennis.DataManagement.Core.Models.BallMotionRecordData> BallMotionData { get; set; }
            public List<MotionRecordData> RacketMotionData { get; set; }
            public List<MotionRecordData> HeadMotionData { get; set; }
            public List<GazeRecordData> GazeData { get; set; }
            // 衝突データを追加
            public List<CollisionRecordData> CollisionData { get; set; }
            // サッカード時のボール非表示関連データを追加
            public bool ShouldHideBallDuringSaccade { get; set; }
            public bool WasBallHiddenDuringSaccade { get; set; }
        }
    }

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

    public class FloatJsonConverter : JsonConverter<float>
    {
        public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var value = reader.GetString();
                if (value == "Infinity") return float.PositiveInfinity;
                if (value == "-Infinity") return float.NegativeInfinity;
                if (value == "NaN") return float.NaN;
            }

            return reader.GetSingle();
        }

        public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
        {
            if (float.IsInfinity(value) || float.IsNaN(value))
                writer.WriteNumberValue(0f);
            else
                writer.WriteNumberValue(value);
        }
    }
}