// TrainingSessionManager.cs
using System;
using System.Collections;
//using System.Collections; // 不要なので削除
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
using StepUpTableTennis.TableTennisEngine.Objects;

namespace StepUpTableTennis.Training
{
    [Serializable]
    public class SessionStatisticsEvent : UnityEvent<int, int, float>
    {
    }

    public class TrainingSessionManager : MonoBehaviour
    {
        #region Fields and Components

        [Header("Core Components")]
        [SerializeField] private PhysicsSettings physicsSettings;
        [SerializeField] private BallLauncher ballLauncher;
        [SerializeField] private BallSpawner ballSpawner;
        [SerializeField] private PhysicsDebugger physicsDebugger;

        [Header("Physics Objects")]
        [SerializeField] private BoxColliderComponent tableCollider;
        [SerializeField] private BoxColliderComponent netCollider;
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
        [SerializeField] private OVREyeGaze eyeGaze;
        [SerializeField] private OVRFaceExpressions faceExpressions;

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
        private TableTennisPhysics physicsEngine;
        private DateTime sessionStartTime;
        private int successfulShots;
        private int totalExecutedShots;
        public HapticClip clip;
        private HapticClipPlayer player;

        // サッカード関連
        [SerializeField] private SaccadeDetector saccadeDetector;
        private MeshRenderer currentBallRenderer;
        [SerializeField] private float saccadeHideTime = 0.1f; // 100ms間非表示

        // 各ショットにつき１回だけ処理を実行するためのフラグ
        private float currentShotExecutionTime;
        private bool eligibleForBallHide = false;
        private bool ballHiddenForCurrentShot = false;

        // ショットパラメータの可視化用
        private Vector3 lastLaunchPosition;
        private Vector3 lastBounceTargetPosition;
        private bool hasLastShotParameters;
        public Color aimLineColor = Color.magenta;
        public float aimSphereRadius = 0.05f;

        // 連続衝突検出用
        private Dictionary<int, (int count, float lastCollisionTime)> collisionCounts = new();
        public float continuousCollisionThreshold = 0.5f; // 連続衝突とみなす時間閾値(秒)
        public int requiredCollisionCount = 2; // ボールを消すために必要な衝突回数

        #endregion

        #region Properties

        // 次のショット発射タイミング
        public float NextShotTime { get; private set; }

        public Vector3 GetNextShotPosition()
        {
            return CurrentShot?.Parameters?.LaunchPosition ?? Vector3.zero;
        }

        public int GetCurrentShotIndex() => currentShotIndex;

        public TrainingShot CurrentShot =>
            currentShotIndex >= 0 && currentShotIndex < currentSession.Shots.Count
                ? currentSession.Shots[currentShotIndex]
                : null;

        public TrainingShot JustFiredShot
        {
            get
            {
                int index = currentShotIndex - 1;
                return (index >= 0 && index < currentSession.Shots.Count) ? currentSession.Shots[index] : null;
            }
        }

        #endregion

        #region Unity Methods

        private void Start()
        {
            player = new HapticClipPlayer(clip);
            InitializeComponents();

            // サッカードディテクターの取得とイベント登録
            if (saccadeDetector == null)
            {
                saccadeDetector = GetComponent<SaccadeDetector>() ?? gameObject.AddComponent<SaccadeDetector>();
            }
            saccadeDetector.OnSaccadeStarted += HandleSaccadeStarted;
            saccadeDetector.OnSaccadeEnded   += HandleSaccadeEnded;

            var courseSettings = new CourseSettings(tableCollider, ballLauncher.transform, physicsSettings.BallRadius);
            difficultySettings.Initialize(courseSettings);

            if (autoStart)
            {
                StartNewSession();
            }
        }

        private void Update()
        {
            if (!isSessionActive) return;

            physicsEngine.Simulate(Time.deltaTime);
            motionRecorder.UpdateRecording();

            // 次のショット発射タイミングになったら実行
            if (Time.time >= NextShotTime && currentShotIndex < currentSession.Shots.Count)
            {
                ExecuteNextShot();
            }
            CheckSessionCompletion();
        }

