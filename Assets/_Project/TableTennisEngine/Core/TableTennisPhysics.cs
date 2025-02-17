// TableTennisPhysics.cs
using System;
using System.Collections.Generic;
using System.Linq;
using StepUpTableTennis.TableTennisEngine.Collisions.Events;
using StepUpTableTennis.TableTennisEngine.Collisions.System;
using StepUpTableTennis.TableTennisEngine.Components;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Core
{
    public class TableTennisPhysics
    {
        private readonly List<Ball> balls = new();
        private readonly CollisionSystem collisionSystem;
        private readonly List<Paddle> paddles = new();
        private readonly List<Table> tables = new();
        private readonly List<BoxColliderComponent> boxColliders = new();

        public TableTennisPhysics(PhysicsSettings settings)
        {
            Settings = settings;
            collisionSystem = new CollisionSystem(settings);
        }

        public PhysicsSettings Settings { get; }
        public event Action<CollisionEventArgs> OnCollision;

        public void AddBall(Ball ball)
        {
            ball.Initialize(Settings);
            balls.Add(ball);
        }

        public void AddPaddle(Paddle paddle)
        {
            paddles.Add(paddle);
        }

        public void AddTable(Table table)
        {
            tables.Add(table);
        }

        public void AddBoxCollider(BoxColliderComponent collider)
        {
            collider.Initialize(Settings);
            boxColliders.Add(collider);
        }

        public void Simulate(float deltaTime)
        {
            var subSteps = Mathf.Min(Mathf.CeilToInt(deltaTime / Settings.TimeStep), Settings.MaxSubsteps);
            var subDeltaTime = deltaTime / subSteps;

            collisionSystem.BeginFrame();

            for (var i = 0; i < subSteps; i++)
            {
                // 物理状態の更新
                foreach (var ball in balls)
                    ball.UpdatePhysics(subDeltaTime, Settings);
                foreach (var paddle in paddles)
                    paddle.UpdatePhysics(subDeltaTime, Settings);

                // 従来のコリジョン処理
                collisionSystem.DetectAndResolveCollisions(balls, paddles, tables);

                // BoxColliderComponentとの衝突チェック
                foreach (var ball in balls)
                foreach (var boxCollider in boxColliders)
                {
                    if (boxCollider.CheckCollision(ball, out var collisionInfo))
                    {
                        boxCollider.ResolveCollision(collisionInfo);
                        OnCollision?.Invoke(new CollisionEventArgs(collisionInfo));
                    }
                }

                var recentCollisions = collisionSystem.CurrentFrameCollisions;
                foreach (var collisionInfo in recentCollisions)
                {
                    // BoxColliderはすでにOnCollisionを呼んでいるので、その重複を避けたい場合は条件分岐可能
                    // ただしここではシンプルに全衝突対象で呼び出し
                    if (collisionInfo.Target is Paddle || collisionInfo.Target is Table)
                    {
                        OnCollision?.Invoke(new CollisionEventArgs(collisionInfo));
                    }
                }
            }
        }

        public void RemoveBoxCollider(BoxColliderComponent collider)
        {
            boxColliders.Remove(collider);
        }

        public void RemoveBall(Ball ball)
        {
            balls.Remove(ball);
        }

        public void ClearBalls()
        {
            balls.Clear();
        }

        public IEnumerable<CollisionInfo> GetRecentCollisions()
        {
            return collisionSystem.CurrentFrameCollisions;
        }

        // デバッグ用のメソッド
        public Ball GetFirstBall()
        {
            return balls.FirstOrDefault();
        }

        public Paddle GetFirstPaddle()
        {
            var paddle = paddles.FirstOrDefault();
            return paddle;
        }

        public Table GetFirstTable()
        {
            return tables.FirstOrDefault();
        }

        // 軌道予測メソッド (ここに追加)
        public List<Vector3> PredictTrajectory(Ball ball, float duration)
        {
            var positions = new List<Vector3>();
            var predictionBall = new Ball();
            predictionBall.Initialize(Settings);
            predictionBall.ResetState(ball.Position, ball.Velocity, ball.Spin);

            var steps = Mathf.CeilToInt(duration / Settings.TimeStep);
            for (var i = 0; i < steps; i++)
            {
                predictionBall.UpdatePhysics(Settings.TimeStep, Settings);
                positions.Add(predictionBall.Position);
            }

            return positions;
        }
    }
}