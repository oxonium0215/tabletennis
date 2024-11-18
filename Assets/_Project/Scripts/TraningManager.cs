using System;
using StepUpTableTennis.TableTennisEngine;
using StepUpTableTennis.TableTennisEngine.Collisions.Events;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Trajectory;
using UnityEngine;
using Random = System.Random;

namespace StepUpTableTennis
{
    /// <summary>
    ///     トレーニングセッションの管理とボール発射制御を行うクラス
    /// </summary>
    public class TrainingManager : MonoBehaviour
    {
        [SerializeField] private MetaRacketController racketController;

        [Header("Training Settings")] [SerializeField]
        private float shotInterval = 3f;

        [SerializeField] private float preparationTime = 3f;
        [SerializeField] private int maxShots = 10;

        [Header("Target Areas")] [SerializeField]
        private Transform[] targetPoints;

        [SerializeField] private float targetAreaRadius = 0.2f;

        // 内部状態管理
        private BallShooter ballShooter;
        private float nextShotTime;

        [Header("Core References")] [SerializeField]
        private PhysicsSimulationManager physicsManager;

        private Random random;
        private int successfulShots;
        private int totalShots;

        // トレーニング設定
        public TrainingSettings Settings { get; private set; }

        // プロパティ
        public TrainingState CurrentState { get; private set; }
        public float Progress => maxShots > 0 ? (float)totalShots / maxShots : 0f;
        public bool IsRunning => CurrentState == TrainingState.Running;

        private void Update()
        {
            if (CurrentState != TrainingState.Running) return;

            if (Time.time >= nextShotTime)
            {
                if (totalShots >= maxShots)
                {
                    StopTraining();
                    return;
                }

                ShootBall();
                nextShotTime = Time.time + shotInterval;
            }
        }

        private void OnDestroy()
        {
            if (physicsManager?.GetPhysicsEngine() != null)
                physicsManager.GetPhysicsEngine().OnCollision -= HandleCollision;
        }

        // イベント
        public event Action<TrainingResult> OnShotComplete;
        public event Action<TrainingSessionResult> OnSessionComplete;

        public void Initialize(TrainingSettings initialSettings)
        {
            if (!ValidateSetup()) return;

            Settings = initialSettings;
            ballShooter = new BallShooter(physicsManager.GetPhysicsSettings());
            random = new Random();
            CurrentState = TrainingState.Ready;
            ResetStatistics();

            Debug.Log(
                $"[TrainingManager] Initialized with settings - Speed: {Settings.SpeedLevel}, Spin: {Settings.SpinLevel}, Course: {Settings.CourseLevel}");
        }

        public void StartTraining()
        {
            if (!ValidateSetup()) return;

            CurrentState = TrainingState.Running;
            ResetStatistics();
            nextShotTime = Time.time + preparationTime;

            Debug.Log("[TrainingManager] Training started");
        }

        public void PauseTraining()
        {
            if (CurrentState == TrainingState.Running)
            {
                CurrentState = TrainingState.Paused;
                Debug.Log("[TrainingManager] Training paused");
            }
        }

        public void ResumeTraining()
        {
            if (CurrentState == TrainingState.Paused)
            {
                CurrentState = TrainingState.Running;
                nextShotTime = Time.time + preparationTime;
                Debug.Log("[TrainingManager] Training resumed");
            }
        }

        public void StopTraining()
        {
            if (CurrentState == TrainingState.Running || CurrentState == TrainingState.Paused)
            {
                var sessionResult = new TrainingSessionResult
                {
                    TotalShots = totalShots,
                    SuccessfulShots = successfulShots,
                    SuccessRate = totalShots > 0 ? (float)successfulShots / totalShots : 0f,
                    AverageScore = totalShots > 0 ? (float)successfulShots / totalShots * 100 : 0f
                };

                OnSessionComplete?.Invoke(sessionResult);
                CurrentState = TrainingState.Stopped;
                Debug.Log($"[TrainingManager] Training stopped. Success rate: {sessionResult.SuccessRate:P2}");
            }
        }

        private void ShootBall()
        {
            var targetPoint = GetNextTargetPoint();
            var shotParameters = GenerateShotParameters(targetPoint);

            physicsManager.GetPhysicsEngine().OnCollision += HandleCollision;
            ballShooter.ExecuteShot(physicsManager, shotParameters);

            totalShots++;
            Debug.Log($"[TrainingManager] Ball shot {totalShots}/{maxShots}");
        }

        private BallShooter.ShotParameters GenerateShotParameters(Vector3 targetPoint)
        {
            var speed = GetSpeedForLevel(Settings.SpeedLevel);
            var spin = GetSpinForLevel(Settings.SpinLevel);

            return new BallShooter.ShotParameters
            {
                StartPosition = GetBallStartPosition(),
                TargetPosition = targetPoint,
                Speed = speed,
                Spin = spin
            };
        }

