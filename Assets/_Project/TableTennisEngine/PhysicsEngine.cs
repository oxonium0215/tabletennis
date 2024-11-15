using System;
using System.Collections.Generic;
using System.Linq;
using StepUpTableTennis.TableTennisEngine.Events;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine
{
    public class PhysicsEngine
    {
        private readonly List<Ball> balls = new();
        private readonly CollisionSystem collisionSystem;
        private readonly List<Paddle> paddles = new();
        private readonly List<Table> tables = new();

        public PhysicsEngine(PhysicsSettings settings)
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

        public void Simulate(float deltaTime)
        {
            var subSteps = Mathf.Min(Mathf.CeilToInt(deltaTime / Settings.TimeStep), Settings.MaxSubsteps);
            var subDeltaTime = deltaTime / subSteps;

            collisionSystem.BeginFrame(); // フレーム開始時にコリジョン情報をクリア

            for (var i = 0; i < subSteps; i++)
            {
                // 物理状態の更新
                foreach (var ball in balls)
                    ball.UpdatePhysics(subDeltaTime, Settings);
                foreach (var paddle in paddles)
                    paddle.UpdatePhysics(subDeltaTime, Settings);

                collisionSystem.DetectAndResolveCollisions(balls, paddles, tables);
            }

            // 蓄積された全ての衝突に対してイベントを発火
            foreach (var collision in collisionSystem.CurrentFrameCollisions)
                OnCollision?.Invoke(new CollisionEventArgs(collision));
        }

        public IEnumerable<BallState> GetBallStates()
        {
            return balls.Select(ball => new BallState
            {
                Position = ball.Position,
                Velocity = ball.Velocity,
                Spin = ball.Spin
            });
        }

        public IEnumerable<CollisionInfo> GetRecentCollisions()
        {
            return collisionSystem.CurrentFrameCollisions;
        }

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
    }
}