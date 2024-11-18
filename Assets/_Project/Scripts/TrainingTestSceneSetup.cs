using StepUpTableTennis.TableTennisEngine;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis
{
    /// <summary>
    ///     トレーニングテストシーンの初期化と管理を行うクラス
    /// </summary>
    public class TrainingTestSceneSetup : MonoBehaviour
    {
        [SerializeField] private TrainingManager trainingManager;
        [SerializeField] private MetaRacketController racketController;

        [Header("Training Settings")] [SerializeField]
        private TrainingSettings initialSettings;

        [SerializeField] private Transform[] targetPositions;

        [Header("Environment Settings")] [SerializeField]
        private Vector3 tablePosition = new(0, 0.728f, 0);

        [SerializeField] private Vector3 defaultBallPosition = new(0, 2f, 0);

        [Header("Debug Visualization")] [SerializeField]
        private bool showTargetMarkers = true;

        [SerializeField] private float markerRadius = 0.1f;
        [SerializeField] private Color markerColor = Color.yellow;
        [SerializeField] private bool showDebugGUI = true;

        [Header("Debug UI Settings")] [SerializeField]
        private Vector2 guiPosition = new(10, 10);

        [SerializeField] private Vector2 guiSize = new(200, 300);

        [Header("Scene Components")] [SerializeField]
        private PhysicsSimulationManager physicsManager;

        private void Start()
        {
            if (!ValidateSetup()) return;

            SetupEnvironment();
            InitializeTrainingSystem();
            CreateDebugVisualizations();
        }

        private void OnDestroy()
        {
            if (trainingManager != null)
            {
                trainingManager.OnShotComplete -= HandleShotComplete;
                trainingManager.OnSessionComplete -= HandleSessionComplete;
            }
        }

        private void OnGUI()
        {
            if (!showDebugGUI) return;

            GUILayout.BeginArea(new Rect(guiPosition, guiSize));

            DrawTrainingControls();
            DrawTrainingStatus();
            DrawSettingsInfo();

            GUILayout.EndArea();
        }

        private bool ValidateSetup()
        {
            var isValid = true;

            if (physicsManager == null)
            {
                Debug.LogError("[TrainingTestScene] PhysicsManager is missing!");
                isValid = false;
            }

            if (trainingManager == null)
            {
                Debug.LogError("[TrainingTestScene] TrainingManager is missing!");
                isValid = false;
            }

            if (racketController == null)
            {
                Debug.LogError("[TrainingTestScene] RacketController is missing!");
                isValid = false;
            }

            if (targetPositions == null || targetPositions.Length == 0)
                Debug.LogWarning("[TrainingTestScene] No target positions defined");

            return isValid;
        }

        private void SetupEnvironment()
        {
            // テーブルのセットアップ
            var table = new Table
            {
                Position = tablePosition,
                Rotation = Quaternion.identity,
                Size = physicsManager.GetPhysicsSettings().TableSize
            };
            physicsManager.GetPhysicsEngine().AddTable(table);

            // 初期ボールの配置
            ResetBall();

            Debug.Log("[TrainingTestScene] Environment setup completed");
        }

        private void InitializeTrainingSystem()
        {
            if (initialSettings == null)
            {
                initialSettings = new TrainingSettings
                {
                    SpeedLevel = 1,
                    SpinLevel = 1,
                    CourseLevel = 1
                };
                Debug.LogWarning("[TrainingTestScene] Using default training settings");
            }

            trainingManager.Initialize(initialSettings);
            trainingManager.OnShotComplete += HandleShotComplete;
            trainingManager.OnSessionComplete += HandleSessionComplete;

            Debug.Log("[TrainingTestScene] Training system initialized");
        }

        private void CreateDebugVisualizations()
        {
            if (!showTargetMarkers) return;

            foreach (var position in targetPositions) CreateTargetMarker(position);
        }

        private void CreateTargetMarker(Transform targetTransform)
        {
            var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.transform.parent = transform;
            marker.transform.position = targetTransform.position;
            marker.transform.localScale = Vector3.one * markerRadius * 2;

            var renderer = marker.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                color = new Color(markerColor.r, markerColor.g, markerColor.b, 0.5f)
            };

            // コライダーは不要なので削除
            Destroy(marker.GetComponent<Collider>());
            marker.name = $"TargetMarker_{targetTransform.name}";
        }

        private void HandleShotComplete(TrainingResult result)
        {
            if (result.IsSuccessful)
            {
                ShowHitEffect(result.HitPoint);
                Debug.Log($"[TrainingTestScene] Successful hit at {result.HitPoint}");
            }
            else
            {
                Debug.Log("[TrainingTestScene] Shot missed");
            }
        }

        private void HandleSessionComplete(TrainingSessionResult result)
        {
            Debug.Log("[TrainingTestScene] Training session completed:\n" +
                      $"Success Rate: {result.SuccessRate:P2}\n" +
                      $"Total Shots: {result.TotalShots}\n" +
                      $"Successful Shots: {result.SuccessfulShots}");
        }

        private void ShowHitEffect(Vector3 position)
        {
            // TODO: ヒットエフェクトの実装
            // 例：パーティクルシステムの再生など
        }

        private void ResetBall()
        {
            physicsManager.ResetBall(new PhysicsSimulationManager.BallResetSettings(
                defaultBallPosition,
                Vector3.zero,
                Vector3.zero
            ));
        }

        private void DrawTrainingControls()
        {
            GUILayout.Label("Training Controls", GUI.skin.box);

            if (GUILayout.Button("Start Training"))
                if (trainingManager != null && trainingManager.CurrentState != TrainingState.Running)
                    trainingManager.StartTraining();

            if (GUILayout.Button("Stop Training"))
                if (trainingManager != null && trainingManager.CurrentState == TrainingState.Running)
                    trainingManager.StopTraining();

            if (GUILayout.Button("Reset Ball")) ResetBall();
        }

        private void DrawTrainingStatus()
        {
            if (trainingManager == null) return;

            GUILayout.Space(10);
            GUILayout.Label("Training Status", GUI.skin.box);
            GUILayout.Label($"State: {trainingManager.CurrentState}");
            GUILayout.Label($"Progress: {trainingManager.Progress:P2}");
        }

        private void DrawSettingsInfo()
        {
            if (initialSettings == null) return;

            GUILayout.Space(10);
            GUILayout.Label("Current Settings", GUI.skin.box);
            GUILayout.Label($"Speed Level: {initialSettings.SpeedLevel}");
            GUILayout.Label($"Spin Level: {initialSettings.SpinLevel}");
            GUILayout.Label($"Course Level: {initialSettings.CourseLevel}");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (markerRadius <= 0)
                markerRadius = 0.1f;

            if (guiSize.x < 100) guiSize.x = 100;
            if (guiSize.y < 100) guiSize.y = 100;
        }

        private void OnDrawGizmos()
        {
            if (!showTargetMarkers || targetPositions == null) return;

            Gizmos.color = markerColor;
            foreach (var position in targetPositions)
                if (position != null)
                    Gizmos.DrawWireSphere(position.position, markerRadius);
        }
#endif
    }
}