using StepUpTableTennis.TableTennisEngine.Collisions.System;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Visualization
{
    public class PhysicsDebugger : MonoBehaviour
    {
        [Header("Visualization Settings")] public bool ShowTrajectory = true;
        public bool ShowCollisions = true;
        public bool ShowVelocities = true;
        public int TrajectorySteps = 60;
        public float TrajectoryDuration = 1.0f;
        public bool ShowColliders = true;
        [Header("Colors")] public Color TrajectoryColor = Color.yellow;
        public Color CollisionColor = Color.red;
        public Color VelocityColor = Color.blue;
        public Color ColliderColor = Color.green;

        [Header("Energy Display")] [SerializeField]
        private bool showEnergyInfo = true;

        [SerializeField] private Vector2 energyDisplayOffset = new(10, 10);
        [SerializeField] private Color energyTextColor = Color.white;
        private GUIStyle energyDisplayStyle;
        private PhysicsEngine physicsEngine;

        private void OnGUI()
        {
            if (!showEnergyInfo || physicsEngine == null) return;

            var ball = physicsEngine.GetFirstBall();
            if (ball == null) return;

            var kineticEnergy = ball.GetKineticEnergy();
            var rotationalEnergy = ball.GetRotationalEnergy();
            var totalEnergy = ball.GetTotalEnergy();

            var content = string.Format(
                "Ball Energy Info:\n" +
                "Kinetic Energy: {0:F3} J\n" +
                "Rotational Energy: {1:F3} J\n" +
                "Total Energy: {2:F3} J\n" +
                "Velocity: {3:F2} m/s\n" +
                "Angular Velocity: {4:F2} rad/s",
                kineticEnergy,
                rotationalEnergy,
                totalEnergy,
                ball.Velocity.magnitude,
                ball.Spin.magnitude
            );

            GUI.Label(new Rect(energyDisplayOffset.x, energyDisplayOffset.y, 300, 100), content, energyDisplayStyle);
        }

        private void OnDrawGizmos()
        {
            if (physicsEngine == null) return;

            if (ShowColliders) DrawColliders();
            if (ShowTrajectory) DrawTrajectory();
            if (ShowVelocities) DrawVelocities();
            if (ShowCollisions) DrawCollisions();
        }

        public void Initialize(PhysicsEngine engine)
        {
            physicsEngine = engine;
            InitializeGUIStyle();
        }

        private void InitializeGUIStyle()
        {
            energyDisplayStyle = new GUIStyle
            {
                fontSize = 14,
                normal = { textColor = energyTextColor }
            };
            energyDisplayStyle.padding = new RectOffset(5, 5, 5, 5);
        }

        private void DrawColliders()
        {
            Gizmos.color = ColliderColor;

            // パドルのコライダー描画
            var paddle = physicsEngine.GetFirstPaddle();
            if (paddle != null) DrawPaddleCollider(paddle);

            // テーブルのコライダー描画
            var table = physicsEngine.GetFirstTable();
            if (table != null) DrawTableCollider(table);

            // ボールのコライダー描画
            var ball = physicsEngine.GetFirstBall();
            if (ball != null) Gizmos.DrawWireSphere(ball.Position, physicsEngine.Settings.BallRadius);
        }

        private void DrawPaddleCollider(Paddle paddle)
        {
            var segments = 32;
            var halfWidth = physicsEngine.Settings.PaddleSize.x * 0.5f;
            var halfHeight = physicsEngine.Settings.PaddleSize.y * 0.5f;
            var halfThickness = physicsEngine.Settings.PaddleThickness * 0.5f;

            // 前面と背面の点を生成
            var frontPoints = new Vector3[segments + 1];
            var backPoints = new Vector3[segments + 1];
            var edgePoints = new Vector3[segments]; // エッジ部分の点

            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2 / segments;
                var localPoint = new Vector3(
                    Mathf.Cos(angle) * halfWidth,
                    Mathf.Sin(angle) * halfHeight,
                    0
                );

                // 前面の点
                frontPoints[i] = paddle.Position + paddle.Rotation * (localPoint + Vector3.forward * halfThickness);
                // 背面の点
                backPoints[i] = paddle.Position + paddle.Rotation * (localPoint + Vector3.back * halfThickness);

                // エッジ部分の点（最後の点は不要なので segments ではなく segments - 1 まで）
                if (i < segments) edgePoints[i] = paddle.Position + paddle.Rotation * localPoint;
            }

            // 前面の楕円を描画
            for (var i = 0; i < segments; i++) Gizmos.DrawLine(frontPoints[i], frontPoints[i + 1]);

            // 背面の楕円を描画
            for (var i = 0; i < segments; i++) Gizmos.DrawLine(backPoints[i], backPoints[i + 1]);

            // エッジ部分の描画（8点で接続）
            var edgeSegments = 8;
            for (var i = 0; i < edgeSegments; i++)
            {
                var index = i * segments / edgeSegments;
                var nextIndex = (i + 1) * segments / edgeSegments;

                // エッジの輪郭線
                Gizmos.DrawLine(frontPoints[index], frontPoints[nextIndex]);
                Gizmos.DrawLine(backPoints[index], backPoints[nextIndex]);

                // 前面と背面を接続する線
                Gizmos.DrawLine(frontPoints[index], backPoints[index]);
            }

            // パドルの法線方向の表示
            if (ShowColliders)
            {
                var normalLength = 0.1f;
                var center = paddle.Position;
                var normal = paddle.Normal;

                // 前面の法線（青色）
                Gizmos.color = Color.blue;
                var frontCenter = center + paddle.Rotation * (Vector3.forward * halfThickness);
                Gizmos.DrawLine(frontCenter, frontCenter + normal * normalLength);

                // 背面の法線（赤色）
                Gizmos.color = Color.red;
                var backCenter = center + paddle.Rotation * (Vector3.back * halfThickness);
                Gizmos.DrawLine(backCenter, backCenter - normal * normalLength);

                // 元の色に戻す
                Gizmos.color = ColliderColor;
            }

            // エッジ部分の衝突判定範囲の可視化（オプション）
            if (ShowColliders)
            {
                var ballRadius = physicsEngine.Settings.BallRadius;
                // 外側の衝突判定範囲
                DrawPaddleOutline(paddle, halfWidth + ballRadius, halfHeight + ballRadius, halfThickness, segments,
                    new Color(ColliderColor.r, ColliderColor.g, ColliderColor.b, 0.2f));
            }
        }

        private void DrawPaddleOutline(Paddle paddle, float width, float height, float thickness, int segments,
            Color color)
        {
            var originalColor = Gizmos.color;
            Gizmos.color = color;

            var points = new Vector3[segments + 1];
            for (var i = 0; i <= segments; i++)
            {
                var angle = i * Mathf.PI * 2 / segments;
                var localPoint = new Vector3(
                    Mathf.Cos(angle) * width,
                    Mathf.Sin(angle) * height,
                    0
                );
                points[i] = paddle.Position + paddle.Rotation * localPoint;
            }

            // 輪郭線を描画
            for (var i = 0; i < segments; i++) Gizmos.DrawLine(points[i], points[i + 1]);

            Gizmos.color = originalColor;
        }

        private void DrawTableCollider(Table table)
        {
            // テーブルの境界ボックスを描画
            var bounds = new Bounds(table.Position, physicsEngine.Settings.TableSize);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }

        private void DrawTrajectory()
        {
            // 現在は最初のボールの軌道のみを表示
            var ball = physicsEngine.GetFirstBall();
            if (ball == null) return;

            var positions = physicsEngine.PredictTrajectory(ball, TrajectoryDuration);

            Gizmos.color = TrajectoryColor;
            for (var i = 0; i < positions.Count - 1; i++) Gizmos.DrawLine(positions[i], positions[i + 1]);
        }

        private void DrawVelocities()
        {
            var ball = physicsEngine.GetFirstBall();
            if (ball == null) return;

            Gizmos.color = VelocityColor;
            var velocityScale = 0.1f;
            var start = ball.Position;
            var end = start + ball.Velocity * velocityScale;

            Gizmos.DrawLine(start, end);
            DrawArrowHead(end, ball.Velocity.normalized, 0.05f);
        }

        private void DrawCollisions()
        {
            foreach (var collision in physicsEngine.GetRecentCollisions()) DrawCollision(collision);
        }

        private void DrawCollision(CollisionInfo collision)
        {
            Gizmos.color = CollisionColor;

            // 衝突点を球体で表示
            Gizmos.DrawWireSphere(collision.Point, 0.02f);

            // 法線ベクトルを表示
            var normalLength = 0.1f;
            var normalEnd = collision.Point + collision.Normal * normalLength;
            Gizmos.DrawLine(collision.Point, normalEnd);
            DrawArrowHead(normalEnd, collision.Normal, 0.02f);

            // 相対速度の表示
            if (ShowVelocities && collision.RelativeVelocity.sqrMagnitude > 0)
            {
                Gizmos.color = VelocityColor * 0.7f;
                var velocityEnd = collision.Point + collision.RelativeVelocity * 0.1f;
                Gizmos.DrawLine(collision.Point, velocityEnd);
            }
        }

        private void DrawArrowHead(Vector3 position, Vector3 direction, float size)
        {
            if (direction.sqrMagnitude < float.Epsilon) return;

            var rotation = Quaternion.LookRotation(direction);
            var rightVector = rotation * Quaternion.Euler(0, 30, 0) * Vector3.back * size;
            var leftVector = rotation * Quaternion.Euler(0, -30, 0) * Vector3.back * size;

            Gizmos.DrawLine(position, position + rightVector);
            Gizmos.DrawLine(position, position + leftVector);
        }

        public void DrawPath(Vector3[] positions)
        {
            if (positions == null || positions.Length < 2) return;

            Gizmos.color = TrajectoryColor;
            for (var i = 0; i < positions.Length - 1; i++) Gizmos.DrawLine(positions[i], positions[i + 1]);
        }
    }
}