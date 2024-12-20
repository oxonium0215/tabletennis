using UnityEngine;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Collisions.System;

namespace StepUpTableTennis.TableTennisEngine.Components
{
    public class BoxColliderComponent : MonoBehaviour
    {
        [Header("Collider Settings")]
        [SerializeField] private Vector3 size = Vector3.one;
        
        [Header("Physics Material")]
        [SerializeField] private float restitution = 0.8f;
        [SerializeField] private float friction = 0.2f;

        private PhysicsSettings settings;
        
        public Vector3 Size
        {
            get => size;
            set => size = value;
        }

        public void Initialize(PhysicsSettings settings)
        {
            this.settings = settings;
        }

        public bool CheckCollision(Ball ball, out CollisionInfo info)
        {
            info = new CollisionInfo(ball, null); // 仮のnull渡し、CollisionInfoの修正が必要

            var worldToLocal = transform.worldToLocalMatrix;
            var localBallPos = worldToLocal.MultiplyPoint3x4(ball.Position);

            var halfSize = size * 0.5f;
            var closestPoint = new Vector3(
                Mathf.Clamp(localBallPos.x, -halfSize.x, halfSize.x),
                Mathf.Clamp(localBallPos.y, -halfSize.y, halfSize.y),
                Mathf.Clamp(localBallPos.z, -halfSize.z, halfSize.z)
            );

            var distance = Vector3.Distance(localBallPos, closestPoint);
            if (distance <= settings.BallRadius)
            {
                var worldClosestPoint = transform.localToWorldMatrix.MultiplyPoint3x4(closestPoint);
                
                info.Point = worldClosestPoint;
                info.Normal = (ball.Position - worldClosestPoint).normalized;
                info.Depth = settings.BallRadius - distance;
                info.RelativeVelocity = ball.Velocity;
                info.Type = CollisionInfo.CollisionType.BallBox;
                
                return true;
            }

            return false;
        }

        public void ResolveCollision(CollisionInfo info)
        {
            if (info.Ball == null) return;

            // 位置の修正
            info.Ball.Position += info.Normal * info.Depth;

            var ball = info.Ball;
            var mass = settings.BallMass;
            var radius = settings.BallRadius;
            var inertia = 2f / 5f * mass * radius * radius;

            var normal = info.Normal;
            var velocity = ball.Velocity;
            var spin = ball.Spin;

            // 接触点での相対速度を求める
            var r = -radius * normal; // ボール中心から接触点までのベクトル
            var contactVelocity = velocity + Vector3.Cross(spin, r);

            // 法線方向と接線方向の成分を分解
            var normalVelocity = Vector3.Dot(contactVelocity, normal);
            var tangentialVelocity = contactVelocity - normalVelocity * normal;

            if (normalVelocity >= 0f) return;  // 離れる方向の場合は計算不要

            // 法線方向の力積
            var normalImpulseMagnitude = -(1 + restitution) * normalVelocity * mass;
            ball.AddForce(normalImpulseMagnitude * normal, Objects.ForceMode.Impulse);

            // 摩擦力積の計算と適用
            var maxFrictionImpulse = friction * Mathf.Abs(normalImpulseMagnitude);
            var desiredFrictionImpulse = -mass * tangentialVelocity;
            var frictionImpulse = desiredFrictionImpulse.magnitude > maxFrictionImpulse
                ? desiredFrictionImpulse.normalized * maxFrictionImpulse
                : desiredFrictionImpulse;

            ball.AddForce(frictionImpulse, Objects.ForceMode.Impulse);

            // 角運動量の変化(トルク)適用
            var deltaAngularMomentum = Vector3.Cross(r, frictionImpulse);
            ball.AddTorque(deltaAngularMomentum, Objects.ForceMode.Impulse);
        }

        private void OnDrawGizmosSelected()
        {
            // ギズモの描画
            Gizmos.color = Color.green;
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.matrix = rotationMatrix;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}