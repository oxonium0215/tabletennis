using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects.Base;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Objects
{
    public class Paddle : PhysicsObject
    {
        public Vector3 AngularVelocity { get; set; }
        public Vector3 Normal => Rotation * Vector3.forward;

        public override void UpdatePhysics(float deltaTime, PhysicsSettings settings)
        {
            // パドルの物理更新
            // 主にコントローラーからの入力で動くため、最小限の物理計算
            base.UpdatePhysics(deltaTime, settings);

            // 回転の更新
            if (AngularVelocity.sqrMagnitude > 0)
            {
                var rotation = Quaternion.Euler(AngularVelocity * deltaTime);
                Rotation *= rotation;
            }
        }

        // パドルの速度を特定の点で取得（衝突計算用）
        public Vector3 GetVelocityAtPoint(Vector3 point)
        {
            var r = point - Position;
            return Velocity + Vector3.Cross(AngularVelocity, r);
        }
    }
}