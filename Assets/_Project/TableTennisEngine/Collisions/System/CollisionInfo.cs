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
            BallBox,  // 新しい汎用的な直方体との衝突タイプ
            Unknown
        }

        public CollisionInfo(Ball ball, PhysicsObject target)
        {
            Ball = ball;
            Target = target;
            Type = DetermineCollisionType(target);
        }

        // 衝突の基本情報
        public Vector3 Point { get; set; }
        public Vector3 Normal { get; set; }
        public float Depth { get; set; }
        public Vector3 RelativeVelocity { get; set; }

        // 衝突オブジェクト
        public Ball Ball { get; set; }
        public PhysicsObject Target { get; set; }

        // 衝突の種類を示す列挙型
        public CollisionType Type { get; set; }

        private CollisionType DetermineCollisionType(PhysicsObject target)
        {
            return target switch
            {
                Table => CollisionType.BallTable,
                Paddle => CollisionType.BallPaddle,
                _ => CollisionType.Unknown
            };
        }

        // 衝突の強さを計算
        public float GetImpactForce(PhysicsSettings settings)
        {
            var normalVelocity = Mathf.Abs(Vector3.Dot(RelativeVelocity, Normal));
            var collisionTime = Mathf.Max(Depth / normalVelocity, settings.TimeStep);
            return settings.BallMass * normalVelocity / collisionTime;
        }
    }
}