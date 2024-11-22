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
            }
            else
            {
                Debug.LogError("BallStateManager component not found on ball visualizer prefab");
            }

            // 物理エンジンにボールを登録
            physicsEngine.AddBall(ball);

            // 初期状態の設定（位置、速度、角速度）
            ball.ResetState(position, velocity, angularVelocity);

            Debug.Log($"Ball spawned at {position} with velocity {velocity}");
            return ball;
        }
    }
}