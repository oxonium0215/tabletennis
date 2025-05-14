using StepUpTableTennis.TableTennisEngine.Collisions.System;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Objects.Base;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Collisions.Handlers
{
    public class PaddleCollisionHandler : ICollisionHandler
    {
        private PhysicsSettings settings;

        public bool DetectCollision(Ball ball, PhysicsObject target, PhysicsSettings settings, out CollisionInfo info)
        {
            info = new CollisionInfo(ball, target);
            this.settings = settings;

            var paddle = target as Paddle;
            if (paddle == null)
            {
                info = null;
                return false;
            }

            var localBallPos = TransformToPaddleSpace(ball.Position, paddle);

            if (IsInsideEllipticalVolume(localBallPos, settings.PaddleSize, settings.BallRadius, settings.PaddleThickness, out var closestLocalPoint))
            {
                var worldClosestPoint = paddle.Position + paddle.Rotation * closestLocalPoint;
                var paddleVelocityAtPoint = paddle.GetVelocityAtPoint(worldClosestPoint);

                // 法線ベクトルの決定
                var normalWorld = DetermineNormal(localBallPos, closestLocalPoint, paddle);

                // 衝突深さ計算
                float depth = settings.BallRadius - Vector3.Distance(worldClosestPoint, ball.Position);
                if (depth < 0)
                {
                    // 極めてまれなケース：計算誤差等で負になった場合は衝突なし扱い
                    info = null;
                    return false;
                }

                info.Point = worldClosestPoint;
                info.Normal = normalWorld;
                info.Depth = depth;
                info.Ball = ball;
                info.Target = paddle;
                info.RelativeVelocity = ball.Velocity - paddleVelocityAtPoint;
                return true;
            }

            return false;
        }

        public void ResolveCollision(CollisionInfo info, PhysicsSettings settings)
        {
            var (ball, paddle) = GetCollisionObjects(info);
            if (ball == null || paddle == null) return;

            ResolvePosition(ball, info);
            ResolveVelocity(ball, paddle, info, settings);
            ApplySpinEffect(ball, paddle, info, settings);
        }

        private Vector3 TransformToPaddleSpace(Vector3 worldPosition, Paddle paddle)
        {
            var toPaddle = worldPosition - paddle.Position;
            return Quaternion.Inverse(paddle.Rotation) * toPaddle;
        }

        private bool IsInsideEllipticalVolume(Vector3 localBallPos, Vector2 paddleSize, float ballRadius, float paddleThickness, out Vector3 closestPointOnSurface)
        {
            float halfW = paddleSize.x * 0.5f;
            float halfH = paddleSize.y * 0.5f;
            float normalizedX = localBallPos.x / halfW;
            float normalizedY = localBallPos.y / halfH;
            float ellipseVal = normalizedX * normalizedX + normalizedY * normalizedY;

            float halfThickness = paddleThickness * 0.5f;
            float maxZ = halfThickness + ballRadius;

            if (ellipseVal <= 1.0f && Mathf.Abs(localBallPos.z) <= maxZ)
            {
                // 最近点を求める
                float scale = 1.0f / Mathf.Sqrt(Mathf.Max(ellipseVal, 1e-8f));
                float clampedX = localBallPos.x * Mathf.Min(scale, 1f);
                float clampedY = localBallPos.y * Mathf.Min(scale, 1f);
                float clampedZ = Mathf.Clamp(localBallPos.z, -halfThickness, halfThickness);

                closestPointOnSurface = new Vector3(clampedX, clampedY, clampedZ);
                return true;
            }

            closestPointOnSurface = Vector3.zero;
            return false;
        }

        /// <summary>
        /// 法線ベクトルを決定する。
        /// パドルは楕円柱状を想定しているため、以下のロジックで決める。
        /// 1. 厚み方向(z軸)の端に当たっている場合(=front or back face)は、法線はz軸方向(前面: +z、背面: -z)。
        /// 2. 側面(楕円境界)に近い場合は、楕円勾配に基づいた法線を使用する。
        /// 
        /// closestLocalPointとの比較で、どの軸方向でクリップされたか判定する：
        /// - z方向が衝突決定要因：|closestLocalPoint.z|がhalfThickness付近ならZ法線を優先
        /// - それ以外（x,y方向で楕円境界に当たっている場合）：楕円ベースの法線を使用
        /// </summary>
        private Vector3 DetermineNormal(Vector3 localBallPos, Vector3 closestLocalPoint, Paddle paddle)
        {
            float halfThickness = settings.PaddleThickness * 0.5f;
            float halfW = settings.PaddleSize.x * 0.5f;
            float halfH = settings.PaddleSize.y * 0.5f;

            // 各軸でどれくらい"クリップ"されたかを見る
            float dx = Mathf.Abs(localBallPos.x - closestLocalPoint.x);
            float dy = Mathf.Abs(localBallPos.y - closestLocalPoint.y);
            float dz = Mathf.Abs(localBallPos.z - closestLocalPoint.z);

            // 最もクリップが大きい方向が衝突表面を決定している
            // front/back面に当たっている場合は、|closestLocalPoint.z|が半厚み近いことが多く、dzが支配的
            // 逆に楕円周縁での衝突ではdx,dyが支配的
            float maxClamp = Mathf.Max(dx, Mathf.Max(dy, dz));

            Vector3 normalLocal;

            if (Mathf.Abs(closestLocalPoint.z) >= halfThickness - 1e-5f && dz == maxClamp)
            {
                // 正面・背面ヒット
                // Z方向法線: front(+z)またはback(-z)
                normalLocal = new Vector3(0, 0, Mathf.Sign(closestLocalPoint.z));
            }
            else
            {
                // 楕円周縁ヒット
                // (x^2/a^2 + y^2/b^2 = 1)上での法線は (x/(a^2), y/(b^2), 0) で求まるが、
                // 簡易的に中心から衝突点へのベクトルを用いる。
                // ただしZ成分は0で押さえ、純粋な楕円面法線とする。
                float nx = closestLocalPoint.x / (halfW * halfW);
                float ny = closestLocalPoint.y / (halfH * halfH);

                var norm = new Vector3(nx, ny, 0f);
                if (norm == Vector3.zero)
                    norm = Vector3.up; // 万が一原点なら上向き

                normalLocal = norm.normalized;
            }

            return paddle.Rotation * normalLocal;
        }

        private (Ball ball, Paddle paddle) GetCollisionObjects(CollisionInfo info)
        {
            return (info.Ball, info.Target as Paddle);
        }

        private void ResolvePosition(Ball ball, CollisionInfo info)
        {
            ball.Position += info.Normal * info.Depth;
        }

        private void ResolveVelocity(Ball ball, Paddle paddle, CollisionInfo info, PhysicsSettings settings)
        {
            var paddleVelocityAtPoint = paddle.GetVelocityAtPoint(info.Point);
            var relativeVelocity = ball.Velocity - paddleVelocityAtPoint;
            var normalVelocity = Vector3.Dot(relativeVelocity, info.Normal);

            var tangentialVelocity = relativeVelocity - normalVelocity * info.Normal;

            // 法線方向反発
            var normalReflectedVelocity = -normalVelocity * settings.PaddleRestitution * info.Normal;

            // 摩擦減衰（接線方向）
            tangentialVelocity *= 1 - settings.PaddleFriction;

            ball.Velocity = paddleVelocityAtPoint + tangentialVelocity + normalReflectedVelocity;
        }

        private void ApplySpinEffect(Ball ball, Paddle paddle, CollisionInfo info, PhysicsSettings settings)
        {
            var paddleVelocityAtPoint = paddle.GetVelocityAtPoint(info.Point);
            var relativeVel = ball.Velocity - paddleVelocityAtPoint;

            // 法線成分を除いた接線成分
            var normalComponent = Vector3.Dot(relativeVel, info.Normal) * info.Normal;
            var tangentVel = relativeVel - normalComponent;

            float torqueMag = settings.BallMass * tangentVel.magnitude * settings.BallRadius * settings.PaddleFriction;
            if (torqueMag > 1e-6f)
            {
                var spinAxis = Vector3.Cross(info.Normal, tangentVel.normalized).normalized;
                var torque = spinAxis * torqueMag;
                ball.AddTorque(torque, StepUpTableTennis.TableTennisEngine.Objects.ForceMode.Impulse);
            }
        }
    }
}
