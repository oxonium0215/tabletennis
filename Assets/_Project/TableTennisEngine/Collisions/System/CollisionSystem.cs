using System;
using System.Collections.Generic;
using StepUpTableTennis.TableTennisEngine.Collisions.Handlers;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Objects.Base;

namespace StepUpTableTennis.TableTennisEngine.Collisions.System
{
    public class CollisionSystem
    {
        private readonly List<CollisionInfo> accumulatedCollisions = new(); // 追加: 蓄積用リスト
        private readonly Dictionary<Type, ICollisionHandler> collisionHandlers;
        private readonly List<CollisionInfo> currentStepCollisions = new();
        private readonly PhysicsSettings settings;

        public CollisionSystem(PhysicsSettings settings)
        {
            this.settings = settings;
            collisionHandlers = new Dictionary<Type, ICollisionHandler>
            {
                { typeof(Paddle), new PaddleCollisionHandler() },
                { typeof(Table), new RectangularObjectCollisionHandler() }
            };
        }

        public IReadOnlyList<CollisionInfo> CurrentFrameCollisions => accumulatedCollisions;

        public void BeginFrame()
        {
            accumulatedCollisions.Clear(); // フレーム開始時にクリア
        }

        public void DetectAndResolveCollisions(List<Ball> balls, List<Paddle> paddles, List<Table> tables)
        {
            currentStepCollisions.Clear(); // 現在のステップの衝突のみクリア

            foreach (var ball in balls)
            {
                foreach (var paddle in paddles)
                    CheckAndResolveCollision(ball, paddle);

                foreach (var table in tables)
                    CheckAndResolveCollision(ball, table);
            }

            // 現在のステップでの衝突を蓄積リストに追加
            accumulatedCollisions.AddRange(currentStepCollisions);
        }

        private void CheckAndResolveCollision(Ball ball, PhysicsObject other)
        {
            var handlerType = other.GetType();
            if (collisionHandlers.TryGetValue(handlerType, out var handler))
                if (handler.DetectCollision(ball, other, settings, out var collisionInfo))
                {
                    currentStepCollisions.Add(collisionInfo);
                    handler.ResolveCollision(collisionInfo, settings);
                }
        }
    }
}