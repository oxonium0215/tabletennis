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

namespace StepUpTableTennis.Training
{
    public class TrainingSessionManager : MonoBehaviour
    {
        [Header("Core Components")] [SerializeField]
        private PhysicsSettings physicsSettings;

        [SerializeField] private BallLauncher ballLauncher;
        [SerializeField] private BallSpawner ballSpawner;
        [SerializeField] private PhysicsDebugger physicsDebugger;

        [Header("Physics Objects")] [SerializeField]
        private TableSetup tableSetup;

        [SerializeField] private PaddleSetup paddleSetup;

        [Header("Session Settings")] [SerializeField]
        private DifficultySettings difficultySettings = new();

        [SerializeField] private int shotsPerSession = 10;
        [SerializeField] private float shotInterval = 3f;
        [SerializeField] private bool autoStart;

        [Header("Recording Components")] [SerializeField]
        private Transform headTransform;

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

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
            if (autoStart) StartNewSession();
        }

        private void Update()
        {
            if (!isSessionActive) return;

            // 物理シミュレーションの更新
            physicsEngine.Simulate(Time.deltaTime);

            // モーションデータの記録を更新
            motionRecorder.UpdateRecording();

            // 次のショットの発射チェック
            if (Time.time >= nextShotTime && currentShotIndex < currentSession.Shots.Count) ExecuteNextShot();

            // セッション終了条件のチェック
            CheckSessionCompletion();
        }

        private void OnValidate()
        {
            ValidateComponents();
        }

        #endregion

        #region Session Management

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

                // 各ショットのパラメータを生成
                var shots = await GenerateAndCalculateShots(config);

                currentSession = new TrainingSession(
                    sessionId,
                    DateTime.Now,
                    config,
                    shots
                );

                // セッション開始時にモーション記録を開始
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
                Debug.LogWarning("Cannot start new session while current session is active");
                return;
            }

            if (currentSession == null)
                if (!await PrepareNewSession())
                    return;

            isSessionActive = true;
            sessionStartTime = DateTime.Now;
            nextShotTime = Time.time;

            Debug.Log($"Started session: {currentSession.Id.Value}");
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
        }

        public void ResumeSession()
        {
            if (isSessionActive || currentSession == null) return;
            isSessionActive = true;
            nextShotTime = Time.time;
        }

        #endregion

        #region Private Methods

        private void InitializeComponents()
        {
            // 物理エンジンの初期化
            physicsEngine = new TableTennisPhysics(physicsSettings);

            // テーブルの登録
            if (tableSetup != null && tableSetup.Table != null)
                physicsEngine.AddTable(tableSetup.Table);

            // パドルの登録
            if (paddleSetup != null && paddleSetup.Paddle != null)
                physicsEngine.AddPaddle(paddleSetup.Paddle);

            // モーション記録機能の初期化
            motionRecorder = new MotionRecorder(
                paddleSetup,
                headTransform ?? Camera.main?.transform
            );

            // ボールスポナーの初期化
            if (ballSpawner != null)
                ballSpawner.Initialize(physicsEngine, motionRecorder);

            // 物理デバッガーの初期化
            if (physicsDebugger != null)
                physicsDebugger.Initialize(physicsEngine);

            // データストレージの初期化
            dataStorage = new TrainingDataStorage(
                Path.Combine(Application.persistentDataPath, "TrainingData")
            );

            // コリジョンイベントの登録
            physicsEngine.OnCollision += HandleCollision;
        }

        private async Task<List<TrainingShot>> GenerateAndCalculateShots(SessionConfig config)
        {
            var shots = new List<TrainingShot>();
            var calculator = new BallTrajectoryCalculator(physicsSettings);

            for (var i = 0; i < config.TotalShots; i++)
            {
                // 難易度に基づいてショットパラメータを生成
                var parameters = GenerateShotParameters(config.Difficulty);

                // 軌道を計算
                var calculatedParams = await calculator.CalculateTrajectoryAsync(parameters);

                // TrainingShotを作成
                var shot = new TrainingShot(calculatedParams);
                shots.Add(shot);
            }

            return shots;
        }

        private ShotParameters GenerateShotParameters(SessionDifficulty difficulty)
        {
            if (ballLauncher == null) throw new InvalidOperationException("BallLauncher reference not set!");

            // 難易度に応じた値を取得
            var speed = difficultySettings.GetSpeedForLevel();
            var spin = difficultySettings.GetSpinForLevel();
            var courseVariation = difficultySettings.GetCourseVariationForLevel();

            // 目標位置を取得
            var targetPosition = ballLauncher.GetRandomTargetPosition();

            return new ShotParameters(
                ballLauncher.transform.position,
                targetPosition,
                speed,
                spin,
                Vector3.up
            );
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

            // BallSpawnerを使用してボールを生成・発射
            ballSpawner.SpawnBall(
                parameters.LaunchPosition,
                parameters.InitialVelocity.Value,
                parameters.InitialAngularVelocity.Value
            );

            // 発射時刻を記録
            shot.RecordExecution(DateTime.Now, false);

            // 現在のショットインデックスを記録システムに通知
            motionRecorder.SetCurrentShot(currentShotIndex);

            currentShotIndex++;
            nextShotTime = Time.time + shotInterval;
            totalExecutedShots++;
        }

        private void CheckSessionCompletion()
        {
            // 最後のショットの発射と物理シミュレーションの余裕を持たせる
            if (currentShotIndex >= currentSession.Shots.Count && Time.time > nextShotTime + 1.0f)
            {
                Debug.Log($"Session completed. Total shots: {totalExecutedShots}, Successful: {successfulShots}");
                StopSession();
            }
        }

        private async void CompleteSession()
        {
            if (currentSession == null) return;

            // セッション終了時に記録を停止
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
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save session data: {e.Message}");
            }
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

            if (paddleSetup == null)
                paddleSetup = FindObjectOfType<PaddleSetup>();
        }

        #endregion
    }
}