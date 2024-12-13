using System.Collections.Generic;
using StepUpTableTennis.DataManagement.Core.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StepUpTableTennis.Playback
{
    public class PlaybackUIController : MonoBehaviour
    {
        [Header("UI Elements")] [SerializeField]
        private Transform shotButtonContainer;

        [SerializeField] private GameObject shotButtonPrefab;
        [SerializeField] private Slider timelineSlider;
        [SerializeField] private Slider speedSlider;
        [SerializeField] private Button playPauseButton;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text speedText;

        [Header("UI Settings")] [SerializeField]
        private Color successColor = Color.green;

        [SerializeField] private Color failureColor = Color.red;
        private readonly List<Button> shotButtons = new();
        private PlaybackManager playbackManager;

        private void Start()
        {
            playbackManager = GetComponent<PlaybackManager>();
            SetupListeners();
            speedSlider.value = 1.0f;
        }

        private void OnDestroy()
        {
            if (timelineSlider != null)
                timelineSlider.onValueChanged.RemoveListener(OnTimelineValueChanged);

            if (speedSlider != null)
                speedSlider.onValueChanged.RemoveListener(OnSpeedValueChanged);

            if (playPauseButton != null)
                playPauseButton.onClick.RemoveListener(OnPlayPauseClicked);
        }

        private void SetupListeners()
        {
            if (timelineSlider != null)
                timelineSlider.onValueChanged.AddListener(OnTimelineValueChanged);

            if (speedSlider != null)
                speedSlider.onValueChanged.AddListener(OnSpeedValueChanged);

            if (playPauseButton != null)
                playPauseButton.onClick.AddListener(OnPlayPauseClicked);
        }

        public void InitializeShotGrid(List<TrainingShot> shots)
        {
            // 既存のボタンをクリア
            foreach (Transform child in shotButtonContainer) Destroy(child.gameObject);

            // ショットボタンを生成
            for (var i = 0; i < shots.Count; i++)
            {
                var shot = shots[i];
                CreateShotButton(shot, i);
            }
        }

        private void CreateShotButton(TrainingShot shot, int index)
        {
            var buttonObj = Instantiate(shotButtonPrefab, shotButtonContainer);
            var button = buttonObj.GetComponent<Button>();
            var text = buttonObj.GetComponentInChildren<TextMeshProUGUI>();

            if (text != null)
                text.text = $"Shot {index + 1}\n" +
                            $"{(shot.WasSuccessful == true ? "Success" : "Failed")}";

            if (button != null) button.onClick.AddListener(() => OnShotSelected(shot));
        }

        private void OnShotSelected(TrainingShot shot)
        {
            playbackManager.Initialize(shot);
            timelineSlider.value = 0f;
        }

        private void OnTimelineValueChanged(float value)
        {
            playbackManager.SetTime(value);
        }

        private void OnSpeedValueChanged(float value)
        {
            playbackManager.SetPlaybackSpeed(value);
        }

        private void OnPlayPauseClicked()
        {
            if (playbackManager.IsPlaying())
                playbackManager.Pause();
            else
                playbackManager.Play();

            UpdatePlayPauseButton();
        }

        public void UpdateTimelineRange(float min, float max)
        {
            if (timelineSlider != null)
            {
                timelineSlider.minValue = min;
                timelineSlider.maxValue = max;
            }
        }

        public void UpdateCurrentTime(float time)
        {
            if (timelineSlider != null)
                timelineSlider.value = time;

            if (timeText != null)
                timeText.text = $"{time:F2}s";
        }

        public void UpdatePlaybackSpeed(float speed)
        {
            if (speedText != null)
                speedText.text = $"{speed:F1}x";
        }

        private void UpdatePlayPauseButton()
        {
            if (playPauseButton == null) return;

            var icon = playPauseButton.GetComponentInChildren<TextMeshProUGUI>();
            if (icon != null)
                icon.text = playbackManager.IsPlaying() ? "❚❚" : "▶";
        }

        public void ShowPlaybackUI()
        {
            gameObject.SetActive(true);
        }

        public void HidePlaybackUI()
        {
            gameObject.SetActive(false);
        }
    }
}