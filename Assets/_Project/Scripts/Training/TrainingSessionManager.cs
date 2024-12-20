using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StepUpTableTennis.DataManagement.Core.Models;
using StepUpTableTennis.DataManagement.Recording;
using StepUpTableTennis.DataManagement.Storage;
using StepUpTableTennis.TableTennisEngine.Collisions.Events;
using StepUpTableTennis.TableTennisEngine.Collisions.System;
using StepUpTableTennis.TableTennisEngine.Components;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Trajectory;
using StepUpTableTennis.TableTennisEngine.Visualization;
using StepUpTableTennis.Training.Course;
using UnityEngine;
using UnityEngine.Events;
using Oculus.Haptics;

namespace StepUpTableTennis.Training
{
    [Serializable]
    public class SessionStatisticsEvent : UnityEvent<int, int, float>
    {
    }

    public class TrainingSessionManager : MonoBehaviour
    {
        [Header("Core Components")]
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private BallLauncher ballLauncher;
        [SerializeField] private BallSpawner ballSpawner;
        [SerializeField] private PhysicsDebugger physicsDebugger;

        [Header("Physics Objects")]
        [SerializeField] private BoxColliderComponent tableCollider; // Changed from TableSetup
        [SerializeField] private BoxColliderComponent netCollider;   // Added for net collision
        [SerializeField] private PaddleSetup paddleStateHandler;

        [Header("Session Settings")]
        public DifficultySettings difficultySettings = new();
        [SerializeField] private bool autoStart;

        [Header("Session Control")]
        public int shotsPerSession = 10;
        public float shotInterval = 3f;
        public bool removeBalLAfterPaddleHit;
        [SerializeField] public float ballRemovalForce = 1000f;

        [Header("Recording Components")]
        [SerializeField] private Transform headTransform;

        [Header("Events")]
        public UnityEvent onSessionStart;
        public UnityEvent onSessionComplete;
        public UnityEvent onSessionPause;
        public UnityEvent onSessionResume;
        public SessionStatisticsEvent onSessionStatistics;

        private TrainingSession currentSession;
        private int currentShotIndex;
        private TrainingDataStorage dataStorage;
        private bool isSessionActive;
        private IMotionRecorder motionRecorder;
        private float nextShotTime;
        private TableTennisPhysics physicsEngine;
        private DateTime sessionStartTime;
        private int successfulShots;
        private int totalExecutedShots;
        public HapticClip clip;
        private HapticClipPlayer player;

        // --- ここから追加 ---
        // 最後に生成されたショットパラメータを可視化するためのフィールド
        private Vector3 lastLaunchPosition;
        private Vector3 lastBounceTargetPosition;
        private bool hasLastShotParameters; // 最後のショット情報があるかどうか
        public Color aimLineColor = Color.magenta; // 目標ライン表示用のカラー
        public float aimSphereRadius = 0.05f; // バウンド目標位置を示す球の大きさ
        // --- ここまで追加 ---

        private void Start()
        {
            player = new HapticClipPlayer(clip);
            InitializeComponents();

            var courseSettings = new CourseSettings(
                tableCollider,
                ballLauncher.transform,
                physicsSettings.BallRadius
            );
            difficultySettings.Initialize(courseSettings);

            if (autoStart) StartNewSession();
        }

        private void Update()
        {
            if (!isSessionActive) return;

            physicsEngine.Simulate(Time.deltaTime);
            motionRecorder.UpdateRecording();

            if (Time.time >= nextShotTime && currentShotIndex < currentSession.Shots.Count)
            {
                ExecuteNextShot();
            }
            CheckSessionCompletion();
        }

        private void OnValidate()
        {
            ValidateComponents();
        }

        public event Action<CollisionEventArgs> OnCollisionOccurred;

        public async Task<bool> PrepareNewSession()
        {
            if (isSessionActive)
            {
                Debug.LogWarning("Cannot prepare new session while current session is active");
                return false;
            }

            try
            {
                var sessionId = SessionId.Generate();
                var config = new SessionConfig(
                    new SessionDifficulty(
                        difficultySettings.SpeedLevel,
                        difficultySettings.SpinLevel,
                        difficultySettings.CourseLevel
                    ),
                    shotsPerSession,
                    shotInterval
                );

                var shots = await GenerateAndCalculateShots(config);

                currentSession = new TrainingSession(
                    sessionId,
                    DateTime.Now,
                    config,
                    shots
                );

                motionRecorder.StartSession(currentSession.Shots);

                currentShotIndex = 0;
                successfulShots = 0;
                totalExecutedShots = 0;

                Debug.Log($"Session prepared successfully: {sessionId.Value}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to prepare session: {e.Message}");
                return false;
            }
        }

