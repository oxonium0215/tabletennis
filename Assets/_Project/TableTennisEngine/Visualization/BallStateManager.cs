// BallStateManager.cs
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Visualization
{
    [RequireComponent(typeof(BallVisualizer))]
    [RequireComponent(typeof(PredictionVisualizer))]
    public class BallStateManager : MonoBehaviour
    {
        [Header("Movement Smoothing")]
        [SerializeField] private bool smoothMovement = true;
        [SerializeField] private float smoothSpeed = 1000f;

        private Vector3 initialPosition;

        private BallVisualizer visualizer;
        private PredictionVisualizer predictionVisualizer;

        private TableTennisPhysics physicsEngine;     // ★追加★

        public Ball Ball { get; private set; }

        private void Awake()
        {
            visualizer = GetComponent<BallVisualizer>();
            predictionVisualizer = GetComponent<PredictionVisualizer>();
        }

        /// <summary>
        /// BallSpawner から呼ばれる。
        /// </summary>
        public void Initialize(Ball ball, Vector3 spawnPosition, TableTennisPhysics engine)
        {
            Ball = ball;
            initialPosition = spawnPosition;
            physicsEngine = engine;

            /* 現在ビジュアライザーを初期化 */
            visualizer.SetSmoothMovement(smoothMovement, smoothSpeed);
            visualizer.Initialize(ball);

            /* 未来予測ビジュアライザー初期化 */
            if (predictionVisualizer != null && physicsEngine != null)
            {
                float radius = physicsEngine.Settings.BallRadius;
                predictionVisualizer.Initialize(ball, physicsEngine, radius);
            }

            transform.position = spawnPosition;
        }

        public void ResetPosition()
        {
            transform.position = initialPosition;
            if (Ball != null)
                Ball.ResetState(initialPosition, Vector3.zero, Vector3.zero);
        }
    }
}