        private void OnValidate()
        {
            ValidateComponents();
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

        private void OnDrawGizmos()
        {
            if (hasLastShotParameters)
            {
                Gizmos.color = aimLineColor;
                Gizmos.DrawLine(lastLaunchPosition, lastBounceTargetPosition);
                Gizmos.DrawSphere(lastBounceTargetPosition, aimSphereRadius);
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(lastLaunchPosition, aimSphereRadius * 0.5f);
            }
        }

        #endregion

        #region Saccade Handling

        /// <summary>
        /// サッカード開始時のハンドラー。
        /// ショット開始から80ms以上経過していれば、ボール非表示の対象としてマークする。
        /// </summary>
        private void HandleSaccadeStarted()
        {
            if (Time.time - currentShotExecutionTime >= 0.08f)
            {
                eligibleForBallHide = true;
            }
        }

        /// <summary>
        /// サッカード終了時のハンドラー。
        /// eligibleForBallHide が true で、かつそのショットではまだ処理を行っていなければ、ボールを100ms非表示にする。
        /// </summary>
        private void HandleSaccadeEnded()
        {
            if (!eligibleForBallHide || ballHiddenForCurrentShot)
                return;

            var ballStateManager = FindFirstObjectByType<BallStateManager>();
            if (ballStateManager != null)
            {
                currentBallRenderer = ballStateManager.gameObject.GetComponent<MeshRenderer>();
                if (currentBallRenderer != null)
                {
                    ballHiddenForCurrentShot = true;
                    StartCoroutine(HideBallTemporarily());
                }
            }
        }

        // ボールを消す確率
        public float hideBallProbability = 0.5f;

        /// <summary>
        /// ボールのレンダラーを無効化して100ms後に再度有効化するコルーチン。
        /// </summary>
        private System.Collections.IEnumerator HideBallTemporarily()
        {
            if (currentBallRenderer == null)
                yield break;

            if (hideBallProbability < UnityEngine.Random.value)
                yield break;
            currentBallRenderer.enabled = false; // 非表示
            yield return new WaitForSeconds(0.1f); // 100ms待機
            if (currentBallRenderer != null)
                currentBallRenderer.enabled = true; // 表示に戻す
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
                    new SessionDifficulty(difficultySettings.SpeedLevel, difficultySettings.SpinLevel, difficultySettings.CourseLevel),
                    shotsPerSession,
                    shotInterval
                );

                var shots = await GenerateAndCalculateShots(config);

                currentSession = new TrainingSession(sessionId, DateTime.Now, config, shots);

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
            NextShotTime = Time.time; // 最初のショットはすぐに発射

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
            NextShotTime = float.MaxValue; // 一時停止中は発射しない
            onSessionPause?.Invoke();
        }

        public void ResumeSession()
        {
            if (isSessionActive || currentSession == null) return;
            isSessionActive = true;
            NextShotTime = Time.time; // 再開時に次のショットタイミングをリセット
            onSessionResume?.Invoke();
        }

        /// <summary>
        /// セッションが完了しているかチェックし、完了していればセッションを終了する。
        /// </summary>
        private void CheckSessionCompletion()
        {
            if (currentSession != null && currentShotIndex >= currentSession.Shots.Count && Time.time > NextShotTime + 1f)
            {
                Debug.Log($"Session completed. Total shots: {totalExecutedShots}, Successful: {successfulShots}");
                StopSession();
            }
        }

        private async void CompleteSession()
        {
            if (currentSession == null) return;

            motionRecorder.StopSession();

            var statistics = new SessionStatistics(totalExecutedShots, successfulShots, DateTime.Now);
            currentSession.Complete(statistics);

            try
            {
                await dataStorage.SaveSessionAsync(currentSession);
                Debug.Log($"Session data saved successfully: {currentSession.Id.Value}");

                float successRate = totalExecutedShots > 0 ? (float)successfulShots / totalExecutedShots * 100f : 0f;
                onSessionStatistics?.Invoke(successfulShots, totalExecutedShots, successRate);
                onSessionComplete?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save session data: {e.Message}");
            }

            ResetSessionState();
        }

