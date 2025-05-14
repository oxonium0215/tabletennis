// PredictionVisualizer.cs
using System.Collections.Generic;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;
using UnityEngine.Rendering;

namespace StepUpTableTennis.TableTennisEngine.Visualization
{
    /// <summary>
    /// 現在ボールの「数フレーム先」を半透明ゴーストで表示する。
    /// TableTennisPhysics.PredictTrajectory() を使うため実際の空力も反映される。
    /// </summary>
    public class PredictionVisualizer : MonoBehaviour
    {
        /* ---------- Inspector ---------- */
        [Header("Prediction Settings")]
        [Tooltip("何フレーム先までゴーストを描画するか")]
        [SerializeField] private int framesAhead = 5;
        [Tooltip("シミュレーション上 1 フレームを何秒で扱うか")]
        [SerializeField] private float frameInterval = 1f / 90f;

        [Header("Visual Settings")]
        [SerializeField] private Material ghostMaterial;

        [Tooltip("ゴースト表示 ON/OFF")]
        public bool predictionEnabled = true;

        /* ---------- 内部状態 ---------- */
        private readonly List<Transform> ghostList = new();
        private Ball targetBall;
        private TableTennisPhysics physicsEngine;
        private float ballRadius = 0.02f;

        #region 初期化 API
        /// <summary>
        /// BallStateManager から呼ばれる。
        /// </summary>
        public void Initialize(Ball ball, TableTennisPhysics engine, float radius)
        {
            targetBall = ball;
            physicsEngine = engine;
            ballRadius = radius;
            PrepareGhostObjects();
        }
        #endregion

        private void PrepareGhostObjects()
        {
            if (ghostMaterial == null)
            {
                Debug.LogError($"{nameof(PredictionVisualizer)} : ghostMaterial が設定されていません");
                return;
            }

            // 既存ゴースト破棄
            foreach (var t in ghostList)
                if (t) Destroy(t.gameObject);
            ghostList.Clear();

            float diameter = ballRadius * 2f;

            for (int i = 0; i < framesAhead; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"GhostBall_{i}";
                go.transform.SetParent(transform, worldPositionStays: false);
                go.transform.localScale = Vector3.one * diameter;

                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = ghostMaterial;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.allowOcclusionWhenDynamic = false;

                Destroy(go.GetComponent<Collider>());     // 当たり判定不要
                ghostList.Add(go.transform);
            }
        }

        private void Update()
        {
            if (!predictionEnabled || targetBall == null || physicsEngine == null)
            {
                SetGhostsVisible(false);
                return;
            }

            SetGhostsVisible(true);
            UpdateGhostTransforms();
        }

        private void SetGhostsVisible(bool on)
        {
            foreach (var t in ghostList)
                if (t && t.TryGetComponent<MeshRenderer>(out var mr))
                    mr.enabled = on;
        }

        /// <summary>
        /// 物理エンジンで厳密に予測し、各フレーム先の位置にゴーストを配置。
        /// </summary>
        private void UpdateGhostTransforms()
        {
            float duration = framesAhead * frameInterval;
            List<Vector3> path = physicsEngine.PredictTrajectory(targetBall, duration);

            if (path.Count == 0) return;

            float dtPhysics = physicsEngine.Settings.TimeStep;
            int samplesPerFrame = Mathf.Max(1, Mathf.RoundToInt(frameInterval / dtPhysics));

            for (int i = 0; i < ghostList.Count; i++)
            {
                int idx = (i + 1) * samplesPerFrame - 1;   // 1 フレーム、2 フレーム… 先
                idx = Mathf.Clamp(idx, 0, path.Count - 1);

                ghostList[i].position = path[idx];
            }
        }

        #region Public Toggle
        public void Toggle(bool on) => predictionEnabled = on;
        #endregion
    }
}
