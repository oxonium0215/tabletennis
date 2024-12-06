using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Random = UnityEngine.Random;

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

        [SerializeField] private PaddleStateHandler paddleStateHandler;
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
        public event Action<CollisionEventArgs> OnCollisionOccurred;

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
            // セッションがアクティブな場合は強制終了
            if (isSessionActive)
            {
                StopSession();
                // 少し待ってから新しいセッションを開始
                await Task.Delay(100);
            }

            // 状態のリセット
            ResetSessionState();

            // 新しいセッションの準備
            if (!await PrepareNewSession())
                return;

            isSessionActive = true;
            sessionStartTime = DateTime.Now;
            nextShotTime = Time.time;

            Debug.Log($"Started new session: {currentSession.Id.Value}");

            // セッション開始イベントを発火
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

            // セッション一時停止イベントを発火
            onSessionPause?.Invoke();
        }

        public void ResumeSession()
        {
            if (isSessionActive || currentSession == null) return;
            isSessionActive = true;
            nextShotTime = Time.time;

            // セッション再開イベントを発火
            onSessionResume?.Invoke();
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
            if (paddleStateHandler != null && paddleStateHandler.Paddle != null)
                physicsEngine.AddPaddle(paddleStateHandler.Paddle);

            // モーション記録機能の初期化
            motionRecorder = new MotionRecorder(
                paddleStateHandler,
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
            physicsEngine.OnCollision += args => OnCollisionOccurred?.Invoke(args);
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
            var spinParams = difficultySettings.GetSpinForLevel();
            var courseVariation = difficultySettings.GetCourseVariationForLevel();

            // 目標位置を取得
            var targetPosition = ballLauncher.GetRandomTargetPosition();
            // 発射地点は、BallLauncherの位置かそれより0.6右か左かをそれぞれ3分の1の確率で選択
            var launchPosition = ballLauncher.transform.position +
                                 (Random.value < 1f / 3f ? Vector3.right * 0.6f :
                                     Random.value < 0.5f ? Vector3.left * 0.6f : Vector3.zero);
            return new ShotParameters(
                launchPosition,
                targetPosition,
                speed,
                spinParams.RotationsPerSecond,
                spinParams.SpinAxis
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
            //
            // // 現在残っているボールを削除
            // ballSpawner.DestroyAllBalls();

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

                // 統計情報イベントを発火
                var successRate = totalExecutedShots > 0 ? (float)successfulShots / totalExecutedShots * 100f : 0f;
                onSessionStatistics?.Invoke(successfulShots, totalExecutedShots, successRate);

                // 完了イベントを発火
                onSessionComplete?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save session data: {e.Message}");
            }

            // クリーンアップ処理
            ResetSessionState();
        }

        // セッション統計情報を取得するパブリックメソッド
        public (int successfulShots, int totalShots, float successRate) GetCurrentStatistics()
        {
            var successRate = totalExecutedShots > 0 ? (float)successfulShots / totalExecutedShots * 100f : 0f;
            return (successfulShots, totalExecutedShots, successRate);
        }

        private void ResetSessionState()
        {
            // ボールの削除
            if (ballSpawner != null) ballSpawner.DestroyAllBalls();

            // セッション関連の変数をリセット
            currentSession = null;
            currentShotIndex = 0;
            successfulShots = 0;
            totalExecutedShots = 0;
            isSessionActive = false;
            nextShotTime = 0;

            // 物理エンジンは再作成せず、既存のものを維持
            // 代わりにボールの状態のみクリア
            if (physicsEngine != null)
            {
                var balls = physicsEngine.GetBallStates().ToList();
                foreach (var ball in balls)
                {
                    // ボールのクリーンアップ処理
                }
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

                // ボールを消す機能を設定に応じて実行
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
                paddleStateHandler = FindObjectOfType<PaddleStateHandler>();
        }

        #endregion
    }
}