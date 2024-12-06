using System;
using System.Collections.Generic;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using UnityEngine;

namespace StepUpTableTennis.DataManagement.Core.Models
{
    public readonly struct SessionId
    {
        public string Value { get; }

        public SessionId(string value)
        {
            Value = value;
        }

        public static SessionId Generate()
        {
            return new SessionId(Guid.NewGuid().ToString());
        }
    }

    public class SessionDifficulty
    {
        public SessionDifficulty(int speedLevel, int spinLevel, int courseLevel)
        {
            SpeedLevel = speedLevel;
            SpinLevel = spinLevel;
            CourseLevel = courseLevel;
        }

        public int SpeedLevel { get; } // 1-5: ボール速度の難易度
        public int SpinLevel { get; } // 1-5: ボール回転の難易度
        public int CourseLevel { get; } // 1-5: コース配置の難易度
    }

    public class SessionConfig
    {
        public SessionConfig(
            SessionDifficulty difficulty,
            int totalShots,
            float shotInterval)
        {
            Difficulty = difficulty;
            TotalShots = totalShots;
            ShotInterval = shotInterval;
        }

        public SessionDifficulty Difficulty { get; }
        public int TotalShots { get; } // セッション内の総ショット数
        public float ShotInterval { get; } // ショット間隔（秒）
    }

    // 記録データの基本構造
    public abstract class RecordData
    {
        protected RecordData(DateTime timestamp, float timeOffset)
        {
            Timestamp = timestamp;
            TimeOffset = timeOffset;
        }

        public DateTime Timestamp { get; }
        public float TimeOffset { get; } // セッション開始からの経過時間
    }

    // 物体の動きに関する記録
    public class MotionRecordData : RecordData
    {
        public MotionRecordData(
            DateTime timestamp,
            float timeOffset,
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity,
            Vector3 angularVelocity) : base(timestamp, timeOffset)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
        }

        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 Velocity { get; set; }
        public Vector3 AngularVelocity { get; set; }
    }

    public class GazeRecordData : RecordData
    {
        public GazeRecordData(
            DateTime timestamp,
            float timeOffset,
            Vector3 direction,
            float leftEyeOpenness,
            float rightEyeOpenness,
            Vector2 leftPupilPosition,
            Vector2 rightPupilPosition) : base(timestamp, timeOffset)
        {
            Direction = direction;
            LeftEyeOpenness = leftEyeOpenness;
            RightEyeOpenness = rightEyeOpenness;
            LeftPupilPosition = leftPupilPosition;
            RightPupilPosition = rightPupilPosition;
        }

        public Vector3 Direction { get; }
        public float LeftEyeOpenness { get; }
        public float RightEyeOpenness { get; }
        public Vector2 LeftPupilPosition { get; }
        public Vector2 RightPupilPosition { get; }
    }

    public class TrainingShot
    {
        public TrainingShot(ShotParameters parameters)
        {
            Parameters = parameters;
        }

        public ShotParameters Parameters { get; }
        public DateTime? ExecutedAt { get; private set; }
        public bool? WasSuccessful { get; set; }
        public List<MotionRecordData> BallMotionData { get; } = new();
        public List<MotionRecordData> RacketMotionData { get; } = new();
        public List<MotionRecordData> HeadMotionData { get; } = new();
        public List<GazeRecordData> GazeData { get; } = new();
        public bool IsExecuted => ExecutedAt.HasValue;

        public void RecordExecution(DateTime executionTime, bool wasSuccessful)
        {
            if (IsExecuted)
                throw new InvalidOperationException("Shot has already been executed");

            ExecutedAt = executionTime;
            WasSuccessful = wasSuccessful;
        }

        public void AddRecordData(RecordData data)
        {
            switch (data)
            {
                case MotionRecordData motionData:
                    // ここでは例として、データの種類を識別する方法は
                    // 呼び出し側で管理することを想定しています
                    // より良い方法があれば変更可能です
                    break;
                case GazeRecordData gazeData:
                    GazeData.Add(gazeData);
                    break;
                // 他のデータ型にも対応可能
            }
        }
    }

    public class SessionStatistics
    {
        public SessionStatistics(
            int executedShots,
            int successfulShots,
            DateTime completedAt)
        {
            ExecutedShots = executedShots;
            SuccessfulShots = successfulShots;
            CompletedAt = completedAt;
        }

        public int ExecutedShots { get; }
        public int SuccessfulShots { get; }
        public DateTime CompletedAt { get; }

        public float SuccessRate => ExecutedShots > 0
            ? (float)SuccessfulShots / ExecutedShots
            : 0f;
    }

    public class TrainingSession
    {
        public TrainingSession(
            SessionId id,
            DateTime startTime,
            SessionConfig config,
            IReadOnlyList<TrainingShot> shots)
        {
            Id = id;
            StartTime = startTime;
            Config = config;
            Shots = shots;
        }

        public SessionId Id { get; }
        public DateTime StartTime { get; }
        public SessionConfig Config { get; }
        public IReadOnlyList<TrainingShot> Shots { get; }
        public SessionStatistics Statistics { get; private set; }
        public bool IsCompleted => Statistics != null;

        public void Complete(SessionStatistics statistics)
        {
            if (IsCompleted)
                throw new InvalidOperationException("Session is already completed");

            Statistics = statistics;
        }
    }
}