        private void ResetSessionState()
        {
            ballSpawner?.DestroyAllBalls();
            currentSession = null;
            currentShotIndex = 0;
            successfulShots = 0;
            totalExecutedShots = 0;
            isSessionActive = false;
            NextShotTime = float.MaxValue;

            // 連続衝突検出用のデータもリセット
            collisionCounts.Clear();
        }

        public (int successfulShots, int totalShots, float successRate) GetCurrentStatistics()
        {
            float successRate = totalExecutedShots > 0 ? (float)successfulShots / totalExecutedShots * 100f : 0f;
            return (successfulShots, totalExecutedShots, successRate);
        }

        #endregion

        #region Shot Execution

        private async Task<List<TrainingShot>> GenerateAndCalculateShots(SessionConfig config)
        {
            var shots = new List<TrainingShot>();
            var calculator = new BallTrajectoryCalculator(physicsSettings);

            for (int i = 0; i < config.TotalShots; i++)
            {
                var parameters = GenerateShotParameters(config.Difficulty);
                var calculatedParams = await calculator.CalculateTrajectoryAsync(parameters);
                var shot = new TrainingShot(calculatedParams);
                shots.Add(shot);

                lastLaunchPosition = calculatedParams.LaunchPosition;
                lastBounceTargetPosition = calculatedParams.AimPosition;
                hasLastShotParameters = true;
            }

            return shots;
        }

        private ShotParameters GenerateShotParameters(SessionDifficulty difficulty)
        {
            if (ballLauncher == null)
                throw new InvalidOperationException("BallLauncher reference not set!");

            Vector3 launchPosition = difficultySettings.GetLaunchPosition();
            Vector3 bounceTargetPosition = difficultySettings.GetRandomBouncePosition();

            float speed = difficultySettings.GetSpeedForLevel();
            var spin = difficultySettings.GetSpinForLevel();

            ShotParameters shotParameters = new ShotParameters(
                launchPosition,
                bounceTargetPosition,
                speed,
                spin.RotationsPerSecond,
                spin.SpinAxis
            );

            Debug.Log($"Generated Shot: Launch={launchPosition}, Bounce={bounceTargetPosition}, Speed={speed:F2}m/s, Spin={spin.RotationsPerSecond} rps, Axis={spin.SpinAxis}");
            return shotParameters;
        }

        private void ExecuteNextShot()
        {
            if (currentShotIndex >= currentSession.Shots.Count)
                return;

            // 新しいショット開始時に、各種フラグをリセットし、発射時刻を記録
            ballHiddenForCurrentShot = false;
            eligibleForBallHide = false;
            currentBallRenderer = null;
            currentShotExecutionTime = Time.time;

            var shot = currentSession.Shots[currentShotIndex];
            var parameters = shot.Parameters;

            if (!parameters.IsCalculated)
            {
                Debug.LogError("Shot parameters have not been calculated");
                return;
            }

            ballSpawner.SpawnBall(parameters.LaunchPosition, parameters.InitialVelocity.Value, parameters.InitialAngularVelocity.Value);
            shot.RecordExecution(DateTime.Now, false);
            motionRecorder.SetCurrentShot(currentShotIndex);

            lastLaunchPosition = parameters.LaunchPosition;
            lastBounceTargetPosition = parameters.AimPosition;
            hasLastShotParameters = true;

            currentShotIndex++;
            NextShotTime = Time.time + shotInterval;
        }

        #endregion

        #region Collision and Haptics Handling

        public event Action<CollisionEventArgs> OnCollision;

