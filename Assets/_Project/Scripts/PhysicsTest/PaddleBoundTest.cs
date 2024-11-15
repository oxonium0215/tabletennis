using StepUpTableTennis.TableTennisEngine;
using StepUpTableTennis.TableTennisEngine.Events;
using UnityEngine;
using ForceMode = StepUpTableTennis.TableTennisEngine.ForceMode;

namespace StepUpTableTennis.PhysicsTest
{
    public class PaddleBoundTest : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private PhysicsSimulationManager simulationManager;

        [SerializeField] private Transform paddleVisual;

        [Header("Ball Reset Settings")] [SerializeField]
        private Vector3 ballResetPosition = new(0, 2f, 1f);

        [SerializeField] private Vector3 initialForce = new(0, 1f, -1f);
        [SerializeField] private float forceMagnitude = 0.001f;
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

        [Header("Paddle Movement Settings")] [SerializeField]
        private float moveSpeed = 5f;

        [SerializeField] private float rotateSpeed = 90f;
        [SerializeField] private float verticalMoveSpeed = 5f;

        [Header("Input Settings")] [SerializeField]
        private KeyCode resetKey = KeyCode.Space;

        [SerializeField] private KeyCode moveUpKey = KeyCode.E;
        [SerializeField] private KeyCode moveDownKey = KeyCode.Q;
        [SerializeField] private KeyCode rotateUpKey = KeyCode.R;
        [SerializeField] private KeyCode rotateDownKey = KeyCode.F;
        [Header("Debug")] [SerializeField] private bool showCollisionForce = true;
        private bool isInitialized;
        private Paddle paddle;
        private PhysicsEngine physicsEngine;

        private void Start()
        {
            if (!ValidateReferences()) return;

            if (!simulationManager.IsPhysicsEngineReady)
            {
                Debug.LogError($"[{nameof(PaddleBoundTest)}] PhysicsEngine is not ready!");
                return;
            }

            InitializePhysicsEngine();
        }

        private void Update()
        {
            if (!isInitialized) return;
            HandleInput();
        }

        private void OnEnable()
        {
            if (physicsEngine != null)
                physicsEngine.OnCollision += HandleCollision;
        }

        private void OnDisable()
        {
            if (physicsEngine != null)
                physicsEngine.OnCollision -= HandleCollision;
        }

        private void OnGUI()
        {
            if (!isInitialized) return;

            GUILayout.BeginArea(new Rect(10, 120, 300, 200));
            GUILayout.Label("Controls:");
            GUILayout.Label($"{resetKey}: Reset Ball");
            GUILayout.Label("Arrows: Move Paddle Horizontally");
            GUILayout.Label($"{moveUpKey}/{moveDownKey}: Move Paddle Vertically");
            GUILayout.Label($"{rotateUpKey}/{rotateDownKey}: Rotate Paddle");
            GUILayout.EndArea();
        }

        private void InitializePhysicsEngine()
        {
            physicsEngine = simulationManager.GetPhysicsEngine();

            if (physicsEngine == null)
            {
                Debug.LogError($"[{nameof(PaddleBoundTest)}] Failed to get PhysicsEngine!");
                return;
            }

            InitializePaddle();
        }

        private bool ValidateReferences()
        {
            if (simulationManager == null)
            {
                Debug.LogError($"[{nameof(PaddleBoundTest)}] PhysicsSimulationManager not assigned!", this);
                return false;
            }

            if (paddleVisual == null)
            {
                Debug.LogError($"[{nameof(PaddleBoundTest)}] PaddleVisual not assigned!", this);
                return false;
            }

            return true;
        }

        private void InitializePaddle()
        {
            paddle = new Paddle
            {
                Position = paddleVisual.position,
                Rotation = paddleVisual.rotation
            };

            physicsEngine.AddPaddle(paddle);
            physicsEngine.OnCollision += HandleCollision;
            isInitialized = true;
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(resetKey)) ResetBallWithInitialForce();

            var moveInput = new Vector3(
                Input.GetAxis("Horizontal"),
                Input.GetKey(moveUpKey) ? 1f : Input.GetKey(moveDownKey) ? -1f : 0f,
                Input.GetAxis("Vertical")
            );

            // 水平・垂直移動速度を別々に適用
            moveInput.x *= moveSpeed;
            moveInput.y *= verticalMoveSpeed;
            moveInput.z *= moveSpeed;

            paddle.Position += moveInput * Time.deltaTime;

            if (Input.GetKey(rotateUpKey))
                paddle.Rotation *= Quaternion.Euler(rotateSpeed * Time.deltaTime, 0, 0);
            if (Input.GetKey(rotateDownKey))
                paddle.Rotation *= Quaternion.Euler(-rotateSpeed * Time.deltaTime, 0, 0);

            UpdatePaddleVisual();
        }

        private void ResetBallWithInitialForce()
        {
            simulationManager.ResetBall(new PhysicsSimulationManager.BallResetSettings(
                ballResetPosition,
                Vector3.zero,
                Vector3.zero
            ));
            simulationManager.ApplyForceToBall(initialForce * forceMagnitude, forceMode);
        }

        private void UpdatePaddleVisual()
        {
            if (paddleVisual == null) return;

            paddleVisual.position = paddle.Position;
            paddleVisual.rotation = paddle.Rotation;
        }

        private void HandleCollision(CollisionEventArgs args)
        {
            var collision = args.CollisionInfo;
            if (collision.Target is Paddle && showCollisionForce)
            {
                var force = collision.GetImpactForce(simulationManager.GetPhysicsSettings());
                Debug.Log($"[{nameof(PaddleBoundTest)}] Paddle Hit! Force: {force:F2}N");
            }
        }
    }
}