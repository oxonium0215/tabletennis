using System;
using StepUpTableTennis.TableTennisEngine;
using StepUpTableTennis.TableTennisEngine.Collisions.Events;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;
using ForceMode = StepUpTableTennis.TableTennisEngine.Objects.ForceMode;

namespace StepUpTableTennis.PhysicsTest
{
    public class SimpleTableBoundTest : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private PhysicsSimulationManager simulationManager;

        [Header("Force Settings")] [SerializeField]
        private Vector3 upwardForce = Vector3.up * 0.01f; // 調整: 0.01Nの力

        [SerializeField] private Vector3 torqueForce = new(0.001f, 0, 0); // 調整: 0.001N・mのトルク
        [SerializeField] private ForceMode forceMode = ForceMode.Impulse;

        [Header("Input Settings")] [SerializeField]
        private KeyCode resetKey = KeyCode.Space;

        [SerializeField] private KeyCode upwardForceKey = KeyCode.R;
        [SerializeField] private KeyCode torqueKey = KeyCode.F;
        [SerializeField] private KeyCode nextPresetKey = KeyCode.Tab;
        [SerializeField] private KeyCode previousPresetKey = KeyCode.LeftShift;

        [Header("Debug Settings")] [SerializeField]
        private bool showDebugInfo = true;

        [SerializeField] private bool showCollisionInfo = true;
        [SerializeField] private bool showTrajectoryPreview = true;
        [SerializeField] private Color trajectoryColor = Color.yellow;
        [SerializeField] private float trajectoryDuration = 2f;

        [Header("Ball Reset Presets")] [SerializeField]
        private BallPreset[] presets;

        private int currentPresetIndex;
        private PhysicsEngine physicsEngine;
        private LineRenderer trajectoryRenderer;

        private void Start()
        {
            InitializeComponents();
            currentPresetIndex = 0;

            // デフォルトのプリセットを現実的な値に調整
            if (presets == null || presets.Length == 0)
                presets = new[]
                {
                    new BallPreset
                    {
                        name = "High Drop",
                        position = new Vector3(0, 2f, 0),
                        description = "Simple drop test from 2m height"
                    },
                    new BallPreset
                    {
                        name = "Gentle Shot",
                        position = new Vector3(-1f, 1f, 1f),
                        initialVelocity = new Vector3(1f, 0.5f, -1f), // 適度な初速
                        description = "Gentle diagonal shot across the table"
                    },
                    new BallPreset
                    {
                        name = "Top Spin",
                        position = new Vector3(0, 1f, 1f),
                        initialVelocity = new Vector3(0, 0, -2f), // 前方への適度な速度
                        initialSpin = new Vector3(50f, 0, 0), // 適度な回転
                        description = "Forward shot with top spin"
                    },
                    new BallPreset
                    {
                        name = "Side Spin",
                        position = new Vector3(-0.5f, 1f, 0.5f),
                        initialVelocity = new Vector3(0.5f, 0, -1f),
                        initialSpin = new Vector3(0, 30f, 0), // 横回転
                        description = "Shot with side spin"
                    }
                };
        }

        private void Update()
        {
            if (!ValidateComponents()) return;

            HandleInput();
            UpdateTrajectoryPreview();
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
            if (!showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));

            // 現在のプリセット情報
            var preset = presets[currentPresetIndex];
            GUILayout.Label($"Current Preset: {preset.name} ({currentPresetIndex + 1}/{presets.Length})");
            GUILayout.Label(preset.description);

            GUILayout.Space(10);

            // コントロール説明
            GUILayout.Label("Controls:");
            GUILayout.Label($"{resetKey}: Reset Ball");
            GUILayout.Label($"{upwardForceKey}: Apply Upward Force");
            GUILayout.Label($"{torqueKey}: Apply Torque");
            GUILayout.Label($"{previousPresetKey}+{nextPresetKey}: Change Preset");

