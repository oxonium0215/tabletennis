using TMPro;
using UnityEngine;

namespace StepUpTableTennis.Training.UI
{
    public class GameplayUIController : MonoBehaviour
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text remainingShotsText;

        public void UpdateScore(int successfulShots, int totalShots)
        {
            if (scoreText != null) scoreText.text = $"成功: {successfulShots} / {totalShots}";
        }

        public void UpdateRemainingShots(int remaining)
        {
            if (remainingShotsText != null) remainingShotsText.text = $"残り: {remaining}球";
        }
    }
}