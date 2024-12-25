using UnityEngine;
using DG.Tweening;

namespace StepUpTableTennis.Training
{
    public class LaunchPreviewEffect : MonoBehaviour
    {
        [Header("Animation Settings")]
        [SerializeField] private float previewDuration = 0.5f;
        [SerializeField] private float startSize = 0.3f;
        [SerializeField] private float endSize = 0.05f;
        [SerializeField, Range(0f, 1f)] private float endAlpha = 0.8f;
        
        [Header("References")]
        [SerializeField] private TrainingSessionManager sessionManager;
        [SerializeField] private Material effectMaterial;

        private GameObject effectSphere;
        private Material instanceMaterial;
        private Sequence currentAnimation;
        private bool isEffectPlaying;

        private void Start()
        {
            if (sessionManager == null)
                sessionManager = FindObjectOfType<TrainingSessionManager>();
                
            CreateEffectSphere();
            
            if (sessionManager != null)
            {
                sessionManager.onSessionStart.AddListener(OnSessionStart);
                sessionManager.onSessionComplete.AddListener(OnSessionEnd);
                sessionManager.onSessionPause.AddListener(OnSessionPause);
            }
        }

        private void CreateEffectSphere()
        {
            if (effectMaterial == null)
            {
                Debug.LogError("Effect material is not assigned!");
                return;
            }

            // スフィアの作成
            effectSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effectSphere.name = "LaunchPreviewEffect";
            Destroy(effectSphere.GetComponent<Collider>());

            // マテリアルの設定
            instanceMaterial = new Material(effectMaterial);
            var renderer = effectSphere.GetComponent<MeshRenderer>();
            renderer.material = instanceMaterial;
            renderer.receiveShadows = false;

            // 初期状態の設定
            effectSphere.SetActive(false);
        }

        private void UpdateTransparency(float alpha)
        {
            if (instanceMaterial != null)
            {
                Color color = instanceMaterial.GetColor("_BaseColor");
                color.a = alpha;
                instanceMaterial.SetColor("_BaseColor", color);
            }
        }

        private void PlayEffect(Vector3 position)
        {
            if (effectSphere == null || isEffectPlaying) return;

            // エフェクトの初期化
            isEffectPlaying = true;
            effectSphere.transform.position = position;
            effectSphere.transform.localScale = Vector3.one * startSize;
            effectSphere.SetActive(true);
            UpdateTransparency(0f);

            // 既存のアニメーションをクリア
            currentAnimation?.Kill();

            // 新しいアニメーションシーケンスを作成
            currentAnimation = DOTween.Sequence();

            // スケールとアルファ値のアニメーションを同時に実行
            currentAnimation.Join(
                effectSphere.transform.DOScale(endSize, previewDuration)
                .SetEase(Ease.InQuad)
            );

            currentAnimation.Join(
                DOTween.To(
                    () => 0f,
                    UpdateTransparency,
                    endAlpha,
                    previewDuration
                ).SetEase(Ease.InQuad)
            );

            // アニメーション完了時の処理
            currentAnimation.OnComplete(() => {
                effectSphere.SetActive(false);
                isEffectPlaying = false;
                currentAnimation = null;
            });
        }

        private void Update()
        {
            if (sessionManager == null || !Application.isPlaying) return;

            float timeUntilNextShot = sessionManager.NextShotTime - Time.time;

            if (timeUntilNextShot <= previewDuration && timeUntilNextShot > 0 && !isEffectPlaying)
            {
                PlayEffect(sessionManager.GetNextShotPosition());
            }
        }

        private void OnSessionStart()
        {
            isEffectPlaying = false;
            if (effectSphere != null)
            {
                effectSphere.SetActive(false);
            }
        }

        private void OnSessionEnd()
        {
            CleanupEffect();
        }

        private void OnSessionPause()
        {
            CleanupEffect();
        }

        private void CleanupEffect()
        {
            currentAnimation?.Kill();
            currentAnimation = null;
            isEffectPlaying = false;
            
            if (effectSphere != null)
            {
                effectSphere.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            currentAnimation?.Kill();
            
            if (instanceMaterial != null)
                Destroy(instanceMaterial);
                
            if (effectSphere != null)
                Destroy(effectSphere);
                
            if (sessionManager != null)
            {
                sessionManager.onSessionStart.RemoveListener(OnSessionStart);
                sessionManager.onSessionComplete.RemoveListener(OnSessionEnd);
                sessionManager.onSessionPause.RemoveListener(OnSessionPause);
            }
        }
    }
}