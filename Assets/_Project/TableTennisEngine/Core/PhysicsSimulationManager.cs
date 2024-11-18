using System;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Visualization;
using UnityEngine;
using ForceMode = StepUpTableTennis.TableTennisEngine.Objects.ForceMode;

namespace StepUpTableTennis.TableTennisEngine.Core
{
    public class PhysicsSimulationManager : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private PhysicsSettings physicsSettings;

        [Header("Ball Settings")] [SerializeField]
        private Vector3 defaultSpawnPosition = new(0, 2f, 0);

        [SerializeField] private Vector3 defaultInitialVelocity = Vector3.zero;
        [SerializeField] private Vector3 defaultInitialSpin = Vector3.zero;
        [Header("Prefabs")] [SerializeField] private GameObject ballPrefab;

        [Header("References")] [SerializeField]
        private PhysicsDebugger debugger;

        [Header("Debug")] [SerializeField] private bool showDebugInfo = true;
        private Ball ball;
        private GameObject ballInstance;
        private BallVisualizer ballVisualizer;
        private PhysicsEngine physicsEngine;
        public bool IsPhysicsEngineReady => physicsEngine != null;

        private void Awake()
        {
            InitCore();
        }

        private void Start()
        {
            SpawnBall();
            InitializeTable();
        }

        private void FixedUpdate()
        {
            if (!ValidatePhysicsEngine()) return;

            physicsEngine.Simulate(Time.fixedDeltaTime);
            LogDebugInfo();
        }

        private void OnGUI()
        {
            if (!showDebugInfo || ball == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"Ball Position: {ball.Position}");
            GUILayout.Label($"Ball Velocity: {ball.Velocity}");
            GUILayout.Label($"Ball Spin: {ball.Spin}");
            GUILayout.EndArea();
        }

        private void InitCore()
        {
            if (physicsEngine != null) return;

            ValidateSettings();
            InitializePhysicsEngine();
        }

        private void InitializePhysicsEngine()
        {
            physicsEngine = new PhysicsEngine(physicsSettings);
            debugger?.Initialize(physicsEngine);
        }

        public PhysicsEngine GetPhysicsEngine()
        {
            if (!IsPhysicsEngineReady) return null;
            return physicsEngine;
        }

        public PhysicsSettings GetPhysicsSettings()
        {
            return physicsSettings;
        }

        private void ValidateSettings()
        {
            if (physicsSettings == null)
                Debug.LogError("PhysicsSettings not assigned!");

            if (ballPrefab == null)
                Debug.LogError("Ball prefab not assigned!");
        }

        private void SpawnBall()
        {
            if (ballInstance != null) Destroy(ballInstance);

            ballInstance = Instantiate(ballPrefab, defaultSpawnPosition, Quaternion.identity);
            SetupBallVisualizer();
            InitializeBallPhysics(new BallResetSettings(defaultSpawnPosition, defaultInitialVelocity,
                defaultInitialSpin));
        }

        private void SetupBallVisualizer()
        {
            ballVisualizer = ballInstance.GetComponent<BallVisualizer>();
            if (ballVisualizer == null)
                ballVisualizer = ballInstance.AddComponent<BallVisualizer>();
        }

        private void InitializeBallPhysics(BallResetSettings settings)
        {
            ball = new Ball();
            ball.Initialize(physicsSettings);
            ball.ResetState(settings.position, settings.velocity, settings.spin);

            physicsEngine.AddBall(ball);
            ballVisualizer.Initialize(ball);
        }

        private void InitializeTable()
        {
            var table = new Table
            {
                Position = new Vector3(0, 0.728f, 0),
                Rotation = Quaternion.identity,
                Size = physicsSettings.TableSize
            };
            physicsEngine.AddTable(table);
        }

        private bool ValidatePhysicsEngine()
        {
            return physicsEngine != null;
        }

        private void LogDebugInfo()
        {
            if (!showDebugInfo || ball == null) return;

            Debug.Log($"Ball State - Position: {ball.Position}, " +
                      $"Velocity: {ball.Velocity}, " +
                      $"Time: {Time.time}");
        }

        public void ResetBall()
        {
            ResetBall(new BallResetSettings(defaultSpawnPosition, defaultInitialVelocity, defaultInitialSpin));
        }

        public void ResetBall(Vector3 resetPosition)
        {
            ResetBall(new BallResetSettings(resetPosition, defaultInitialVelocity, defaultInitialSpin));
        }

        public void ResetBall(BallResetSettings settings)
        {
            if (ball != null)
                ball.ResetState(settings.position, settings.velocity, settings.spin);
            else
                Debug.LogError("Cannot reset ball - ball instance is null");
        }

        public void ApplyForceToBall(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            if (ball != null)
                ball.AddForce(force, mode);
            else
                Debug.LogError("Cannot apply force - ball instance is null");
        }

        public void ApplyTorqueToBall(Vector3 torque, ForceMode mode = ForceMode.Force)
        {
            if (ball != null)
                ball.AddTorque(torque, mode);
            else
                Debug.LogError("Cannot apply torque - ball instance is null");
        }

        [Serializable]
        public class BallResetSettings
        {
            public Vector3 position;
            public Vector3 velocity;
            public Vector3 spin;

            public BallResetSettings(Vector3 pos, Vector3 vel, Vector3 spn)
            {
                position = pos;
                velocity = vel;
                spin = spn;
            }
        }
    }
}