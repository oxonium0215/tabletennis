using DG.Tweening;
using TMPro;
using UnityEngine;

namespace StepUpTableTennis.Training.UI
{
    public class SessionStatisticsDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI statisticsText;

        [Header("Animation Settings")] [SerializeField]
        private bool useAnimation = true;

        [SerializeField] private float displayDuration = 3f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        private void Start()
        {
            if (statisticsText == null) statisticsText = GetComponent<TextMeshProUGUI>();
            HideText();
        }

        private void OnDestroy()
        {
            statisticsText?.DOKill();
        }

        // TrainingSessionManagerのonSessionStatisticsイベントにこのメソッドを登録
        public void DisplayStatistics(int successfulShots, int totalShots, float successRate)
        {
            var statsMessage = "Session Complete!\n" +
                               $"Successful Shots: {successfulShots}/{totalShots}\n" +
                               $"Success Rate: {successRate:F1}%";

            statisticsText.text = statsMessage;

            if (useAnimation)
                AnimateStatistics();
            else
                statisticsText.alpha = 1f;
        }

        private void AnimateStatistics()
        {
            // 既存のアニメーションをキル
            statisticsText.DOKill();

            // アニメーションシーケンスを作成
            var sequence = DOTween.Sequence();

            // テキストをフェードイン
            statisticsText.alpha = 0f;
            sequence.Append(statisticsText.DOFade(1f, fadeInDuration));

            // 表示時間を待つ
            sequence.AppendInterval(displayDuration);

            // フェードアウト
            sequence.Append(statisticsText.DOFade(0f, fadeOutDuration));
        }

        private void HideText()
        {
            if (statisticsText != null) statisticsText.alpha = 0f;
        }
    }
}