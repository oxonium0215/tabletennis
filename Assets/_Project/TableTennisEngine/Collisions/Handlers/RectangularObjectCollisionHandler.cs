using StepUpTableTennis.TableTennisEngine.Collisions.System;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Objects.Base;
using UnityEngine;
using ForceMode = StepUpTableTennis.TableTennisEngine.Objects.ForceMode;

namespace StepUpTableTennis.TableTennisEngine.Collisions.Handlers
{
    public class RectangularObjectCollisionHandler : ICollisionHandler
    {
        public bool DetectCollision(Ball ball, PhysicsObject other, PhysicsSettings settings, out CollisionInfo info)
        {
            info = new CollisionInfo(ball, other);

            // Table collision detection
            if (other is Table table)
            {
                var bounds = table.GetBounds();
                return DetectRectangularCollision(ball, bounds, settings, other, ref info);
            }

            // 将来的な拡張のためのプレースホルダー
            // if (other is Net net) { ... }
            // if (other is Floor floor) { ... }

            return false;
        }

        public void ResolveCollision(CollisionInfo info, PhysicsSettings settings)
        {
            var ball = info.Ball;
            if (ball == null) return;

            // 位置の修正: ResetStateではなくPositionを直接修正
            ball.Position += info.Normal * info.Depth;

            var mass = settings.BallMass;
            var radius = settings.BallRadius;
            var inertia = 2f / 5f * mass * radius * radius;

            // 反発係数と摩擦係数の取得
            var restitution = settings.TableRestitution;
            var frictionCoefficient = settings.TableFriction;

            var normal = info.Normal;
            var v = ball.Velocity;
            var w = ball.Spin;

            // 接触点での相対速度を求める
            var r = -radius * normal; // ボール中心から接触点までのベクトル
            var v_contact = v + Vector3.Cross(w, r);

            // 法線方向と接線方向の成分を分解
            var v_contact_n = Vector3.Dot(v_contact, normal);
            var v_contact_t = v_contact - v_contact_n * normal;

            // ボールが既に衝突面から離れる方向に動いている場合は反発インパルス不要
            if (v_contact_n >= 0f)
            {
                // 離れる方向なら特に反発は与えず終了
                return;
            }

            // 法線方向の力積
            var normalImpulseMagnitude = -(1 + restitution) * v_contact_n * mass;
            ball.AddForce(normalImpulseMagnitude * normal, ForceMode.Impulse);

            // 最大摩擦力積
            var maxFrictionImpulse = frictionCoefficient * Mathf.Abs(normalImpulseMagnitude);

            // 接線方向の目標摩擦力積
            var desiredFrictionImpulse = -mass * v_contact_t;
            var frictionImpulse = desiredFrictionImpulse.magnitude > maxFrictionImpulse
                ? desiredFrictionImpulse.normalized * maxFrictionImpulse
                : desiredFrictionImpulse;

            // 摩擦力積の適用
            ball.AddForce(frictionImpulse, ForceMode.Impulse);

            // 角運動量の変化(トルク)適用
            var deltaAngularMomentum = Vector3.Cross(r, frictionImpulse);
            ball.AddTorque(deltaAngularMomentum, ForceMode.Impulse);
        }

        private bool DetectRectangularCollision(Ball ball, Bounds bounds, PhysicsSettings settings,
            PhysicsObject other, ref CollisionInfo info)
        {
            var closestPoint = new Vector3(
                Mathf.Clamp(ball.Position.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(ball.Position.y, bounds.min.y, bounds.max.y),
                Mathf.Clamp(ball.Position.z, bounds.min.z, bounds.max.z)
            );

            var distance = Vector3.Distance(ball.Position, closestPoint);
            if (distance <= settings.BallRadius)
            {
                info.Point = closestPoint;
                info.Normal = (ball.Position - closestPoint).normalized;
                info.Depth = settings.BallRadius - distance;
                info.RelativeVelocity = ball.Velocity;
                info.Ball = ball;
                info.Target = other;
                return true;
            }

            return false;
        }
    }
}