        public async void StartNewSession()
        {
            if (isSessionActive)
            {
                StopSession();
                await Task.Delay(100);
            }

            ResetSessionState();

            if (!await PrepareNewSession())
                return;

            isSessionActive = true;
            sessionStartTime = DateTime.Now;
            nextShotTime = Time.time;

            Debug.Log($"Started new session: {currentSession.Id.Value}");

            onSessionStart?.Invoke();
        }

        public void StopSession()
        {
            if (!isSessionActive) return;
            CompleteSession();
            isSessionActive = false;
        }

        public void PauseSession()
        {
            if (!isSessionActive) return;
            isSessionActive = false;
            onSessionPause?.Invoke();
        }

        public void ResumeSession()
        {
            if (isSessionActive || currentSession == null) return;
            isSessionActive = true;
            nextShotTime = Time.time;
            onSessionResume?.Invoke();
        }

        private void InitializeComponents()
        {
            physicsEngine = new TableTennisPhysics(physicsSettings);

            if (tableCollider != null)
                physicsEngine.AddBoxCollider(tableCollider);

            if (netCollider != null)
                physicsEngine.AddBoxCollider(netCollider);

            if (paddleStateHandler != null && paddleStateHandler.Paddle != null)
                physicsEngine.AddPaddle(paddleStateHandler.Paddle);

            motionRecorder = new MotionRecorder(
                paddleStateHandler,
                headTransform ?? Camera.main?.transform
            );

            if (ballSpawner != null)
                ballSpawner.Initialize(physicsEngine, motionRecorder);

            if (physicsDebugger != null)
                physicsDebugger.Initialize(physicsEngine);

            dataStorage = new TrainingDataStorage(
                Path.Combine(Application.persistentDataPath, "TrainingData")
            );

            physicsEngine.OnCollision += HandleCollision;
            physicsEngine.OnCollision += args => OnCollisionOccurred?.Invoke(args);
            
            // --- ここから追加 ---
            // 衝突時のハプティクス処理を追加
            physicsEngine.OnCollision += HandleHapticsOnCollision;
            // --- ここまで追加 ---
        }
        
        private void HandleHapticsOnCollision(CollisionEventArgs args)
        {
            if (args.CollisionInfo.Type == CollisionInfo.CollisionType.BallPaddle)
            {
                // 衝突強度を計算
                float impactForce = args.CollisionInfo.GetImpactForce(physicsSettings);
                Debug.Log("Collision occurred: " + impactForce);

                // impactForceはある程度の範囲に正規化した方がよい
                // 例：最大値を仮に100N程度と想定して0～1に正規化
                float normalizedForce = Mathf.Clamp01(impactForce / 10f);

                // normalizedForceに応じてハプティクス強度を決定
                float amplitude = normalizedForce; // 0～1
                float duration = 0.1f + 0.2f * normalizedForce; // 衝突が大きいほど長く振動
                player.amplitude = amplitude;

                // ハプティクスを再生
                player.Play(Controller.Right);
            }
        }

        private async Task<List<TrainingShot>> GenerateAndCalculateShots(SessionConfig config)
        {
            var shots = new List<TrainingShot>();
            var calculator = new BallTrajectoryCalculator(physicsSettings);

            for (var i = 0; i < config.TotalShots; i++)
            {
                var parameters = GenerateShotParameters(config.Difficulty);
                var calculatedParams = await calculator.CalculateTrajectoryAsync(parameters);
                var shot = new TrainingShot(calculatedParams);
                shots.Add(shot);

                // --- ここから追加 ---
                // 生成したショットパラメータを記録し、後でGizmosで表示できるようにする
                lastLaunchPosition = calculatedParams.LaunchPosition;
                lastBounceTargetPosition = calculatedParams.AimPosition;
                hasLastShotParameters = true;
                // --- ここまで追加 ---
            }

            return shots;
        }

        private ShotParameters GenerateShotParameters(SessionDifficulty difficulty)
        {
            if (ballLauncher == null)
                throw new InvalidOperationException("BallLauncher reference not set!");

            var launchPosition = difficultySettings.GetLaunchPosition();
            var bounceTargetPosition = difficultySettings.GetRandomBouncePosition();

            var speed = difficultySettings.GetSpeedForLevel();
            var spin = difficultySettings.GetSpinForLevel();

            var shotParameters = new ShotParameters(
                launchPosition,
                bounceTargetPosition,
                speed,
                spin.RotationsPerSecond,
                spin.SpinAxis
            );

            Debug.Log(
                $"Generated Shot: Launch={launchPosition}, " +
                $"Bounce={bounceTargetPosition}, " +
                $"Speed={speed:F2}m/s, " +
                $"Spin={spin.RotationsPerSecond} rps, " +
                $"Axis={spin.SpinAxis}"
            );

            return shotParameters;
        }

