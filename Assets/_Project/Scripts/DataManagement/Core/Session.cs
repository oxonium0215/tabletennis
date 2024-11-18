using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using StepUpTableTennis.DataManagement.Core.Models.Shot;

namespace StepUpTableTennis.DataManagement.Core.Models.Session
{
    [Serializable]
    public sealed class SessionId
    {
        [JsonConstructor]
        private SessionId()
        {
        }

        public SessionId(string value)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("Session ID cannot be null or empty");
            Value = value;
        }

        [JsonProperty] public string Value { get; private set; }

        public static SessionId Generate()
        {
            return new SessionId(Guid.NewGuid().ToString());
        }

        public override string ToString()
        {
            return Value;
        }

        public override bool Equals(object obj)
        {
            return obj is SessionId other && Value == other.Value;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Value);
        }
    }

    [Serializable]
    public sealed class SessionConfig
    {
        [JsonConstructor]
        private SessionConfig()
        {
        }

        public SessionConfig(
            int speedLevel,
            int spinLevel,
            int courseLevel,
            int totalPlannedShots,
            float shotInterval)
        {
            SpeedLevel = speedLevel;
            SpinLevel = spinLevel;
            CourseLevel = courseLevel;
            TotalPlannedShots = totalPlannedShots;
            ShotInterval = shotInterval;
            Validate();
        }

        [JsonProperty] public int SpeedLevel { get; private set; }
        [JsonProperty] public int SpinLevel { get; private set; }
        [JsonProperty] public int CourseLevel { get; private set; }
        [JsonProperty] public int TotalPlannedShots { get; private set; }
        [JsonProperty] public float ShotInterval { get; private set; }

        public void Validate()
        {
            if (SpeedLevel is < 1 or > 5)
                throw new ArgumentException("Speed level must be between 1 and 5");
            if (SpinLevel is < 1 or > 5)
                throw new ArgumentException("Spin level must be between 1 and 5");
            if (CourseLevel is < 1 or > 5)
                throw new ArgumentException("Course level must be between 1 and 5");
            if (TotalPlannedShots <= 0)
                throw new ArgumentException("Total planned shots must be positive");
            if (ShotInterval <= 0)
                throw new ArgumentException("Shot interval must be positive");
        }
    }

    [Serializable]
    public sealed class SessionData
    {
        [JsonConstructor]
        private SessionData()
        {
        }

        public SessionData(
            SessionId id,
            DateTime timestamp,
            SessionConfig config,
            IReadOnlyList<PlannedShotData> plannedShots)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Timestamp = timestamp;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            PlannedShots = plannedShots ?? throw new ArgumentNullException(nameof(plannedShots));
            Validate();
        }

        [JsonProperty] public SessionId Id { get; private set; }
        [JsonProperty] public DateTime Timestamp { get; private set; }
        [JsonProperty] public SessionConfig Config { get; private set; }
        [JsonProperty] public IReadOnlyList<PlannedShotData> PlannedShots { get; private set; }
        [JsonProperty] public SessionResults Results { get; private set; }

        public void SetResults(SessionResults results)
        {
            Results = results ?? throw new ArgumentNullException(nameof(results));
        }

        public void Validate()
        {
            Config.Validate();
            if (PlannedShots.Count != Config.TotalPlannedShots)
                throw new ArgumentException("Planned shots count doesn't match configuration");
        }
    }

    [Serializable]
    public sealed class SessionResults
    {
        [JsonConstructor]
        private SessionResults()
        {
        }

        public SessionResults(
            int totalShots,
            int successfulShots,
            float averageResponseTime,
            DateTime completedAt)
        {
            if (totalShots < 0)
                throw new ArgumentException("Total shots cannot be negative");
            if (successfulShots < 0 || successfulShots > totalShots)
                throw new ArgumentException("Successful shots must be between 0 and total shots");
            if (averageResponseTime < 0)
                throw new ArgumentException("Average response time cannot be negative");

            TotalShots = totalShots;
            SuccessfulShots = successfulShots;
            AverageResponseTime = averageResponseTime;
            CompletedAt = completedAt;
        }

        [JsonProperty] public int TotalShots { get; private set; }
        [JsonProperty] public int SuccessfulShots { get; private set; }
        [JsonProperty] public float AverageResponseTime { get; private set; }
        [JsonProperty] public DateTime CompletedAt { get; private set; }
        public float SuccessRate => TotalShots > 0 ? (float)SuccessfulShots / TotalShots : 0f;
    }
}