        private float GetSpeedForLevel(int level)
        {
            // km/hをm/sに変換
            var baseSpeed = level switch
            {
                1 => UnityEngine.Random.Range(18f, 22f) * (1f / 3.6f),
                2 => UnityEngine.Random.Range(25f, 35f) * (1f / 3.6f),
                3 => UnityEngine.Random.Range(30f, 50f) * (1f / 3.6f),
                4 => UnityEngine.Random.Range(35f, 65f) * (1f / 3.6f),
                5 => UnityEngine.Random.Range(40f, 80f) * (1f / 3.6f),
                _ => 20f * (1f / 3.6f)
            };
            return baseSpeed;
        }

        private Vector3 GetSpinForLevel(int level)
        {
            var spinStrength = level switch
            {
                1 => 0f,
                2 => UnityEngine.Random.Range(50f, 100f),
                3 => UnityEngine.Random.Range(80f, 150f),
                4 => UnityEngine.Random.Range(100f, 200f),
                5 => UnityEngine.Random.Range(150f, 250f),
                _ => 0f
            };

            return level switch
            {
                1 => Vector3.zero,
                2 => Vector3.right * spinStrength, // トップスピンのみ
                3 => UnityEngine.Random.value > 0.5f
                    ? Vector3.right * spinStrength
                    : Vector3.left * spinStrength, // トップ/バック
                4 => GetRandomSpinVector(spinStrength), // 全方向
                5 => GetRandomSpinVector(spinStrength) + GetRandomSpinVector(spinStrength * 0.5f), // 複合スピン
                _ => Vector3.zero
            };
        }

        private Vector3 GetRandomSpinVector(float strength)
        {
            return UnityEngine.Random.onUnitSphere * strength;
        }

        private Vector3 GetNextTargetPoint()
        {
            if (targetPoints == null || targetPoints.Length == 0)
            {
                Debug.LogWarning("[TrainingManager] No target points defined, using default");
                return new Vector3(0, 0.76f, -1.3f);
            }

            return Settings.CourseLevel switch
            {
                1 => targetPoints[0].position, // 固定位置
                2 => targetPoints[UnityEngine.Random.Range(0, 2)].position, // 左右2箇所
                3 => targetPoints[totalShots % 2].position, // 左右交互
                4 => GetRandomPositionInRange(0.5f), // 限定範囲
                5 => GetRandomPositionInRange(1f), // 全面
                _ => targetPoints[0].position
            };
        }

        private Vector3 GetRandomPositionInRange(float range)
        {
            var basePoint = targetPoints[0].position;
            return new Vector3(
                basePoint.x + UnityEngine.Random.Range(-range, range),
                basePoint.y,
                basePoint.z + UnityEngine.Random.Range(-range * 0.5f, range * 0.5f)
            );
        }

        private Vector3 GetBallStartPosition()
        {
            return new Vector3(0, 1f, 1.8f);
        }

        private void HandleCollision(CollisionEventArgs args)
        {
            var collision = args.CollisionInfo;

            if (collision.Target == racketController.GetPaddlePhysics())
            {
                var result = new TrainingResult
                {
                    HitPoint = collision.Point,
                    HitVelocity = collision.RelativeVelocity,
                    IsSuccessful = true
                };

                successfulShots++;
                OnShotComplete?.Invoke(result);

                physicsManager.GetPhysicsEngine().OnCollision -= HandleCollision;

                Debug.Log($"[TrainingManager] Successful hit at {result.HitPoint}");
            }
        }

        private bool ValidateSetup()
        {
            if (physicsManager == null)
            {
                Debug.LogError("[TrainingManager] PhysicsManager not assigned!");
                return false;
            }

            if (racketController == null)
            {
                Debug.LogError("[TrainingManager] RacketController not assigned!");
                return false;
            }

            return true;
        }

        private void ResetStatistics()
        {
            successfulShots = 0;
            totalShots = 0;
        }
    }

    [Serializable]
    public class TrainingSettings
    {
        [Range(1, 5)] public int SpeedLevel = 1;
        [Range(1, 5)] public int SpinLevel = 1;
        [Range(1, 5)] public int CourseLevel = 1;

        public override string ToString()
        {
            return $"Speed: {SpeedLevel}, Spin: {SpinLevel}, Course: {CourseLevel}";
        }
    }

    public enum TrainingState
    {
        Ready,
        Running,
        Paused,
        Stopped
    }

    public class TrainingResult
    {
        public Vector3 HitPoint { get; set; }
        public Vector3 HitVelocity { get; set; }
        public bool IsSuccessful { get; set; }
        public float Score { get; set; }
    }

    public class TrainingSessionResult
    {
        public int TotalShots { get; set; }
        public int SuccessfulShots { get; set; }
        public float SuccessRate { get; set; }
        public float AverageScore { get; set; }
    }
}