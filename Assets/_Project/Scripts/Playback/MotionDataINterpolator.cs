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

            // 時間範囲内のデータを探す
            var startIndex = -1;
            for (var i = 0; i < data.Count - 1; i++)
                if (data[i].TimeOffset <= targetTime && targetTime <= data[i + 1].TimeOffset)
                {
                    startIndex = i;
                    break;
                }

            // 範囲外の場合は最も近い値を返す
            if (startIndex == -1) return targetTime <= data[0].TimeOffset ? data[0] : data[^1];

            var a = data[startIndex];
            var b = data[startIndex + 1];
            var t = Mathf.InverseLerp(a.TimeOffset, b.TimeOffset, targetTime);

            return new MotionRecordData(
                DateTime.Now, // 補間時は現在時刻を使用
                targetTime,
                Vector3.Lerp(a.Position, b.Position, t),
                Quaternion.Slerp(a.Rotation, b.Rotation, t),
                Vector3.Lerp(a.Velocity, b.Velocity, t),
                Vector3.Lerp(a.AngularVelocity, b.AngularVelocity, t)
            );
        }
    }
}