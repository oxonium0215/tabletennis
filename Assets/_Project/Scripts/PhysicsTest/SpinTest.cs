using StepUpTableTennis.TableTennisEngine;
using StepUpTableTennis.TableTennisEngine.Events;
using UnityEngine;
using ForceMode = StepUpTableTennis.TableTennisEngine.ForceMode;

namespace StepUpTableTennis.PhysicsTest
{
    public class SpinTest : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private PhysicsSimulationManager simulationManager;

        [SerializeField] private Transform ballVisual;

        [Header("Ball Reset Settings")] [SerializeField]
        private Vector3 defaultResetPosition = new(0, 2f, 0);

        [Header("Spin Settings")] [SerializeField]
        private float defaultSpinStrength = 500f;

        [SerializeField] private float spinStrengthAdjustSpeed = 50f;
        [SerializeField] private Vector3 topSpinDirection = new(1, 0, 0);
        [SerializeField] private Vector3 rightSideSpinDirection = new(0, 1, 0);
        [SerializeField] private Vector3 leftSideSpinDirection = new(0, -1, 0);

        [Header("Input Settings")] [SerializeField]
        private KeyCode resetKey = KeyCode.Space;

        [SerializeField] private KeyCode topSpinKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode rightSpinKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode leftSpinKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode decreaseSpinKey = KeyCode.Q;
        [SerializeField] private KeyCode increaseSpinKey = KeyCode.E;

        [Header("Debug Visualization")] [SerializeField]
        private float spinVisualizationScale = 0.5f;

        [SerializeField] private Color spinAxisColor = Color.yellow;
        [SerializeField] private bool showCollisionInfo = true;
        private float currentSpinStrength;
        private PhysicsEngine physicsEngine;

        private void Start()
        {
            ValidateReferences();
            physicsEngine = simulationManager.GetPhysicsEngine();
            physicsEngine.OnCollision += HandleCollision;
            currentSpinStrength = defaultSpinStrength;
        }

        private void Update()
        {
            HandleInput();
            UpdateBallVisual();
        }

        private void OnDestroy()
        {
            if (physicsEngine != null)
                physicsEngine.OnCollision -= HandleCollision;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Spin Strength: {currentSpinStrength:F1}");
            GUILayout.Label("\nControls:");
            GUILayout.Label($"{resetKey}: Reset Ball");
            GUILayout.Label($"{topSpinKey}: Top Spin");
            GUILayout.Label($"{rightSpinKey}: Right Side Spin");
            GUILayout.Label($"{leftSpinKey}: Left Side Spin");
            GUILayout.Label($"{decreaseSpinKey}/{increaseSpinKey}: Adjust Spin Strength");
            GUILayout.EndArea();
        }

        private void ValidateReferences()
        {
            if (simulationManager == null)
                Debug.LogError("PhysicsSimulationManager not assigned!");

            if (ballVisual == null)
                Debug.LogError("BallVisual not assigned!");
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(resetKey)) simulationManager.ResetBall(defaultResetPosition);

            HandleSpinInput();
        }

        private void HandleSpinInput()
        {
            var settings = simulationManager.GetPhysicsSettings();
            var momentOfInertia = 2f / 5f * settings.BallMass * settings.BallRadius * settings.BallRadius;

            if (Input.GetKeyDown(topSpinKey))
                ApplySpinTorque(topSpinDirection);

            if (Input.GetKeyDown(rightSpinKey))
                ApplySpinTorque(rightSideSpinDirection);

            if (Input.GetKeyDown(leftSpinKey))
                ApplySpinTorque(leftSideSpinDirection);

            if (Input.GetKey(decreaseSpinKey))
                currentSpinStrength = Mathf.Max(0, currentSpinStrength - spinStrengthAdjustSpeed * Time.deltaTime);

            if (Input.GetKey(increaseSpinKey))
                currentSpinStrength += spinStrengthAdjustSpeed * Time.deltaTime;
        }

        private void ApplySpinTorque(Vector3 direction)
        {
            var settings = simulationManager.GetPhysicsSettings();
            var momentOfInertia = 2f / 5f * settings.BallMass * settings.BallRadius * settings.BallRadius;
            simulationManager.ApplyTorqueToBall(direction * currentSpinStrength * momentOfInertia, ForceMode.Impulse);
        }

        private void UpdateBallVisual()
        {
            if (ballVisual == null) return;

            var ball = physicsEngine.GetFirstBall();
            if (ball == null) return;

            ballVisual.position = ball.Position;

            if (ball.Spin.sqrMagnitude > 0)
            {
                ballVisual.Rotate(ball.Spin * (Time.deltaTime * Mathf.Rad2Deg));
                Debug.DrawLine(
                    ball.Position,
                    ball.Position + ball.Spin.normalized * spinVisualizationScale,
                    spinAxisColor
                );
            }
        }

        private void HandleCollision(CollisionEventArgs args)
        {
            if (!showCollisionInfo) return;

            var collision = args.CollisionInfo;
            Debug.Log($"Ball collision with {collision.Target.GetType().Name} at {collision.Point}");
            Debug.Log($"Impact Velocity: {collision.RelativeVelocity.magnitude:F2} m/s");
        }
    }
}