using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Visualization
{
    [RequireComponent(typeof(BallVisualizer))]
    public class BallStateManager : MonoBehaviour
    {
        [SerializeField] private bool smoothMovement = true;
        [SerializeField] private float smoothSpeed = 1000f;
        private Vector3 initialPosition;
        private BallVisualizer visualizer;
        public Ball Ball { get; private set; }

        private void Awake()
        {
            visualizer = GetComponent<BallVisualizer>();
        }

        public void Initialize(Ball ball, Vector3 spawnPosition)
        {
            this.Ball = ball;
            initialPosition = spawnPosition;

            // ビジュアライザーの初期設定
            visualizer.SetSmoothMovement(smoothMovement, smoothSpeed);
            visualizer.Initialize(ball);

            // ボールの初期位置設定
            transform.position = spawnPosition;
        }

        public void ResetPosition()
        {
            transform.position = initialPosition;
            if (Ball != null) Ball.ResetState(initialPosition, Vector3.zero, Vector3.zero);
        }
    }
}