            GUILayout.EndArea();
        }

        private void InitializeComponents()
        {
            if (simulationManager == null)
            {
                Debug.LogError("PhysicsSimulationManager not assigned!");
                return;
            }

            physicsEngine = simulationManager.GetPhysicsEngine();
            physicsEngine.OnCollision += HandleCollision;

            if (showTrajectoryPreview)
                InitializeTrajectoryRenderer();
        }

        private void InitializeTrajectoryRenderer()
        {
            trajectoryRenderer = gameObject.AddComponent<LineRenderer>();
            trajectoryRenderer.startWidth = 0.02f;
            trajectoryRenderer.endWidth = 0.02f;
            trajectoryRenderer.material = new Material(Shader.Find("Sprites/Default"));
            trajectoryRenderer.startColor = trajectoryColor;
            trajectoryRenderer.endColor = new Color(trajectoryColor.r, trajectoryColor.g, trajectoryColor.b, 0.2f);
        }

        private bool ValidateComponents()
        {
            if (simulationManager == null || physicsEngine == null)
            {
                Debug.LogError("Required components are missing!");
                return false;
            }

            return true;
        }

        private void HandleInput()
        {
            // プリセット切り替え
            if (Input.GetKey(previousPresetKey))
            {
                if (Input.GetKeyDown(nextPresetKey))
                    CyclePreviousPreset();
            }
            else if (Input.GetKeyDown(nextPresetKey))
            {
                CycleNextPreset();
            }

            // ボールのリセットと力の適用
            if (Input.GetKeyDown(resetKey)) ResetBallWithCurrentPreset();

            if (Input.GetKeyDown(upwardForceKey))
            {
                simulationManager.ApplyForceToBall(upwardForce, forceMode);
                if (showDebugInfo)
                    Debug.Log($"Applied upward force: {upwardForce}N");
            }

            if (Input.GetKeyDown(torqueKey))
            {
                simulationManager.ApplyTorqueToBall(torqueForce, forceMode);
                if (showDebugInfo)
                    Debug.Log($"Applied torque: {torqueForce}N·m");
            }
        }

        private void CycleNextPreset()
        {
            currentPresetIndex = (currentPresetIndex + 1) % presets.Length;
            if (showDebugInfo)
                Debug.Log($"Switched to preset: {presets[currentPresetIndex].name}");
        }

        private void CyclePreviousPreset()
        {
            currentPresetIndex = (currentPresetIndex - 1 + presets.Length) % presets.Length;
            if (showDebugInfo)
                Debug.Log($"Switched to preset: {presets[currentPresetIndex].name}");
        }

        private void ResetBallWithCurrentPreset()
        {
            var preset = presets[currentPresetIndex];
            simulationManager.ResetBall(new PhysicsSimulationManager.BallResetSettings(
                preset.position,
                preset.initialVelocity,
                preset.initialSpin
            ));

            if (showDebugInfo)
                Debug.Log($"Reset ball using preset: {preset.name}");
        }

        private void UpdateTrajectoryPreview()
        {
            if (!showTrajectoryPreview || trajectoryRenderer == null) return;

            var ball = physicsEngine.GetFirstBall();
            if (ball == null) return;

            var positions = physicsEngine.PredictTrajectory(ball, trajectoryDuration);
            trajectoryRenderer.positionCount = positions.Count;
            trajectoryRenderer.SetPositions(positions.ToArray());
        }

        private void HandleCollision(CollisionEventArgs args)
        {
            if (!showCollisionInfo) return;

            var collision = args.CollisionInfo;
            if (collision.Target is Table)
            {
                var force = collision.GetImpactForce(simulationManager.GetPhysicsSettings());
                Debug.Log($"Table collision! Impact force: {force:F2}N");
                Debug.Log($"Collision point: {collision.Point}, Normal: {collision.Normal}");
            }
        }

        [Serializable]
        public class BallPreset
        {
            public string name = "Preset";
            public Vector3 position = new(0, 2f, 0);
            public Vector3 initialVelocity = Vector3.zero;
            public Vector3 initialSpin = Vector3.zero;
            [Multiline] public string description = "Preset description";
        }
    }
}