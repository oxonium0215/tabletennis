using System;
using System.Collections.Generic;
using StepUpTableTennis.DataManagement.Core.Models;
using UnityEngine;

namespace StepUpTableTennis.Playback
{
    public static class MotionDataInterpolator
    {
        public static MotionRecordData InterpolateMotionData(
            IReadOnlyList<MotionRecordData> data,
            float targetTime)
        {
            if (data == null || data.Count == 0)
                throw new ArgumentException("No motion data available");

            // 累積時間を計算
            var cumulativeTime = new float[data.Count];
            cumulativeTime[0] = data[0].TimeOffset;
            for (var i = 1; i < data.Count; i++) cumulativeTime[i] = cumulativeTime[i - 1] + data[i].TimeOffset;

            // 時間範囲内のデータを探す
            var startIndex = -1;
            for (var i = 0; i < data.Count - 1; i++)
                if (cumulativeTime[i] <= targetTime && targetTime <= cumulativeTime[i + 1])
                {
                    startIndex = i;
                    break;
                }

            // 範囲外の場合は最も近い値を返す
            if (startIndex == -1)
                return targetTime <= cumulativeTime[0] ? data[0] : data[^1];

            var a = data[startIndex];
            var b = data[startIndex + 1];
            var t = Mathf.InverseLerp(cumulativeTime[startIndex], cumulativeTime[startIndex + 1], targetTime);

            return new MotionRecordData(
                DateTime.Now,
                targetTime,
                Vector3.Lerp(a.Position, b.Position, t),
                Quaternion.Slerp(a.Rotation, b.Rotation, t),
                Vector3.Lerp(a.Velocity, b.Velocity, t),
                Vector3.Lerp(a.AngularVelocity, b.AngularVelocity, t)
            );
        }
    }
}