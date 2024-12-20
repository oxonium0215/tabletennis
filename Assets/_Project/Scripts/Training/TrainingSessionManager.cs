using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using StepUpTableTennis.DataManagement.Core.Models;
using StepUpTableTennis.DataManagement.Recording;
using StepUpTableTennis.DataManagement.Storage;
using StepUpTableTennis.TableTennisEngine.Collisions.Events;
using StepUpTableTennis.TableTennisEngine.Collisions.System;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Trajectory;
using StepUpTableTennis.TableTennisEngine.Visualization;
using UnityEngine;
using UnityEngine.Events;

namespace StepUpTableTennis.Training
{
    [Serializable]
    public class SessionStatisticsEvent : UnityEvent<int, int, float>
    {
    }

    public class TrainingSessionManager : MonoBehaviour
    {
        [Header("Core Components")] [SerializeField]
        private PhysicsSettings physicsSettings;

        [SerializeField] private BallLauncher ballLauncher;
        [SerializeField] private BallSpawner ballSpawner;
        [SerializeField] private PhysicsDebugger physicsDebugger;

        [Header("Physics Objects")] [SerializeField]
        private TableSetup tableSetup;

        [SerializeField] private PaddleSetup paddleStateHandler;
        [Header("Session Settings")] public DifficultySettings difficultySettings = new();
        [SerializeField] private bool autoStart;
        [Header("Session Settings")] public int shotsPerSession = 10;
        public float shotInterval = 3f;
        public bool removeBalLAfterPaddleHit;
        [SerializeField] public float ballRemovalForce = 1000f;

        [Header("Recording Components")] [SerializeField]
        private Transform headTransform;

        [Header("Events")] public UnityEvent onSessionStart;
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

        private void Start()
        {
            InitializeComponents();
            if (autoStart) StartNewSession();
        }

        private void Update()
        {
            if (!isSessionActive) return;

            physicsEngine.Simulate(Time.deltaTime);
            motionRecorder.UpdateRecording();

            if (Time.time >= nextShotTime && currentShotIndex < currentSession.Shots.Count) ExecuteNextShot();
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

            if (tableSetup != null && tableSetup.Table != null)
                physicsEngine.AddTable(tableSetup.Table);

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
            }

            return shots;
        }

        private ShotParameters GenerateShotParameters(SessionDifficulty difficulty)
        {
            if (ballLauncher == null)
                throw new InvalidOperationException("BallLauncher reference not set!");

            // **1. 発射位置の決定**
            // ボールランチャーの基本位置にオフセットを追加して発射位置を決定
            var launchBase = ballLauncher.transform.position; // ランチャーのワールド座標
            var launchOffset = difficultySettings.GetLaunchOffset();
            var launchPosition = launchBase + launchOffset;

            // **2. バウンス位置の決定**
            // テーブル情報を基にバウンス地点をCourseSettingsから取得
            var table = tableSetup.Table;

            // テーブルのサイズ (width: X方向の幅, length: Z方向の長さ)
            var tableSize = new Vector2(table.Size.x, table.Size.z);

            // ボール半径をPhysicsSettingsから取得
            var ballRadius = physicsSettings.BallRadius;

            // バウンス目標位置を取得
            var bounceTargetPosition = difficultySettings.GetRandomBounceTarget(table.Position, tableSize, ballRadius);

            // **3. 難易度に基づく速度とスピンの取得**
            var speed = difficultySettings.GetSpeedForLevel(); // ボールの速度 (m/s)
            var spin = difficultySettings.GetSpinForLevel(); // スピン情報

            // **4. ShotParametersの生成**
            // バウンス位置をターゲットにし、速度とスピン軸を考慮してパラメータを構築
            var shotParameters = new ShotParameters(
                launchPosition, // 発射位置
                bounceTargetPosition, // バウンス目標位置
                speed, // 初速度 (m/s)
                spin.RotationsPerSecond, // 回転数 (rps)
                spin.SpinAxis // 回転軸
            );

            Debug.Log(
                $"Generated Shot: Launch={launchPosition}, Bounce={bounceTargetPosition}, Speed={speed:F2}m/s, Spin={spin.RotationsPerSecond} rps, Axis={spin.SpinAxis}");

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
            if (ballSpawner != null) ballSpawner.DestroyAllBalls();

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

            if (ballLauncher == null)
                ballLauncher = GetComponentInChildren<BallLauncher>();

            if (ballSpawner == null)
                ballSpawner = GetComponentInChildren<BallSpawner>();

            if (physicsDebugger == null)
                physicsDebugger = GetComponentInChildren<PhysicsDebugger>();

            if (tableSetup == null)
                tableSetup = FindObjectOfType<TableSetup>();

            if (paddleStateHandler == null)
                paddleStateHandler = FindObjectOfType<PaddleSetup>();
        }
    }
}