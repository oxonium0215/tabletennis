using StepUpTableTennis.TableTennisEngine.Core.Models;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Objects.Base
{
    public abstract class PhysicsObject
    {
        // 基本的な物理プロパティ
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Quaternion Rotation { get; set; }

        public virtual void UpdatePhysics(float deltaTime, PhysicsSettings settings)
        {
            // 基本的な物理更新（オーバーライド可能）
            Position += Velocity * deltaTime;
        }
    }
}