        private void ExecuteNextShot()
        {
            if (currentShotIndex >= currentSession.Shots.Count)
                return;

            var shot = currentSession.Shots[currentShotIndex];
            var parameters = shot.Parameters;

            if (!parameters.IsCalculated)
            {
                Debug.LogError("Shot parameters have not been calculated");
                return;
            }

            ballSpawner.SpawnBall(
                parameters.LaunchPosition,
                parameters.InitialVelocity.Value,
                parameters.InitialAngularVelocity.Value
            );

            shot.RecordExecution(DateTime.Now, false);
            motionRecorder.SetCurrentShot(currentShotIndex);

            // --- ここから追加 ---
            // 発射するたびに更新
            lastLaunchPosition = parameters.LaunchPosition;
            lastBounceTargetPosition = parameters.AimPosition;
            hasLastShotParameters = true;
            // --- ここまで追加 ---

            currentShotIndex++;
            nextShotTime = Time.time + shotInterval;
            totalExecutedShots++;
        }


        private void CheckSessionCompletion()
        {
            if (currentShotIndex >= currentSession.Shots.Count && Time.time > nextShotTime + 1.0f)
            {
                Debug.Log($"Session completed. Total shots: {totalExecutedShots}, Successful: {successfulShots}");
                StopSession();
            }
        }

        private async void CompleteSession()
        {
            if (currentSession == null) return;

            motionRecorder.StopSession();

            var statistics = new SessionStatistics(
                totalExecutedShots,
                successfulShots,
                DateTime.Now
            );

            currentSession.Complete(statistics);

            try
            {
                await dataStorage.SaveSessionAsync(currentSession);
                Debug.Log($"Session data saved successfully: {currentSession.Id.Value}");

                var successRate = totalExecutedShots > 0 ? (float)successfulShots / totalExecutedShots * 100f : 0f;
                onSessionStatistics?.Invoke(successfulShots, totalExecutedShots, successRate);
                onSessionComplete?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save session data: {e.Message}");
            }

            ResetSessionState();
        }

        public (int successfulShots, int totalShots, float successRate) GetCurrentStatistics()
        {
            var successRate = totalExecutedShots > 0 ? (float)successfulShots / totalExecutedShots * 100f : 0f;
            return (successfulShots, totalExecutedShots, successRate);
        }

        private void ResetSessionState()
        {
            if (ballSpawner != null)
                ballSpawner.DestroyAllBalls();

            currentSession = null;
            currentShotIndex = 0;
            successfulShots = 0;
            totalExecutedShots = 0;
            isSessionActive = false;
            nextShotTime = 0;
        }

        private void HandleCollision(CollisionEventArgs args)
        {
            if (args.CollisionInfo.Type == CollisionInfo.CollisionType.BallPaddle
                && currentShotIndex > 0
                && currentShotIndex <= currentSession.Shots.Count)
            {
                var shot = currentSession.Shots[currentShotIndex - 1];
                shot.WasSuccessful = true;
                successfulShots++;

                if (removeBalLAfterPaddleHit && args.CollisionInfo.Ball != null)
                    args.CollisionInfo.Ball.AddForce(Vector3.down * ballRemovalForce);
            }
        }

        private void ValidateComponents()
        {
            if (physicsSettings == null)
                Debug.LogError("PhysicsSettings is not assigned!");

            if (tableCollider == null)
                tableCollider = FindObjectOfType<BoxColliderComponent>();

            if (ballLauncher == null)
                ballLauncher = GetComponentInChildren<BallLauncher>();

            if (ballSpawner == null)
                ballSpawner = GetComponentInChildren<BallSpawner>();

            if (physicsDebugger == null)
                physicsDebugger = GetComponentInChildren<PhysicsDebugger>();

            if (paddleStateHandler == null)
                paddleStateHandler = FindObjectOfType<PaddleSetup>();
        }

        private void OnDestroy()
        {
            if (physicsEngine != null)
            {
                if (tableCollider != null)
                    physicsEngine.RemoveBoxCollider(tableCollider);

                if (netCollider != null)
                    physicsEngine.RemoveBoxCollider(netCollider);
            }
        }

        // --- ここから追加 ---
        // OnDrawGizmosで、最後に生成したショットの発射地点とターゲット地点を可視化
        private void OnDrawGizmos()
        {
            if (hasLastShotParameters)
            {
                Gizmos.color = aimLineColor;
                // 発射地点からターゲット地点へライン
                Gizmos.DrawLine(lastLaunchPosition, lastBounceTargetPosition);
                // ターゲット地点に球を表示
                Gizmos.DrawSphere(lastBounceTargetPosition, aimSphereRadius);

                // 発射地点にも小さな球を描いてわかりやすくする
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(lastLaunchPosition, aimSphereRadius * 0.5f);
            }
        }
        // --- ここまで追加 ---
    }
}
