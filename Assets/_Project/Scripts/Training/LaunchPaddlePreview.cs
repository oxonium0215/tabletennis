using System;
using DG.Tweening;
using UnityEngine;
using StepUpTableTennis.Training;
using StepUpTableTennis.DataManagement.Core.Models;
using StepUpTableTennis.TableTennisEngine.Core.Models;

namespace StepUpTableTennis
{
    /// <summary>
    /// 次ショットの発射情報(LaunchPositionなど)を考慮し、
    /// ボールが10m/s程度で直進するときに矛盾しないラケットスイングを演出するコンポーネント。
    /// </summary>
    public class LaunchPaddlePreview : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TrainingSessionManager sessionManager;
        [Tooltip("ラケットの可視化用オブジェクト(ワールド座標で動かす)")]
        [SerializeField] private Transform paddlePreviewTransform;

        [Header("Timing Settings")]
        [Tooltip("ショットの何秒前から構えを始めるか")]
        public float readyTimeBeforeShot = 1.0f;
        [Tooltip("ショットの何秒前からスイング開始するか(バックスイング)")]
        public float swingTimeBeforeShot = 0.3f;
        [Tooltip("インパクト後のフォロースルー時間")]
        public float followThroughTime = 0.2f;

        [Header("Speed Settings")]
        [Tooltip("ラケットの衝突速度に対するボール速度の係数(ラケットが少し速いと仮定)")]
        public float racketToBallSpeedRatio = 1.1f; // ボール10m/sならラケット11m/s

        [Header("Position Offsets")]
        [Tooltip("インパクト位置: LaunchPositionよりどのくらい手前で打つか(ボール進行方向の逆向き)")]
        public float impactDistanceOffset = 0.05f;

        [Tooltip("構え時にImpactPointからどれだけ後方に下げるか(バックスイング開始位置)")]
        public float backSwingDistance = 0.3f;

        [Tooltip("フォロースルーでImpactPointからどれだけ前に振り抜くか")]
        public float followThroughDistance = 0.2f;

        [Header("Angle Offsets")]
        [Tooltip("構え時の回転オフセット(相対的)")]
        public Vector3 readyRotationOffset = new Vector3(-20f, -30f, 0f);
        [Tooltip("フォロースルー時の回転オフセット(相対的)")]
        public Vector3 followRotationOffset = new Vector3(20f, 40f, 0f);

        // DOTween制御
        private Sequence currentSequence;
        private bool isPreviewPlaying;
        private float nextShotTimeCached = -1f;
        private int nextShotIndexCached = -1;

        // ラケットの初期ワールド姿勢を保持
        private Vector3 basePosition;
        private Quaternion baseRotation;

        private void Awake()
        {
            if (sessionManager == null)
                sessionManager = FindObjectOfType<TrainingSessionManager>();

            if (paddlePreviewTransform != null)
            {
                basePosition = paddlePreviewTransform.position;
                baseRotation = paddlePreviewTransform.rotation;
            }

            SubscribeSessionEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeSessionEvents();
        }

        private void SubscribeSessionEvents()
        {
            if (sessionManager == null) return;
            UnsubscribeSessionEvents();

            sessionManager.onSessionStart.AddListener(OnSessionStart);
            sessionManager.onSessionComplete.AddListener(OnSessionComplete);
            sessionManager.onSessionPause.AddListener(OnSessionPause);
            sessionManager.onSessionResume.AddListener(OnSessionResume);
        }

        private void UnsubscribeSessionEvents()
        {
            if (sessionManager == null) return;
            sessionManager.onSessionStart.RemoveListener(OnSessionStart);
            sessionManager.onSessionComplete.RemoveListener(OnSessionComplete);
            sessionManager.onSessionPause.RemoveListener(OnSessionPause);
            sessionManager.onSessionResume.RemoveListener(OnSessionResume);
        }

        private void Update()
        {
            if (sessionManager == null || paddlePreviewTransform == null) return;
            var shot = sessionManager.JustFiredShot;

            if (shot == null) return;

            float nextShotTime = sessionManager.NextShotTime;
            int nextShotIndex = sessionManager.GetCurrentShotIndex();

            // ショット変更やタイミング変化でシーケンス再構築
            if (nextShotIndex != nextShotIndexCached 
                || !Mathf.Approximately(nextShotTimeCached, nextShotTime))
            {
                nextShotIndexCached = nextShotIndex;
                nextShotTimeCached = nextShotTime;
                SetupPaddlePreviewSequence(shot, nextShotTime);
            }

            // 構え開始タイミング
            if (!isPreviewPlaying)
            {
                float startPreviewTime = nextShotTime - readyTimeBeforeShot;
                if (Time.time >= startPreviewTime && Time.time < nextShotTime)
                {
                    currentSequence?.Restart();
                    isPreviewPlaying = true;
                }
            }
        }

