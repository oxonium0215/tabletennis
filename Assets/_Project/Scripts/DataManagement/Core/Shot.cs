using System;
using Newtonsoft.Json;
using StepUpTableTennis.DataManagement.Core.Models.Common;
using StepUpTableTennis.DataManagement.Core.Models.Session;

namespace StepUpTableTennis.DataManagement.Core.Models.Shot
{
    [Serializable]
    public sealed class ShotId
    {
        [JsonConstructor]
        private ShotId()
        {
        }

        public ShotId(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Shot ID cannot be null or empty");
            Value = value;
        }

        [JsonProperty] public string Value { get; private set; }

        public static ShotId Generate()
        {
            return new ShotId(Guid.NewGuid().ToString());
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ShotId other && Value == other.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }
    }

    [Serializable]
    public sealed class BallParameters
    {
        [JsonConstructor]
        private BallParameters()
        {
        }

        public BallParameters(
            float speed,
            Vector3Data direction,
            float spinSpeed,
            Vector3Data spinAxis)
        {
            Speed = speed;
            Direction = direction ?? throw new ArgumentNullException(nameof(direction));
            SpinSpeed = spinSpeed;
            SpinAxis = spinAxis ?? throw new ArgumentNullException(nameof(spinAxis));
            Validate();
        }

        [JsonProperty] public float Speed { get; private set; }
        [JsonProperty] public Vector3Data Direction { get; private set; }
        [JsonProperty] public float SpinSpeed { get; private set; }
        [JsonProperty] public Vector3Data SpinAxis { get; private set; }

        public void Validate()
        {
            if (Speed <= 0)
                throw new ArgumentException("Speed must be positive");
            if (Math.Abs(Direction.ToUnityVector3().magnitude - 1f) > 0.001f)
                throw new ArgumentException("Direction must be a unit vector");
            if (Math.Abs(SpinAxis.ToUnityVector3().magnitude - 1f) > 0.001f)
                throw new ArgumentException("Spin axis must be a unit vector");
        }
    }

    [Serializable]
    public sealed class PlannedShotData
    {
        [JsonConstructor]
        private PlannedShotData()
        {
        }

        public PlannedShotData(
            ShotId id,
            SessionId sessionId,
            DateTime plannedTimestamp,
            BallParameters ballParams,
            Vector3Data targetPosition)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
            PlannedTimestamp = plannedTimestamp;
            BallParams = ballParams ?? throw new ArgumentNullException(nameof(ballParams));
            TargetPosition = targetPosition ?? throw new ArgumentNullException(nameof(targetPosition));
            Validate();
        }

        [JsonProperty] public ShotId Id { get; private set; }
        [JsonProperty] public SessionId SessionId { get; private set; }
        [JsonProperty] public DateTime PlannedTimestamp { get; private set; }
        [JsonProperty] public BallParameters BallParams { get; private set; }
        [JsonProperty] public Vector3Data TargetPosition { get; private set; }

        public void Validate()
        {
            BallParams.Validate();
            // 将来的な拡張のために、ターゲット位置の妥当性チェックなども追加可能
        }
    }

    [Serializable]
    public sealed class ShotResult
    {
        [JsonConstructor]
        private ShotResult()
        {
        }

        public ShotResult(
            ShotId shotId,
            DateTime timestamp,
            bool success,
            Vector3Data? hitPosition,
            Vector3Data? hitVelocity,
            float responseTime)
        {
            ShotId = shotId ?? throw new ArgumentNullException(nameof(shotId));
            Timestamp = timestamp;
            Success = success;
            HitPosition = hitPosition;
            HitVelocity = hitVelocity;
            ResponseTime = responseTime;
            Validate();
        }

        [JsonProperty] public ShotId ShotId { get; private set; }
        [JsonProperty] public DateTime Timestamp { get; private set; }
        [JsonProperty] public bool Success { get; private set; }
        [JsonProperty] public Vector3Data? HitPosition { get; private set; }
        [JsonProperty] public Vector3Data? HitVelocity { get; private set; }
        [JsonProperty] public float ResponseTime { get; private set; }

        public void Validate()
        {
            if (ResponseTime < 0)
                throw new ArgumentException("Response time cannot be negative");
            if (Success && (HitPosition == null || HitVelocity == null))
                throw new ArgumentException("Successful shot must have hit position and velocity");
        }
    }
}