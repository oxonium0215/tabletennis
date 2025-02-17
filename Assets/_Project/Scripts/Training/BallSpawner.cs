// BallSpawner.cs
using StepUpTableTennis.DataManagement.Recording;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Visualization;
using UnityEngine;
using System.Collections.Generic;

namespace StepUpTableTennis.Training
{
    public class BallSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject ballVisualizerPrefab;
        private IMotionRecorder motionRecorder;
        private TableTennisPhysics physicsEngine;

        // BallStateManager と Ball のペアを管理する Dictionary
        private Dictionary<Ball, BallStateManager> ballMap = new Dictionary<Ball, BallStateManager>();

        public void Initialize(TableTennisPhysics engine, IMotionRecorder recorder)
        {
            physicsEngine = engine;
            motionRecorder = recorder;
            Debug.Log($"BallSpawner initialized with recorder: {(recorder != null ? "yes" : "no")}");
        }

        public Ball SpawnBall(Vector3 position, Vector3 velocity, Vector3 angularVelocity)
        {
            // ボールの物理オブジェクトを作成
            var ball = new Ball();
            ball.Initialize(physicsEngine.Settings);

            // ビジュアライザーの生成と初期化
            var visualizerObj = Instantiate(ballVisualizerPrefab, position, Quaternion.identity);
            var stateManager = visualizerObj.GetComponent<BallStateManager>();
            if (stateManager != null)
            {
                stateManager.Initialize(ball, position);
                // 新しく生成したボールをモーションレコーダーに通知
                motionRecorder?.TrackBall(stateManager);

                // Ball と BallStateManager を Dictionary に追加
                ballMap.Add(ball, stateManager);
            }
            else
            {
                Debug.LogError("BallStateManager component not found on ball visualizer prefab");
            }

            // 物理エンジンにボールを登録
            physicsEngine.AddBall(ball);

            // 初期状態の設定（位置、速度、角速度）
            ball.ResetState(position, velocity, angularVelocity);

            return ball;
        }

        // 特定のボールを破棄するメソッド
        public void DestroyBall(Ball ball)
        {
            if (ballMap.ContainsKey(ball))
            {
                // BallStateManager を破棄
                Destroy(ballMap[ball].gameObject);

                // Dictionary から削除
                ballMap.Remove(ball);

                // TableTennisPhysics からも Ball を削除
                physicsEngine?.RemoveBall(ball);

            }
        }

        // 既存の DestroyAllBalls() は残しておいても良いし、必要なければ削除しても良い
        public void DestroyAllBalls()
        {
            foreach (var visualizer in FindObjectsOfType<BallStateManager>())
            {
                Destroy(visualizer.gameObject);
            }
            ballMap.Clear();

            // 物理エンジンからもすべてのボールを削除 (必要に応じて)
            physicsEngine?.ClearBalls();  // このメソッドは TableTennisPhysics に追加する必要がある
        }
    }
}