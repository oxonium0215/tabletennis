using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Objects.Base;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Collisions.System
{
    public class CollisionInfo
    {
        public enum CollisionType
        {
            BallTable,
            BallPaddle,
            BallNet, // 将来的な拡張用
            Unknown
        }

        public CollisionInfo(Ball ball, PhysicsObject target)
        {
            Ball = ball;
            Target = target;
            Type = DetermineCollisionType(target);
        }

        // 衝突の基本情報
        public Vector3 Point { get; set; } // 衝突点
        public Vector3 Normal { get; set; } // 衝突面の法線（ボールから見た方向）
        public float Depth { get; set; } // めり込み量
        public Vector3 RelativeVelocity { get; set; } // 相対速度

        // 衝突オブジェクト（より具体的な型で定義）
        public Ball Ball { get; set; }
        public PhysicsObject Target { get; set; }

        // 衝突の種類を示す列挙型
        public CollisionType Type { get; set; }

        // 衝突時のエネルギー関連の情報
        public float ImpactEnergy { get; }
        public float RestitutionCoefficient { get; }

        private CollisionType DetermineCollisionType(PhysicsObject target)
        {
            return target switch
            {
                Table => CollisionType.BallTable,
                Paddle => CollisionType.BallPaddle,
                _ => CollisionType.Unknown
            };
        }

        // TODO: 以下の情報も必要か検討
        // - 衝突時の角運動量
        // - 摩擦係数
        // - エネルギー損失

        // 衝突の強さを計算
        public float GetImpactForce(PhysicsSettings settings)
        {
            // 法線方向の相対速度を取得（衝突速度）
            var normalVelocity = Mathf.Abs(Vector3.Dot(RelativeVelocity, Normal));

            // 運動量変化から衝撃力を計算
            // F = m * Δv / Δt
            // ここでΔtは衝突時間の概算値として、めり込み量と速度から推定
            var collisionTime = Mathf.Max(Depth / normalVelocity, settings.TimeStep);
            var impactForce = settings.BallMass * normalVelocity / collisionTime;

            return impactForce;
        }

        public string GetDebugInfo()
        {
            return $"Collision Type: {Type}\n" +
                   $"Impact Point: {Point}\n" +
                   $"Normal: {Normal}\n" +
                   $"Relative Velocity: {RelativeVelocity}\n" +
                   $"Impact Energy: {ImpactEnergy:F2} J\n" +
                   $"Restitution: {RestitutionCoefficient:F2}";
        }
    }
}