        /// <summary>
        /// 次ショットのShotParametersから、物理的に矛盾の少ないラケットスイングアニメを組み立てる。
        /// </summary>
        private void SetupPaddlePreviewSequence(TrainingShot shot, float shotTime)
        {
            currentSequence?.Kill();
            currentSequence = null;
            isPreviewPlaying = false;

            // ShotParameters 取得
            var sp = shot.Parameters; 
            if (sp == null || !sp.IsCalculated)
            {
                Debug.LogWarning($"[LaunchPaddlePreview] ShotParameters not calculated yet.");
                return;
            }

            // Ballの初期速度
            float ballSpeed = sp.InitialSpeed; // 例: 10m/s程度
            var ballDir = sp.InitialVelocity.Value.normalized; 
            var ballLaunchPos = sp.LaunchPosition; 

            // 実際のインパクト位置は「LaunchPosition - ballDir * impactDistanceOffset」
            // (ボールの進行方向の逆向きに少しずらした場所)
            Vector3 impactPoint = ballLaunchPos - ballDir * impactDistanceOffset;

            // ラケット速度を仮定(ラケット/ボール速度比)
            float racketSpeed = ballSpeed * racketToBallSpeedRatio; 
            // これにより、インパクト直前でラケットが ~10m/s 前後で移動している想定

            // --- 時間割 ---
            //   nextShotTime でインパクト
            //   その0.3秒前からスイング開始(バックスイングから加速)
            //   その1.0秒前に構え(ready)に移動
            //   0.2秒後までフォロースルー
            float timeUntilImpact = shotTime - Time.time; 
            float startSwingTime = shotTime - swingTimeBeforeShot;   // バックスイング開始
            float timeForSwing = swingTimeBeforeShot;                // バックスイング + 加速
            // フォロースルーは followThroughTime

            // === 各ポイント計算 ===
            // 1) 構え(ready) -> インパクト点の反対側にバックスイング位置を設ける
            //    バックスイング位置 = impactPoint - ballDir * backSwingDistance
            Vector3 backSwingPos = impactPoint - ballDir * backSwingDistance;

            // 構え(ready)は、バックスイング位置とほぼ同じだが、少し下げor角度オフセットなど
            // ここでは簡単に backSwingPos と同じ座標とする (回転だけいじる)
            Vector3 readyPos = backSwingPos;

            // 2) インパクト(impact) ->  (impactPoint)  (speed=~10~12m/s)
            // 3) フォロースルー(impactの先) -> impactPoint + ballDir * followThroughDistance
            Vector3 followPos = impactPoint + ballDir * followThroughDistance;

            // --- 回転(向き)算出 ---
            //   ラケットがボール進行方向に向いている(法線)とみなし、若干のオフセットを加える
            Quaternion baseRot = Quaternion.LookRotation(ballDir, Vector3.up);

            // ready, follow でそれぞれ少し回転オフセット
            Quaternion readyRot = baseRot * Quaternion.Euler(readyRotationOffset);
            Quaternion impactRot = baseRot; // インパクト時はボールに垂直に合わせる想定
            Quaternion followRot = baseRot * Quaternion.Euler(followRotationOffset);

            // まず位置＆回転をリセット(静止)しておく
            ResetPaddlePreview();

            // === DOTweenシーケンス作成 ===
            currentSequence = DOTween.Sequence()
                .SetAutoKill(false)
                .Pause();

            // A) 構えへ(ready)
            currentSequence.Append(
                paddlePreviewTransform.DOMove(readyPos, 0.1f).SetEase(Ease.Linear)
            );
            currentSequence.Join(
                paddlePreviewTransform.DORotateQuaternion(readyRot, 0.1f)
            );

            // B) バックスイング -> インパクト  (timeForSwing=0.3s ぐらい)
            //    ここで実際に速度が racketSpeed になるような移動量＆時間を計算すると、
            //    移動距離 = Vector3.Distance(backSwingPos, impactPoint)
            //    時間 = timeForSwing
            //    速度 = 距離 / 時間
            //    ここでは演出用に "timeForSwing で移動" する
            float distSwing = Vector3.Distance(backSwingPos, impactPoint);
            float actualSwingVel = distSwing / timeForSwing; // これが演出的なラケット速度
            // 実際に(10~12)m/s近い速度になっているか確認してみる
            Debug.Log($"[LaunchPaddlePreview] Racket velocity approx: {actualSwingVel:F2} m/s (aim={racketSpeed:F2})");

            currentSequence.Append(
                paddlePreviewTransform.DOMove(impactPoint, timeForSwing).SetEase(Ease.OutQuad)
            );
            currentSequence.Join(
                paddlePreviewTransform.DORotateQuaternion(impactRot, timeForSwing)
            );

            // C) フォロースルー
            //    0.2秒程度かけて followPos へ
            currentSequence.Append(
                paddlePreviewTransform.DOMove(followPos, followThroughTime).SetEase(Ease.InQuad)
            );
            currentSequence.Join(
                paddlePreviewTransform.DORotateQuaternion(followRot, followThroughTime)
            );

            // D) 終了後: 少し待って初期位置に戻す or そのまま放置
            currentSequence.AppendInterval(0.1f);
            currentSequence.AppendCallback(() =>
            {
                // 必要なら初期位置に戻す or 非表示
                //paddlePreviewTransform.position = basePosition;
                //paddlePreviewTransform.rotation = baseRotation;
            });
        }

        // --- セッションイベント ---
        private void OnSessionStart()  => ResetPaddlePreview();
        private void OnSessionComplete() => ResetPaddlePreview();
        private void OnSessionPause() => currentSequence?.Pause();
        private void OnSessionResume() => currentSequence?.Play();

        private void ResetPaddlePreview()
        {
            currentSequence?.Kill();
            currentSequence = null;
            isPreviewPlaying = false;

            if (paddlePreviewTransform != null)
            {
                paddlePreviewTransform.position = basePosition;
                paddlePreviewTransform.rotation = baseRotation;
            }
        }
    }
}
