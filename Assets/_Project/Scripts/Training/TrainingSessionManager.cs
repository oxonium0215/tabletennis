// TrainingSessionManager.cs
using System;
using System.Collections;
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

    // 衝突情報を保存するクラスを追加
    [Serializable]
    public class CollisionRecordData
    {
        public CollisionRecordData(Vector3 position, float impactForce, DateTime timestamp)
        {
            Position = position;
            ImpactForce = impactForce;
            Timestamp = timestamp;
        }

        public Vector3 Position { get; }
        public float ImpactForce { get; }
        public DateTime Timestamp { get; }
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

        // ラケット衝突防止用の新しいデータ構造
        private Dictionary<Ball, float> lastPaddleCollisionTimes = new Dictionary<Ball, float>();
        [Header("Collision Settings")]
        [SerializeField] private float paddleCollisionCooldown = 0.1f; // ラケットへの連続衝突を無視する時間(秒)

        // 衝突位置の記録用
        private Dictionary<int, List<CollisionRecordData>> shotCollisionRecords = new Dictionary<int, List<CollisionRecordData>>();

        // デバッグ用
        [Header("Debug")]
        [SerializeField] private bool logCollisions = true;
        [SerializeField] private bool visualizeNormalizedPositions = true;

        // パドル位置の可視化用
        private List<(Vector2 normalizedPos, float time)> recentPaddleHits = new List<(Vector2, float)>();

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
            
            // 古い衝突記録を削除 (5秒以上経過したものは表示しない)
            if (visualizeNormalizedPositions)
            {
                recentPaddleHits.RemoveAll(hit => Time.time - hit.time > 5f);
            }
        }

        private void OnValidate()
        {
            ValidateComponents();
        }

        private void OnDestroy()
        {
            if (physicsEngine != null)
            {
                // イベント登録解除
                physicsEngine.OnCollision -= HandleCollision;
                physicsEngine.OnCollision -= HandleHapticsOnCollision;
                
                if (tableCollider != null)
                    physicsEngine.RemoveBoxCollider(tableCollider);
                if (netCollider != null)
                    physicsEngine.RemoveBoxCollider(netCollider);
            }
            
            // サッカードイベントの登録解除
            if (saccadeDetector != null)
            {
                saccadeDetector.OnSaccadeStarted -= HandleSaccadeStarted;
                saccadeDetector.OnSaccadeEnded -= HandleSaccadeEnded;
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

            // 衝突位置の可視化（デバッグ用）
            DrawCollisionRecords();
            
            // パドル上の正規化された位置の可視化
            if (visualizeNormalizedPositions && paddleStateHandler != null)
            {
                DrawNormalizedPaddlePositions();
            }
        }

        private void DrawCollisionRecords()
        {
            foreach (var records in shotCollisionRecords.Values)
            {
                foreach (var record in records)
                {
                    Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // オレンジ色
                    Gizmos.DrawSphere(record.Position, 0.01f);
                    
                    // 衝突の強さを可視化
                    float intensity = Mathf.Clamp01(record.ImpactForce / 10f);
                    Gizmos.color = new Color(1f, intensity, 0f, 0.5f);
                    Gizmos.DrawRay(record.Position, Vector3.up * (0.05f + intensity * 0.1f));
                }
            }
        }
        
        private void DrawNormalizedPaddlePositions()
        {
            if (paddleStateHandler == null || paddleStateHandler.Paddle == null)
                return;
                
            var paddle = paddleStateHandler.Paddle;
            
            // パドルの位置と回転
            Vector3 paddlePos = paddle.Position;
            Quaternion paddleRot = paddle.Rotation;
            
            // パドルの大きさ
            float halfWidth = physicsSettings.PaddleSize.x * 0.5f;
            float halfHeight = physicsSettings.PaddleSize.y * 0.5f;
            
            // パドルの描画（半透明の楕円）
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(
                paddlePos, 
                paddleRot, 
                new Vector3(halfWidth * 2f, halfHeight * 2f, 0.01f)
            );
            
            Gizmos.color = new Color(0.2f, 0.2f, 0.8f, 0.3f);
            Gizmos.DrawSphere(Vector3.zero, 0.5f);
            
            // リファレンス線の描画
            Gizmos.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
            Gizmos.DrawLine(new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0)); // X軸
            Gizmos.DrawLine(new Vector3(0, -0.5f, 0), new Vector3(0, 0.5f, 0)); // Y軸
            
            // 最近の衝突位置の描画
            foreach (var hit in recentPaddleHits)
            {
                // 経過時間による透明度の調整（古いほど透明に）
                float age = Time.time - hit.time;
                float alpha = Mathf.Clamp01(1f - (age / 5f));
                
                // 正規化された位置を楕円上の位置に変換
                Vector3 hitPos = new Vector3(hit.normalizedPos.x * 0.5f, hit.normalizedPos.y * 0.5f, 0);
                
                // 衝突点の描画
                Gizmos.color = new Color(1f, 0.3f, 0.3f, alpha);
                Gizmos.DrawSphere(hitPos, 0.05f);
                
                // 点と中心を結ぶ線
                Gizmos.color = new Color(0.8f, 0.8f, 0.2f, alpha * 0.7f);
                Gizmos.DrawLine(Vector3.zero, hitPos);
            }
            
            // 行列を元に戻す
            Gizmos.matrix = oldMatrix;
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

                // 衝突記録を初期化
                shotCollisionRecords.Clear();
                recentPaddleHits.Clear();
                
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

            // 連続衝突検出用のデータをリセット
            collisionCounts.Clear();
            lastPaddleCollisionTimes.Clear();
            shotCollisionRecords.Clear();
            recentPaddleHits.Clear();
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

            // ショットの衝突記録用のリストを初期化
            if (!shotCollisionRecords.ContainsKey(currentShotIndex))
            {
                shotCollisionRecords[currentShotIndex] = new List<CollisionRecordData>();
            }

            ballSpawner.SpawnBall(parameters.LaunchPosition, parameters.InitialVelocity.Value, parameters.InitialAngularVelocity.Value);
            shot.RecordExecution(DateTime.Now, false);
            motionRecorder.SetCurrentShot(currentShotIndex);

            lastLaunchPosition = parameters.LaunchPosition;
            lastBounceTargetPosition = parameters.AimPosition;
            hasLastShotParameters = true;

            currentShotIndex++;
            totalExecutedShots++;
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

            // 重要: 物理エンジンの衝突イベントに HandleCollision を登録
            physicsEngine.OnCollision += HandleCollision;
            
            // ハプティクスイベントも登録
            physicsEngine.OnCollision += HandleHapticsOnCollision;
            
            Debug.Log("Collision handlers registered");
        }

        private void HandleHapticsOnCollision(CollisionEventArgs args)
        {
            if (args.CollisionInfo.Type == CollisionInfo.CollisionType.BallPaddle)
            {
                float impactForce = args.CollisionInfo.GetImpactForce(physicsSettings);
                if (logCollisions) Debug.Log($"Haptics collision occurred: Force={impactForce:F2}N");
                
                float normalizedForce = Mathf.Clamp01(impactForce / 10f);
                float amplitude = normalizedForce;
                float duration = 0.1f + 0.2f * normalizedForce;
                player.amplitude = amplitude;
                player.Play(Controller.Right);
            }
        }

        private void HandleCollision(CollisionEventArgs args)
        {
            var collisionInfo = args.CollisionInfo;
            var ball = collisionInfo.Ball;
            
            if (ball == null) return;
            
            if (logCollisions) Debug.Log($"Collision detected: Type={collisionInfo.Type}, Ball={ball.Id}");

            // ラケットとの衝突処理
            if (collisionInfo.Type == CollisionInfo.CollisionType.BallPaddle)
            {
                // 連続衝突防止: 同じボールのラケットとの前回衝突からの経過時間をチェック
                if (lastPaddleCollisionTimes.TryGetValue(ball, out float lastCollisionTime))
                {
                    float timeSinceLastCollision = Time.time - lastCollisionTime;
                    if (timeSinceLastCollision < paddleCollisionCooldown)
                    {
                        // クールダウン中なのでこの衝突を無視
                        if (logCollisions) Debug.Log($"Ignored paddle collision - too soon after last one ({timeSinceLastCollision:F3}s)");
                        return;
                    }
                }

                // パドルおよび衝突点の情報を取得
                Paddle paddle = collisionInfo.Target as Paddle;
                if (paddle == null)
                {
                    Debug.LogError("Target is not a paddle!");
                    return;
                }

                // 衝突点をパドルローカル座標系に変換
                Vector3 localPos = Quaternion.Inverse(paddle.Rotation) * (collisionInfo.Point - paddle.Position);

                // 楕円面に投影（z座標を0にする）
                Vector2 projectedLocalPos = new Vector2(localPos.x, localPos.y);

                // パドルサイズで正規化（-1~1の範囲に）
                Vector2 normalizedPos = new Vector2(
                    projectedLocalPos.x / (physicsSettings.PaddleSize.x * 0.5f),
                    projectedLocalPos.y / (physicsSettings.PaddleSize.y * 0.5f)
                );
                
                // 正規化された位置が-1から1の範囲に収まるようにクランプ
                normalizedPos = new Vector2(
                    Mathf.Clamp(normalizedPos.x, -1f, 1f),
                    Mathf.Clamp(normalizedPos.y, -1f, 1f)
                );

                if (logCollisions) Debug.Log($"Paddle hit at normalized position: ({normalizedPos.x:F2}, {normalizedPos.y:F2})");

                // 可視化用に記録
                if (visualizeNormalizedPositions)
                {
                    recentPaddleHits.Add((normalizedPos, Time.time));
                }

                // 衝突を記録
                lastPaddleCollisionTimes[ball] = Time.time;

                // 現在のショットのWasSuccessfulフラグを更新
                if (currentShotIndex > 0 && currentShotIndex <= currentSession?.Shots?.Count)
                {
                    var shot = currentSession.Shots[currentShotIndex - 1];
                    shot.WasSuccessful = true;
                    successfulShots++;

                    // 衝突位置と力を記録
                    float impactForce = collisionInfo.GetImpactForce(physicsSettings);
                    var collisionRecord = new CollisionRecordData(
                        collisionInfo.Point,
                        impactForce,
                        DateTime.Now
                    );

                    // ショットに紐づけて記録
                    int shotIndex = currentShotIndex - 1;
                    if (!shotCollisionRecords.ContainsKey(shotIndex))
                    {
                        shotCollisionRecords[shotIndex] = new List<CollisionRecordData>();
                    }
                    shotCollisionRecords[shotIndex].Add(collisionRecord);

                    // TrainingShotの衝突データにも追加
                    float timeOffset = Time.time - currentShotExecutionTime;
                    shot.AddCollisionRecord(
                        collisionInfo.Point, 
                        impactForce, 
                        collisionInfo.Normal,
                        normalizedPos,  // 正規化された位置情報を追加
                        DateTime.Now, 
                        timeOffset
                    );

                    if (logCollisions) Debug.Log($"Recorded paddle collision at {collisionInfo.Point}, force: {impactForce:F2}N, normalized: {normalizedPos}");

                    // ボールの除去設定が有効なら実行
                    if (removeBalLAfterPaddleHit && ball != null)
                        ball.AddForce(Vector3.down * ballRemovalForce);
                }
                
                // 外部イベントも発火
                OnCollision?.Invoke(args);
            }

            // ネットまたはテーブルとの衝突をチェック
            if (collisionInfo.Type == CollisionInfo.CollisionType.BallTable || 
                collisionInfo.Type == CollisionInfo.CollisionType.BallBox)
            {
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
                            
                            // ラケット衝突記録も削除
                            if (lastPaddleCollisionTimes.ContainsKey(ball))
                            {
                                lastPaddleCollisionTimes.Remove(ball);
                            }
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
        
        #region Debug Methods
        
        [ContextMenu("Debug: Force Collision")]
        public void DebugForceCollision()
        {
            if (physicsEngine == null || logCollisions == false) return;
            
            var ball = physicsEngine.GetFirstBall();
            var paddle = physicsEngine.GetFirstPaddle();
            
            if (ball != null && paddle != null)
            {
                var collisionInfo = new CollisionInfo(ball, paddle);
                collisionInfo.Point = paddle.Position + Vector3.up * 0.1f;
                collisionInfo.Normal = Vector3.up;
                collisionInfo.Depth = 0.01f;
                collisionInfo.RelativeVelocity = Vector3.down * 5f;
                collisionInfo.Type = CollisionInfo.CollisionType.BallPaddle;
                
                var args = new CollisionEventArgs(collisionInfo);
                Debug.Log("Forcing collision for debug purposes");
                HandleCollision(args);
            }
            else
            {
                Debug.LogWarning("No ball or paddle found for debug collision");
            }
        }
        
        #endregion
    }
}