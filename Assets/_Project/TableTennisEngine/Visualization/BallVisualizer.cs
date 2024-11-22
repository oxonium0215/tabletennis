using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Visualization
{
    public class BallVisualizer : MonoBehaviour
    {
        [SerializeField] private bool showTrails = true;
        [SerializeField] private bool showDebugInfo = true;
        private bool isInitialized;
        private Vector3 previousPosition;
        private bool smoothMovement = true;
        private float smoothSpeed = 20f;
        private Ball targetBall;
        private TrailRenderer trailRenderer;

        private void Awake()
        {
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer)
                trailRenderer.enabled = showTrails;
        }

        private void Update()
        {
            if (!isInitialized || targetBall == null)
            {
                Debug.LogWarning("BallVisualizer not initialized or targetBall is null");
                return;
            }

            UpdateVisualization();
        }

        public void Initialize(Ball ball)
        {
            targetBall = ball;
            if (ball != null)
            {
                UpdateTransform(ball.Position, ball.Rotation);
                previousPosition = ball.Position;
                isInitialized = true;
            }
        }

        public void SetSmoothMovement(bool enabled, float speed = 20f)
        {
            smoothMovement = enabled;
            smoothSpeed = speed;
        }

        public void SetTrailEnabled(bool enabled)
        {
            showTrails = enabled;
            if (trailRenderer)
                trailRenderer.enabled = enabled;
        }

        public void SetDebugEnabled(bool enabled)
        {
            showDebugInfo = enabled;
        }

        private void UpdateVisualization()
        {
            if (showDebugInfo)
                Debug.Log($"Ball Physics State - Position: {targetBall.Position}, " +
                          $"Velocity: {targetBall.Velocity}, " +
                          $"Current Transform Position: {transform.position}");

            if (smoothMovement)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetBall.Position,
                    Time.deltaTime * smoothSpeed
                );

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetBall.Rotation,
                    Time.deltaTime * smoothSpeed
                );
            }
            else
            {
                UpdateTransform(targetBall.Position, targetBall.Rotation);
            }

            CheckAndUpdateTrail();
        }

        private void UpdateTransform(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
        }

        private void CheckAndUpdateTrail()
        {
            if (showTrails && trailRenderer && Vector3.Distance(previousPosition, targetBall.Position) > 1f)
                trailRenderer.Clear();
            previousPosition = targetBall.Position;
        }
    }
}