        private void InitializeComponents()
        {
            physicsEngine = new TableTennisPhysics(physicsSettings);

            if (tableCollider != null)
                physicsEngine.AddBoxCollider(tableCollider);

            if (netCollider != null)
                physicsEngine.AddBoxCollider(netCollider);

            if (paddleStateHandler != null && paddleStateHandler.Paddle != null)
                physicsEngine.AddPaddle(paddleStateHandler.Paddle);

            // MotionRecorder の初期化（eyeGaze, faceExpressions, saccadeDetector を渡す）
            motionRecorder = new MotionRecorder(
                paddleStateHandler, 
                headTransform ?? Camera.main?.transform,
                eyeGaze,
                faceExpressions,
                saccadeDetector
            );

            ballSpawner?.Initialize(physicsEngine, motionRecorder);
            physicsDebugger?.Initialize(physicsEngine);

            dataStorage = new TrainingDataStorage(Path.Combine(Application.persistentDataPath, "TrainingData"));
        }

        private void HandleHapticsOnCollision(CollisionEventArgs args)
        {
            if (args.CollisionInfo.Type == CollisionInfo.CollisionType.BallPaddle)
            {
                float impactForce = args.CollisionInfo.GetImpactForce(physicsSettings);
                Debug.Log("Collision occurred: " + impactForce);
                float normalizedForce = Mathf.Clamp01(impactForce / 10f);
                float amplitude = normalizedForce;
                float duration = 0.1f + 0.2f * normalizedForce;
                player.amplitude = amplitude;
                player.Play(Controller.Right);
            }
        }

        private void HandleCollision(CollisionEventArgs args)
        {
            if (args.CollisionInfo.Type == CollisionInfo.CollisionType.BallPaddle &&
                currentShotIndex > 0 && currentShotIndex <= currentSession.Shots.Count)
            {
                var shot = currentSession.Shots[currentShotIndex - 1];
                shot.WasSuccessful = true;
                successfulShots++;

                if (removeBalLAfterPaddleHit && args.CollisionInfo.Ball != null)
                    args.CollisionInfo.Ball.AddForce(Vector3.down * ballRemovalForce);
            }

            // ネットまたはテーブルとの衝突をチェック
            if (args.CollisionInfo.Type == CollisionInfo.CollisionType.BallTable || args.CollisionInfo.Type == CollisionInfo.CollisionType.BallBox)
            {
                Ball ball = args.CollisionInfo.Ball;
                float currentTime = Time.time;

                if (collisionCounts.ContainsKey(ball.Id))
                {
                    // 既存のエントリがある場合
                    (int count, float lastCollisionTime) = collisionCounts[ball.Id];

                    if (currentTime - lastCollisionTime <= continuousCollisionThreshold)
                    {
                        // 連続衝突とみなす
                        count++;
                        if (count >= requiredCollisionCount)
                        {
                            // ボールを消去: DestroyBall を使用
                            Debug.Log("Continuous collision detected. Removing ball.");
                            ballSpawner.DestroyBall(ball);
                            collisionCounts.Remove(ball.Id); // エントリを削除
                        }
                        else
                        {
                            // カウントを更新
                            collisionCounts[ball.Id] = (count, currentTime);
                        }
                    }
                    else
                    {
                        // 時間閾値を超えた場合はリセット
                        collisionCounts[ball.Id] = (1, currentTime);
                    }
                }
                else
                {
                    // 新しいエントリを追加
                    collisionCounts.Add(ball.Id, (1, currentTime));
                }
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

            // 追加: eyeGazeのバリデーション
            if (eyeGaze == null)
                eyeGaze = FindObjectOfType<OVREyeGaze>();

            // 追加: faceExpressionsのバリデーション
            if (faceExpressions == null)
                faceExpressions = FindObjectOfType<OVRFaceExpressions>();

            if (eyeGaze == null)
                Debug.LogWarning("OVREyeGaze component not found. Eye tracking will be disabled.");

            if (faceExpressions == null)
                Debug.LogWarning("OVRFaceExpressions component not found. Eye closure tracking will be disabled.");
        }

        #endregion
    }
}
