// BallSpawner.cs
using System.Collections.Generic;
using StepUpTableTennis.DataManagement.Recording;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Visualization;
using UnityEngine;

namespace StepUpTableTennis.Training
{
    public class BallSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject ballVisualizerPrefab;

        private IMotionRecorder motionRecorder;
        private TableTennisPhysics physicsEngine;

        // Ball と BallStateManager のペア
        private readonly Dictionary<Ball, BallStateManager> ballMap = new();

        public void Initialize(TableTennisPhysics engine, IMotionRecorder recorder)
        {
            physicsEngine = engine;
            motionRecorder = recorder;
            Debug.Log($"BallSpawner initialized (recorder={(recorder != null ? "yes" : "no")})");
        }

        public Ball SpawnBall(Vector3 position, Vector3 velocity, Vector3 angularVelocity)
        {
            /* --------- 新規 Ball 生成 --------- */
            var ball = new Ball();
            ball.Initialize(physicsEngine.Settings);

            /* --------- 可視化 --------- */
            var vizObj = Instantiate(ballVisualizerPrefab, position, Quaternion.identity);
            var stateManager = vizObj.GetComponent<BallStateManager>();
            if (stateManager == null)
            {
                Debug.LogError("BallStateManager component not found on prefab");
                Destroy(vizObj);
                return null;
            }

            stateManager.Initialize(ball, position, physicsEngine);

            // MotionRecorder へ通知
            motionRecorder?.TrackBall(stateManager);

            // 管理テーブルに追加
            ballMap.Add(ball, stateManager);

            /* --------- 物理エンジンへ登録 & 状態初期化 --------- */
            physicsEngine.AddBall(ball);
            ball.ResetState(position, velocity, angularVelocity);

            return ball;
        }

        public void DestroyBall(Ball ball)
        {
            if (ballMap.TryGetValue(ball, out var sm))
            {
                Destroy(sm.gameObject);
                ballMap.Remove(ball);
            }
            physicsEngine?.RemoveBall(ball);
        }

        public void DestroyAllBalls()
        {
            foreach (var sm in ballMap.Values)
                if (sm) Destroy(sm.gameObject);
            ballMap.Clear();
            physicsEngine?.ClearBalls();
        }
    }
}
