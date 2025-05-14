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

    // 物体の動きに関する記録（ラケットやヘッド用）
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

    // ボール専用の動きの記録データ
    public class BallMotionRecordData : MotionRecordData
    {
        public BallMotionRecordData(
            DateTime timestamp,
            float timeOffset,
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity,
            Vector3 angularVelocity,
            bool isBallVisible) : base(timestamp, timeOffset, position, rotation, velocity, angularVelocity)
        {
            IsBallVisible = isBallVisible;
        }

        public bool IsBallVisible { get; set; }
    }

    // 視線データの記録構造を修正
    public class GazeRecordData : RecordData
    {
        public GazeRecordData(
            DateTime timestamp,
            float timeOffset,
            Vector3 leftEyeDirection,
            Vector3 rightEyeDirection,
            Vector3 leftEyePosition,
            Vector3 rightEyePosition,
            float leftEyeClosedAmount,
            float rightEyeClosedAmount,
            bool isSaccade,
            float angularVelocity,
            float angularAcceleration) : base(timestamp, timeOffset)
        {
            LeftEyeDirection = leftEyeDirection;
            RightEyeDirection = rightEyeDirection;
            LeftEyePosition = leftEyePosition;
            RightEyePosition = rightEyePosition;
            LeftEyeClosedAmount = leftEyeClosedAmount;
            RightEyeClosedAmount = rightEyeClosedAmount;
            IsSaccade = isSaccade;
            AngularVelocity = angularVelocity;
            AngularAcceleration = angularAcceleration;
        }

        public Vector3 LeftEyeDirection { get; } // 左目の視線方向
        public Vector3 RightEyeDirection { get; } // 右目の視線方向
        public Vector3 LeftEyePosition { get; } // 左目の位置
        public Vector3 RightEyePosition { get; } // 右目の位置
        public float LeftEyeClosedAmount { get; } // 左目の閉じている度合い (0: 完全に開いている, 1: 完全に閉じている)
        public float RightEyeClosedAmount { get; } // 右目の閉じている度合い (0: 完全に開いている, 1: 完全に閉じている)
        public bool IsSaccade { get; } // サッカード状態かどうか
        public float AngularVelocity { get; } // 視線の角速度 (度/秒)
        public float AngularAcceleration { get; } // 視線の角加速度 (度/秒²)
    }

    // ラケット衝突情報の記録データ
    public class CollisionRecordData : RecordData
    {
        public CollisionRecordData(
            DateTime timestamp,
            float timeOffset,
            Vector3 position,
            float impactForce,
            Vector3 normal,
            Vector2 normalizedPaddlePosition) : base(timestamp, timeOffset)
        {
            Position = position;
            ImpactForce = impactForce;
            Normal = normal;
            NormalizedPaddlePosition = normalizedPaddlePosition;
        }

        public Vector3 Position { get; } // 衝突位置（ワールド座標）
        public float ImpactForce { get; } // 衝突の強さ
        public Vector3 Normal { get; } // 衝突法線
        public Vector2 NormalizedPaddlePosition { get; } // パドル上の正規化された位置 (-1〜1の範囲、中心が(0,0))
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
        // ボールの記録は BallMotionRecordData に限定する
        public List<BallMotionRecordData> BallMotionData { get; } = new();
        public List<MotionRecordData> RacketMotionData { get; } = new();
        public List<MotionRecordData> HeadMotionData { get; } = new();
        public List<GazeRecordData> GazeData { get; } = new();
        // 衝突記録を保存するリスト
        public List<CollisionRecordData> CollisionData { get; } = new();

        // サッカード時のボール非表示に関する情報
        public bool ShouldHideBallDuringSaccade { get; set; } = false; // ボールを隠すかの事前決定
        public bool WasBallHiddenDuringSaccade { get; set; } = false;  // 実際に隠されたかどうか
        
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
                case BallMotionRecordData ballMotionData:
                    BallMotionData.Add(ballMotionData);
                    break;
                case MotionRecordData motionData:
                    // ラケットやヘッドの記録は個別に追加されるためここでは何もしない
                    break;
                case GazeRecordData gazeData:
                    GazeData.Add(gazeData);
                    break;
                case CollisionRecordData collisionData:
                    CollisionData.Add(collisionData);
                    break;
            }
        }

        // 衝突記録を追加するための特定のメソッド（パドル上の正規化された位置も記録）
        public void AddCollisionRecord(Vector3 position, float impactForce, Vector3 normal, 
            Vector2 normalizedPaddlePosition, DateTime timestamp, float timeOffset)
        {
            var collisionData = new CollisionRecordData(
                timestamp,
                timeOffset,
                position,
                impactForce,
                normal,
                normalizedPaddlePosition
            );
            CollisionData.Add(collisionData);
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