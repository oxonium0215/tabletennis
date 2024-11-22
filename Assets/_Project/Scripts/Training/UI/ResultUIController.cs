using StepUpTableTennis.DataManagement.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StepUpTableTennis.Training.UI
{
    public class ResultUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text totalShotsText;
        [SerializeField] private TMP_Text successfulShotsText;
        [SerializeField] private TMP_Text accuracyText;
        [SerializeField] private Button retryButton;
        [SerializeField] private TrainingSessionManager sessionManager;

        private void Start()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryButtonPressed);
        }

        public void DisplayResults(TrainingSession session)
        {
            if (session == null || session.Statistics == null) return;

            if (totalShotsText != null)
                totalShotsText.text = $"総ショット数: {session.Statistics.ExecutedShots}";

            if (successfulShotsText != null)
                successfulShotsText.text = $"成功数: {session.Statistics.SuccessfulShots}";

            if (accuracyText != null)
                accuracyText.text = $"正確率: {session.Statistics.SuccessRate:P1}";
        }

        private void OnRetryButtonPressed()
        {
            if (sessionManager != null) sessionManager.StartNewSession();
        }
    }
}