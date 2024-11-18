using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using StepUpTableTennis.DataManagement.Core.Models.Common;
using StepUpTableTennis.DataManagement.Core.Models.Shot;

namespace StepUpTableTennis.DataManagement.Core.Models.TimeSeries
{
    [Serializable]
    public sealed class TimeSeriesDataPoint
    {
        public TimeSeriesDataPoint(
            long timestampMs,
            MotionData.TimeSeriesDataType dataType,
            Vector3Data position,
            QuaternionData rotation,
            Vector3Data velocity,
            Vector3Data angularVelocity,
            Vector3Data? spinAxis = null,
            float? spinSpeed = null,
            string? customDataType = null,
            string? customData = null)
        {
            TimestampMs = timestampMs;
            DataType = dataType;
            Position = position ?? throw new ArgumentNullException(nameof(position));
            Rotation = rotation ?? throw new ArgumentNullException(nameof(rotation));
            Velocity = velocity ?? throw new ArgumentNullException(nameof(velocity));
            AngularVelocity = angularVelocity ?? throw new ArgumentNullException(nameof(angularVelocity));

            // ボール特有のデータ
            if (dataType == MotionData.TimeSeriesDataType.Ball)
            {
                SpinAxis = spinAxis ?? throw new ArgumentNullException(nameof(spinAxis));
                SpinSpeed = spinSpeed ?? throw new ArgumentNullException(nameof(spinSpeed));
            }
            else
            {
                SpinAxis = spinAxis;
                SpinSpeed = spinSpeed;
            }

            // カスタムデータ
            if (dataType == MotionData.TimeSeriesDataType.Custom)
            {
                CustomDataType = customDataType ?? throw new ArgumentNullException(nameof(customDataType));
                CustomData = customData;
            }
        }

        [JsonProperty] public long TimestampMs { get; private set; } // ショット開始からの経過時間（ミリ秒）
        [JsonProperty] public MotionData.TimeSeriesDataType DataType { get; private set; }
        [JsonProperty] public Vector3Data Position { get; private set; }
        [JsonProperty] public QuaternionData Rotation { get; private set; }
        [JsonProperty] public Vector3Data Velocity { get; private set; }
        [JsonProperty] public Vector3Data AngularVelocity { get; private set; }

        // ボール特有のデータ
        [JsonProperty] public Vector3Data? SpinAxis { get; private set; }
        [JsonProperty] public float? SpinSpeed { get; private set; }

        // カスタムデータ用のプロパティ
        [JsonProperty] public string? CustomDataType { get; private set; }
        [JsonProperty] public string? CustomData { get; private set; }

        public void Validate()
        {
            if (TimestampMs < 0)
                throw new ArgumentException("Timestamp cannot be negative");

            if (DataType == MotionData.TimeSeriesDataType.Ball)
                if (SpinAxis == null || SpinSpeed == null)
                    throw new ArgumentException("Ball data must include spin information");

            if (DataType == MotionData.TimeSeriesDataType.Custom && string.IsNullOrEmpty(CustomDataType))
                throw new ArgumentException("Custom data must include custom data type");
        }
    }

    [Serializable]
    public sealed class MotionData
    {
        [Serializable]
        public enum TimeSeriesDataType
        {
            Ball, // ボールの動き
            Racket, // ラケットの動き
            Head, // 頭（VRヘッドセット）の動き
            LeftHand, // 左手の動き
            RightHand, // 右手の動き
            Custom // カスタム用途
        }

        public MotionData(
            ShotId shotId,
            TimeSeriesDataType type,
            IReadOnlyList<TimeSeriesDataPoint> dataPoints)
        {
            ShotId = shotId ?? throw new ArgumentNullException(nameof(shotId));
            Type = type;
            DataPoints = dataPoints ?? throw new ArgumentNullException(nameof(dataPoints));
            Validate();
        }

        [JsonProperty] public ShotId ShotId { get; private set; }
        [JsonProperty] public TimeSeriesDataType Type { get; private set; }
        [JsonProperty] public IReadOnlyList<TimeSeriesDataPoint> DataPoints { get; private set; }

        public void Validate()
        {
            if (DataPoints.Count == 0)
                throw new ArgumentException("Motion data must contain at least one data point");

            foreach (var point in DataPoints) point.Validate();

            // 時系列の単調増加チェック
            for (var i = 1; i < DataPoints.Count; i++)
                if (DataPoints[i].TimestampMs <= DataPoints[i - 1].TimestampMs)
                    throw new ArgumentException("Timestamps must be strictly increasing");
        }

        public MotionData GetSubset(long startMs, long endMs)
        {
            if (startMs > endMs)
                throw new ArgumentException("Start time must be less than or equal to end time");

            var subset = new List<TimeSeriesDataPoint>();
            foreach (var point in DataPoints)
                if (point.TimestampMs >= startMs && point.TimestampMs <= endMs)
                    subset.Add(point);

            return new MotionData(ShotId, Type, subset);
        }

        public TimeSeriesDataPoint? GetDataPointAtTime(long timestampMs)
        {
            // 二分探索で最も近い時刻のデータポイントを探す
            var left = 0;
            var right = DataPoints.Count - 1;

            while (left <= right)
            {
                var mid = (left + right) / 2;
                var midPoint = DataPoints[mid];

                if (midPoint.TimestampMs == timestampMs)
                    return midPoint;

                if (midPoint.TimestampMs < timestampMs)
                    left = mid + 1;
                else
                    right = mid - 1;
            }

            // 最も近い時刻のポイントを返す（補間することも可能）
            if (right >= 0 && left < DataPoints.Count)
            {
                var beforePoint = DataPoints[right];
                var afterPoint = DataPoints[left];
                return Math.Abs(timestampMs - beforePoint.TimestampMs) <
                       Math.Abs(timestampMs - afterPoint.TimestampMs)
                    ? beforePoint
                    : afterPoint;
            }

            return null;
        }
    }
}