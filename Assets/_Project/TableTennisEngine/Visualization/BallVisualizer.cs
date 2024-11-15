using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Visualization
{
    public class BallVisualizer : MonoBehaviour
    {
        [Header("Visualization")] [SerializeField]
        private bool showTrails = true;

        [SerializeField] private bool smoothMovement = true;
        [SerializeField] private float smoothSpeed = 20f;
        [SerializeField] private bool showDebugInfo = true;
        private bool isInitialized;
        private Vector3 previousPosition;
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

            if (showDebugInfo)
                Debug.Log(
                    $"Ball Physics State - Position: {targetBall.Position}, Velocity: {targetBall.Velocity}, Current Transform Position: {transform.position}");

            if (smoothMovement)
            {
                // 補間を使用してスムーズに移動
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
                // 直接位置と回転を更新
                UpdatePosition(targetBall.Position);
                UpdateRotation(targetBall.Rotation);
            }

            // 急激な位置の変化を検出してトレイルをリセット
            if (showTrails && trailRenderer && Vector3.Distance(previousPosition, targetBall.Position) > 1f)
                trailRenderer.Clear();

            previousPosition = targetBall.Position;
        }

        public void Initialize(Ball ball)
        {
            targetBall = ball;
            if (ball != null)
            {
                UpdatePosition(ball.Position);
                UpdateRotation(ball.Rotation);
                previousPosition = ball.Position;
                isInitialized = true;
            }
        }

        private void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }

        private void UpdateRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void SetTrailEnabled(bool enabled)
        {
            showTrails = enabled;
            if (trailRenderer)
                trailRenderer.enabled = enabled;
        }
    }
}