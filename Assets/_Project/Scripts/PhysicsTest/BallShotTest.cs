using System;
using StepUpTableTennis.TableTennisEngine;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Trajectory;
using UnityEditor;
using UnityEngine;

namespace StepUpTableTennis.PhysicsTest
{
    public class BallShotTest : MonoBehaviour
    {
        [Header("Core References")] [SerializeField]
        private PhysicsSimulationManager simulationManager;

        [SerializeField] private Transform targetMarker;
        [SerializeField] private LineRenderer trajectoryRenderer;

        [Header("Ball Settings")] [SerializeField]
        private Vector3 ballStartPosition = new(0, 1f, 1.8f);

        [SerializeField] private float ballSpeed = 15f;

        [Header("Spin Control")] [SerializeField]
        private SpinControl spinControl = new();

        [Header("Visual Settings")] [SerializeField]
        private float markerMoveSpeed = 2f;

        [SerializeField] private bool showTrajectoryPreview = true;
        private BallShooter ballShooter;

        private void Start()
        {
            if (!ValidateReferences()) return;

            ballShooter = new BallShooter(simulationManager.GetPhysicsSettings());

            if (targetMarker != null) targetMarker.position = new Vector3(0, 0.76f, -1.3f);

            SetupTrajectoryRenderer();
        }

        private void Update()
        {
            HandleTargetMarkerMovement();
            HandleShotInput();
            UpdateTrajectoryPreview();
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));

            GUILayout.Label("Controls:");
            GUILayout.Label("WASD: Move Target Marker");
            GUILayout.Label("QE: Adjust Target Height");
            GUILayout.Label("Space: Shoot Ball");

            GUILayout.Space(10);

            GUILayout.Label("Spin Settings:");
            GUILayout.Label($"Strength: {spinControl.strength:F1} rad/s");
            GUILayout.Label($"RPM: {spinControl.strength * 60 / (2 * Mathf.PI):F1}");
            GUILayout.Label($"Axis: {spinControl.axis:F2}");

            var parameters = CreateShotParameters();
            var error = ballShooter.GetEstimatedError(parameters);
            GUILayout.Label($"Estimated Error: {error:F4}m");

            GUILayout.EndArea();
        }

        private void OnDrawGizmos()
        {
            spinControl.DrawGizmo(ballStartPosition);

            if (targetMarker != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetMarker.position, 0.05f);
            }
        }

        private void OnValidate()
        {
            spinControl.OnValidate();
        }

        private bool ValidateReferences()
        {
            if (simulationManager == null)
            {
                Debug.LogError($"[{nameof(BallShotTest)}] PhysicsSimulationManager is not assigned!");
                return false;
            }

            if (targetMarker == null)
            {
                Debug.LogError($"[{nameof(BallShotTest)}] Target Marker is not assigned!");
                return false;
            }

            return true;
        }

        private void SetupTrajectoryRenderer()
        {
            if (trajectoryRenderer != null)
            {
                trajectoryRenderer.startWidth = 0.02f;
                trajectoryRenderer.endWidth = 0.02f;
                trajectoryRenderer.gameObject.SetActive(showTrajectoryPreview);
            }
        }

        private void HandleTargetMarkerMovement()
        {
            if (targetMarker == null) return;

            var moveInput = new Vector3(
                Input.GetAxis("Horizontal"),
                0,
                Input.GetAxis("Vertical")
            );

            if (Input.GetKey(KeyCode.Q)) moveInput.y = -1;
            if (Input.GetKey(KeyCode.E)) moveInput.y = 1;

            targetMarker.position += moveInput * (markerMoveSpeed * Time.deltaTime);
        }

        private void HandleShotInput()
        {
            if (Input.GetKeyDown(KeyCode.Space)) ExecuteShot();
        }

        private void ExecuteShot()
        {
            var parameters = CreateShotParameters();
            ballShooter.ExecuteShot(simulationManager, parameters);
        }

        private BallShooter.ShotParameters CreateShotParameters()
        {
            return new BallShooter.ShotParameters
            {
                StartPosition = ballStartPosition,
                TargetPosition = targetMarker.position,
                Speed = ballSpeed,
                Spin = spinControl.GetSpinVector()
            };
        }

        private void UpdateTrajectoryPreview()
        {
            if (!showTrajectoryPreview || trajectoryRenderer == null) return;

            var parameters = CreateShotParameters();
            var points = ballShooter.PredictTrajectory(parameters);

            trajectoryRenderer.positionCount = points.Length;
            for (var i = 0; i < points.Length; i++) trajectoryRenderer.SetPosition(i, points[i].position);
        }

        [Serializable]
        public class SpinControl
        {
            [Header("Spin Parameters")] [Tooltip("Direction of the spin axis")]
            public Vector3 axis = Vector3.right;

            [Tooltip("Spin strength in radians per second")]
            public float strength = 100f;

            [Header("Visualization")] public bool visualizeInScene = true;
            public float axisLength = 0.5f;
            public Color axisColor = Color.yellow;

            public Vector3 GetSpinVector()
            {
                return axis.normalized * strength;
            }

            public void OnValidate()
            {
                if (axis != Vector3.zero) axis = axis.normalized;
            }

            public void DrawGizmo(Vector3 position)
            {
                if (!visualizeInScene) return;
                if (strength < float.Epsilon) return;

                var normalizedAxis = axis.normalized;

                // Draw spin axis
                Gizmos.color = axisColor;
                var start = position - normalizedAxis * axisLength * 0.5f;
                var end = position + normalizedAxis * axisLength * 0.5f;
                Gizmos.DrawLine(start, end);

                // Draw rotation direction arrows
                var arrowSize = axisLength * 0.2f;
                var right = Vector3.Cross(normalizedAxis, Vector3.up);
                if (right.magnitude < float.Epsilon)
                    right = Vector3.Cross(normalizedAxis, Vector3.right);

                var up = Vector3.Cross(normalizedAxis, right);
                var arrowCount = 8;
                for (var i = 0; i < arrowCount; i++)
                {
                    var angle = i * (2 * Mathf.PI / arrowCount);
                    var arrowDir = Mathf.Cos(angle) * right + Mathf.Sin(angle) * up;
                    var arrowPos = position + normalizedAxis * (axisLength * 0.25f);
                    Gizmos.DrawRay(arrowPos, arrowDir * arrowSize);
                }

                // Show spin values
#if UNITY_EDITOR
                Handles.Label(end + normalizedAxis * 0.1f,
                    $"{strength:F0} rad/s\n({strength * 60 / (2 * Mathf.PI):F0} rpm)");
#endif
            }
        }
    }
}