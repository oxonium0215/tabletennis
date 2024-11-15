using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Collisions
{
    public class PaddleCollisionHandler : ICollisionHandler
    {
        public bool DetectCollision(Ball ball, PhysicsObject target, PhysicsSettings settings, out CollisionInfo info)
        {
            info = new CollisionInfo(ball, target);
            var paddle = target as Paddle;
            if (paddle == null)
            {
                info = null;
                return false;
            }

            var localBallPos = TransformToPaddleSpace(ball.Position, paddle);
            var collisionResult = DetectFaceCollisions(localBallPos, paddle, settings);

            if (collisionResult.Collided)
            {
                SetupCollisionInfo(ref info, collisionResult, ball, paddle, settings);
                return true;
            }

            return false;
        }

        public void ResolveCollision(CollisionInfo info, PhysicsSettings settings)
        {
            var (ball, paddle) = GetCollisionObjects(info);
            if (ball == null || paddle == null) return;

            ResolvePosition(ball, info);
            ResolveVelocity(ball, paddle, info, settings);
            // ApplySpinEffect(ball, paddle, info, settings);
        }

        private Vector3 TransformToPaddleSpace(Vector3 worldPosition, Paddle paddle)
        {
            var toPaddle = worldPosition - paddle.Position;
            return Quaternion.Inverse(paddle.Rotation) * toPaddle;
        }

        private FaceCollisionResult DetectFaceCollisions(Vector3 localBallPos, Paddle paddle, PhysicsSettings settings)
        {
            var halfThickness = settings.PaddleThickness * 0.5f;

            // 前面との衝突をチェック
            var frontResult = CheckFaceCollision(localBallPos, halfThickness, true, paddle, settings);
            if (frontResult.Collided) return frontResult;

            // 背面との衝突をチェック
            return CheckFaceCollision(localBallPos, -halfThickness, false, paddle, settings);
        }

        private FaceCollisionResult CheckFaceCollision(
            Vector3 localBallPos,
            float faceOffset,
            bool isFrontFace,
            Paddle paddle,
            PhysicsSettings settings)
        {
            var result = new FaceCollisionResult();
            var distanceToFace = localBallPos.z - faceOffset;

            if (Mathf.Abs(distanceToFace) > settings.BallRadius)
                return result;

            if (IsPointInEllipse(localBallPos, settings.PaddleSize))
            {
                result.Collided = true;
                result.Depth = settings.BallRadius - Mathf.Abs(distanceToFace);
                result.Point = new Vector3(localBallPos.x, localBallPos.y, faceOffset);
            }

            return result;
        }

        private bool IsPointInEllipse(Vector3 localPoint, Vector2 paddleSize)
        {
            var normalizedX = localPoint.x / (paddleSize.x * 0.5f);
            var normalizedY = localPoint.y / (paddleSize.y * 0.5f);
            return normalizedX * normalizedX + normalizedY * normalizedY <= 1.0f;
        }

        private void SetupCollisionInfo(
            ref CollisionInfo info,
            FaceCollisionResult result,
            Ball ball,
            Paddle paddle,
            PhysicsSettings settings)
        {
            info.Point = paddle.Position + paddle.Rotation * result.Point;
            info.Normal = (ball.Position - info.Point).normalized;
            info.Depth = result.Depth;
            info.Ball = ball;
            info.Target = paddle;

            var paddleVelocityAtPoint = paddle.GetVelocityAtPoint(info.Point);
            info.RelativeVelocity = ball.Velocity - paddleVelocityAtPoint;
        }

        private (Ball ball, Paddle paddle) GetCollisionObjects(CollisionInfo info)
        {
            return (info.Ball, info.Target as Paddle);
        }

        private void ResolvePosition(Ball ball, CollisionInfo info)
        {
            ball.Position += info.Normal * info.Depth;
        }

        private void ResolveVelocity(Ball ball, Paddle paddle, CollisionInfo info, PhysicsSettings settings)
        {
            var paddleVelocityAtPoint = paddle.GetVelocityAtPoint(info.Point);
            var relativeVelocity = ball.Velocity - paddleVelocityAtPoint;
            var normalVelocity = Vector3.Dot(relativeVelocity, info.Normal);

            var tangentialVelocity = relativeVelocity - normalVelocity * info.Normal;

            var normalReflectedVelocity = -normalVelocity * settings.PaddleRestitution * info.Normal;

            tangentialVelocity *= 1 - settings.PaddleFriction;

            ball.Velocity = paddleVelocityAtPoint + tangentialVelocity + normalReflectedVelocity;
        }

        private struct FaceCollisionResult
        {
            public bool Collided;
            public float Depth;
            public Vector3 Point;
        }
    }
}