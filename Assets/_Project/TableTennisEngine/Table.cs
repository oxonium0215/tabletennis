using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine
{
    public class Table : PhysicsObject
    {
        public Vector3 Size { get; set; }

        public override void UpdatePhysics(float deltaTime, PhysicsSettings settings)
        {
            // テーブルは静的なので物理更新不要
            // ただし、将来的に変更の可能性があるため、メソッドは残しておく
        }

        // テーブルの境界ボックスを取得
        public Bounds GetBounds()
        {
            return new Bounds(Position, Size);
        }
    }
}