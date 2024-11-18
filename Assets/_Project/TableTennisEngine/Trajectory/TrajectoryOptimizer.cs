using System;
using System.Collections.Generic;
using System.Linq;
using StepUpTableTennis.TableTennisEngine.Core;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Trajectory
{
    public class TrajectoryOptimizer
    {
        private const float DT = 0.001f; // シミュレーション時間刻み [s]
        private const float MAX_TIME = 5.0f; // 最大シミュレーション時間 [s]
        private const float MIN_VELOCITY = 1e-3f; // 最小速度閾値 [m/s]
        private readonly float ballArea; // ボールの断面積 [m^2]
        private readonly float ballMassInv; // 質量の逆数 [1/kg]
        private readonly Vector3 gravityForce; // 重力による力 [N]
        private readonly PhysicsSettings settings;

        public TrajectoryOptimizer(PhysicsSettings settings)
        {
            this.settings = settings;
            ballArea = Mathf.PI * settings.BallRadius * settings.BallRadius;
            ballMassInv = 1.0f / settings.BallMass;
            gravityForce = settings.Gravity * settings.BallMass;
        }

        public OptimizedShot CalculateOptimalShot(Vector3 startPos, Vector3 targetPos, float speed, Vector3 spin)
        {
            // 初期角度の推定
            var delta = targetPos - startPos;
            var horizontalDist = new Vector3(delta.x, 0, delta.z).magnitude;
            var theta = Mathf.Atan2(delta.z, delta.x);

            // 投射角度の初期推定
            float phi;
            try
            {
                var g = Mathf.Abs(settings.Gravity.y);
                var t = horizontalDist / speed;
                phi = Mathf.Asin((delta.y + 0.5f * g * t * t) / (speed * t));
            }
            catch
            {
                phi = 5f * Mathf.Deg2Rad; // フォールバック：5度
            }

            // Nelder-Mead法による最適化
            var initialGuess = new[] { theta, phi };
            var result = NelderMead(
                angles => SimulationError(angles, startPos, targetPos, speed, spin),
                initialGuess
            );

            // 最適な角度から速度ベクトルを計算
            var optimalVelocity = CalculateVelocityFromAngles(result.Parameters, speed);

            return new OptimizedShot
            {
                Velocity = optimalVelocity,
                Spin = spin,
                Error = result.Value
            };
        }

        private Vector3 CalculateVelocityFromAngles(float[] angles, float speed)
        {
            float theta = angles[0], phi = angles[1];
            var cosPhi = Mathf.Cos(phi);

            return new Vector3(
                cosPhi * Mathf.Cos(theta),
                Mathf.Sin(phi),
                cosPhi * Mathf.Sin(theta)
            ) * speed;
        }

        private float SimulationError(float[] angles, Vector3 startPos, Vector3 targetPos, float speed, Vector3 spin)
        {
            var velocity = CalculateVelocityFromAngles(angles, speed);
            var finalPos = SimulateTrajectory(startPos, velocity, spin, targetPos.z);
            return Vector3.Distance(finalPos, targetPos);
        }

        private Vector3 SimulateTrajectory(Vector3 position, Vector3 velocity, Vector3 spin, float targetZ)
        {
            var timeElapsed = 0f;
            var currentPos = position;
            var currentVel = velocity;
            var currentSpin = spin;
            var prevPos = position;
            var crossedTarget = false;

            while (timeElapsed < MAX_TIME)
            {
                prevPos = currentPos;

                var vMag = currentVel.magnitude;
                if (vMag < MIN_VELOCITY) break;

                var vHat = currentVel / vMag;

                // 抗力の計算
                var dragForce = -0.5f * settings.BallDragCoefficient * ballArea *
                                settings.AirDensity * vMag * currentVel;

                // マグヌス力の計算
                var magnusForce = Vector3.zero;
                var spinMag = currentSpin.magnitude;
                if (spinMag > MIN_VELOCITY)
                {
                    var spinHat = currentSpin / spinMag;
                    var cl = settings.LiftCoefficientSlope *
                        (settings.BallRadius * spinMag) / vMag;
                    magnusForce = 0.5f * cl * ballArea * settings.AirDensity *
                                  vMag * vMag * Vector3.Cross(spinHat, vHat);
                }

                // 運動方程式の解法
                var totalForce = gravityForce + dragForce + magnusForce;
                var acceleration = totalForce * ballMassInv;

                currentVel += acceleration * DT;
                currentPos += currentVel * DT;
                currentSpin *= 1f - settings.SpinDampingCoefficient * DT;

                timeElapsed += DT;

                // 目標z平面との交差チェック
                if (!crossedTarget &&
                    Mathf.Sign(prevPos.z - targetZ) != Mathf.Sign(currentPos.z - targetZ))
                {
                    var t = Mathf.Abs((targetZ - prevPos.z) / (currentPos.z - prevPos.z));
                    return Vector3.Lerp(prevPos, currentPos, t);
                }

                if (currentPos.y < 0) break; // 地面に到達
            }

            return currentPos;
        }

        private OptimizationResult NelderMead(Func<float[], float> objective, float[] initial,
            float step = 0.1f, float tolerance = 1e-6f, int maxIter = 100)
        {
            var n = initial.Length;
            var simplexPoints = new List<(float[] point, float value)>();

            // 初期シンプレックスの構築
            simplexPoints.Add((initial, objective(initial)));
            for (var i = 0; i < n; i++)
            {
                var point = (float[])initial.Clone();
                point[i] = point[i] + step;
                simplexPoints.Add((point, objective(point)));
            }

            for (var iter = 0; iter < maxIter; iter++)
            {
                // ソートと終了判定
                simplexPoints = simplexPoints.OrderBy(x => x.value).ToList();
                if (simplexPoints[n].value - simplexPoints[0].value < tolerance)
                    break;

                // 重心の計算（最悪点を除く）
                var centroid = new float[n];
                for (var i = 0; i < n; i++)
                for (var j = 0; j < n; j++)
                    centroid[i] += simplexPoints[j].point[i];
                for (var i = 0; i < n; i++)
                    centroid[i] /= n;

                // 反射
                var reflected = Reflect(centroid, simplexPoints[n].point, 1.0f);
                var reflectedValue = objective(reflected);

                if (reflectedValue < simplexPoints[n - 1].value)
                {
                    if (reflectedValue >= simplexPoints[0].value)
                    {
                        simplexPoints[n] = (reflected, reflectedValue);
                        continue;
                    }

                    // 拡張
                    var expanded = Reflect(centroid, simplexPoints[n].point, 2.0f);
                    var expandedValue = objective(expanded);

                    simplexPoints[n] = expandedValue < reflectedValue
                        ? (expanded, expandedValue)
                        : (reflected, reflectedValue);
                    continue;
                }

                // 収縮
                var contracted = Reflect(centroid, simplexPoints[n].point, -0.5f);
                var contractedValue = objective(contracted);

                if (contractedValue < simplexPoints[n].value)
                {
                    simplexPoints[n] = (contracted, contractedValue);
                    continue;
                }

                // 全体の収縮
                for (var i = 1; i <= n; i++)
                {
                    var point = new float[n];
                    for (var j = 0; j < n; j++)
                        point[j] = (simplexPoints[0].point[j] + simplexPoints[i].point[j]) * 0.5f;
                    simplexPoints[i] = (point, objective(point));
                }
            }

            return new OptimizationResult
            {
                Parameters = simplexPoints[0].point,
                Value = simplexPoints[0].value
            };
        }

        public (Vector3 position, Vector3 velocity)[] GetTrajectoryPoints(
            Vector3 startPos, Vector3 initialVelocity, Vector3 initialSpin,
            float timeStep, int maxPoints)
        {
            var points = new List<(Vector3 position, Vector3 velocity)>();
            var timeElapsed = 0f;
            var currentPos = startPos;
            var currentVel = initialVelocity;
            var currentSpin = initialSpin;

            points.Add((currentPos, currentVel));

            for (var i = 1; i < maxPoints && timeElapsed < MAX_TIME; i++)
            {
                var vMag = currentVel.magnitude;
                if (vMag < MIN_VELOCITY) break;

                var vHat = currentVel / vMag;

                // 抗力の計算
                var dragForce = -0.5f * settings.BallDragCoefficient * ballArea *
                                settings.AirDensity * vMag * currentVel;

                // マグヌス力の計算
                var magnusForce = Vector3.zero;
                var spinMag = currentSpin.magnitude;
                if (spinMag > MIN_VELOCITY)
                {
                    var spinHat = currentSpin / spinMag;
                    var cl = settings.LiftCoefficientSlope *
                        (settings.BallRadius * spinMag) / vMag;
                    magnusForce = 0.5f * cl * ballArea * settings.AirDensity *
                                  vMag * vMag * Vector3.Cross(spinHat, vHat);
                }

                // 運動方程式の解法
                var totalForce = gravityForce + dragForce + magnusForce;
                var acceleration = totalForce * ballMassInv;

                currentVel += acceleration * timeStep;
                currentPos += currentVel * timeStep;
                currentSpin *= 1f - settings.SpinDampingCoefficient * timeStep;

                points.Add((currentPos, currentVel));
                timeElapsed += timeStep;

                // 地面到達で終了
                if (currentPos.y < 0) break;
            }

            return points.ToArray();
        }

        private float[] Reflect(float[] centroid, float[] point, float coefficient)
        {
            var result = new float[point.Length];
            for (var i = 0; i < point.Length; i++)
                result[i] = centroid[i] + coefficient * (centroid[i] - point[i]);
            return result;
        }

        public class OptimizedShot
        {
            public Vector3 Velocity { get; set; }
            public Vector3 Spin { get; set; }
            public float Error { get; set; }
        }

        private class OptimizationResult
        {
            public float[] Parameters { get; set; }
            public float Value { get; set; }
        }
    }
}