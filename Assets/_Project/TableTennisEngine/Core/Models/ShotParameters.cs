using System;
using System.Linq;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Core.Models
{
    public class ShotParameters
    {
        public ShotParameters(
            Vector3 launchPosition,
            Vector3 aimPosition,
            float initialSpeed,
            float spinRate,
            Vector3 spinAxis)
        {
            if (initialSpeed <= 0)
                throw new ArgumentException("Initial speed must be positive", nameof(initialSpeed));

            LaunchPosition = launchPosition;
            AimPosition = aimPosition;
            InitialSpeed = initialSpeed;
            SpinRate = spinRate;
            SpinAxis = spinRate != 0 ? spinAxis.normalized : Vector3.up;
        }

        // 基本パラメータ（入力値）
        public Vector3 LaunchPosition { get; } // 発射位置
        public Vector3 AimPosition { get; } // 目標位置
        public float InitialSpeed { get; } // 初速 (m/s)
        public float SpinRate { get; } // 回転速度 (rps)
        public Vector3 SpinAxis { get; } // 回転軸 (normalized)

        // 計算結果
        public bool IsCalculated => InitialVelocity.HasValue;
        public Vector3? InitialVelocity { get; private set; } // 初速ベクトル
        public Vector3? InitialAngularVelocity { get; private set; } // 初角速度ベクトル
        public float? TargetError { get; private set; } // 目標位置との誤差
        public Vector3[]? PredictedPath { get; private set; } // 予測軌道

        // 計算結果を設定
        internal void SetCalculationResults(
            Vector3 initialVelocity,
            Vector3 initialAngularVelocity,
            float targetError,
            Vector3[] predictedPath)
        {
            InitialVelocity = initialVelocity;
            InitialAngularVelocity = initialAngularVelocity;
            TargetError = targetError;
            PredictedPath = predictedPath;
        }

        // パラメータのクローンを作成
        public ShotParameters Clone()
        {
            var clone = new ShotParameters(
                LaunchPosition,
                AimPosition,
                InitialSpeed,
                SpinRate,
                SpinAxis);

            if (IsCalculated)
                clone.SetCalculationResults(
                    InitialVelocity.Value,
                    InitialAngularVelocity.Value,
                    TargetError.Value,
                    PredictedPath.ToArray());
            return clone;
        }

        // 文字列表現（デバッグ用）
        public override string ToString()
        {
            return $"Shot[Launch={LaunchPosition:F2}, Aim={AimPosition:F2}, " +
                   $"Speed={InitialSpeed:F2}m/s, Spin={SpinRate:F2}rps, " +
                   $"Calculated={IsCalculated}]";
        }
    }
}