using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StepUpTableTennis.Training.UI
{
    public class SessionSettingsUI : MonoBehaviour
    {
        [Header("UI Components")] [SerializeField]
        private Slider shotsPerSessionSlider;

        [SerializeField] private TMP_Text shotsPerSessionValueText;
        [SerializeField] private Slider shotIntervalSlider;
        [SerializeField] private TMP_Text shotIntervalValueText;
        [SerializeField] private Toggle removeBallAfterHitToggle;

        [Header("UI Container")] [SerializeField]
        private GameObject uiContainer;

        [Header("VR Components")] [SerializeField]
        private GameObject controllerInteractors; // インタラクターの親オブジェクト

        [SerializeField] private GameObject rightControllerInteractor; // 右手用インタラクター
        [SerializeField] private GameObject leftControllerInteractor; // 左手用インタラクター

        [Header("Slider Settings")] [SerializeField]
        private int minShots = 1;

        [SerializeField] private int maxShots = 50;
        [SerializeField] private float minInterval = 0.5f;
        [SerializeField] private float maxInterval = 3f;
        private bool isUIVisible = true;
        private TrainingSessionManager sessionManager;

        private void Start()
        {
            sessionManager = FindObjectOfType<TrainingSessionManager>();
            InitializeUI();
            SetupListeners();
        }

        private void InitializeUI()
        {
            if (shotsPerSessionSlider != null)
            {
                shotsPerSessionSlider.minValue = minShots;
                shotsPerSessionSlider.maxValue = maxShots;
                shotsPerSessionSlider.value = sessionManager.shotsPerSession;
                UpdateShotsText(sessionManager.shotsPerSession);
            }

            if (shotIntervalSlider != null)
            {
                shotIntervalSlider.minValue = minInterval;
                shotIntervalSlider.maxValue = maxInterval;
                shotIntervalSlider.value = sessionManager.shotInterval;
                UpdateIntervalText(sessionManager.shotInterval);
            }

            if (removeBallAfterHitToggle != null)
                removeBallAfterHitToggle.isOn = sessionManager.removeBalLAfterPaddleHit;
        }

        private void SetupListeners()
        {
            if (shotsPerSessionSlider != null)
                shotsPerSessionSlider.onValueChanged.AddListener(OnShotsPerSessionChanged);

            if (shotIntervalSlider != null) shotIntervalSlider.onValueChanged.AddListener(OnShotIntervalChanged);

            if (removeBallAfterHitToggle != null)
                removeBallAfterHitToggle.onValueChanged.AddListener(OnRemoveBallToggleChanged);
        }

        #region Public UI Control Methods

        /// <summary>
        ///     UIの表示/非表示を切り替えます
        /// </summary>
        public void ToggleUI()
        {
            SetUIVisible(!isUIVisible);
        }

        /// <summary>
        ///     UIの表示/非表示を設定します
        /// </summary>
        /// <param name="visible">表示する場合はtrue</param>
        public void SetUIVisible(bool visible)
        {
            isUIVisible = visible;
            if (uiContainer != null) uiContainer.SetActive(visible);

            // UI表示時のみインタラクターを表示
            SetInteractorsVisibility(visible);
        }

        /// <summary>
        ///     全てのコントローラーインタラクターの表示を制御します
        /// </summary>
        public void SetInteractorsVisibility(bool visible)
        {
            if (controllerInteractors != null) controllerInteractors.SetActive(visible);
        }

        /// <summary>
        ///     特定の手のインタラクターの表示を制御します
        /// </summary>
        /// <param name="hand">制御する手 (true: 右手, false: 左手)</param>
        /// <param name="visible">表示する場合はtrue</param>
        public void SetInteractorVisibilityForHand(bool isRightHand, bool visible)
        {
            var targetInteractor = isRightHand ? rightControllerInteractor : leftControllerInteractor;
            if (targetInteractor != null) targetInteractor.SetActive(visible);
        }

        #endregion

        #region Private Methods

        private void OnShotsPerSessionChanged(float value)
        {
            var shots = Mathf.RoundToInt(value);
            sessionManager.shotsPerSession = shots;
            UpdateShotsText(shots);
        }

        private void OnShotIntervalChanged(float value)
        {
            sessionManager.shotInterval = value;
            UpdateIntervalText(value);
        }

        private void OnRemoveBallToggleChanged(bool value)
        {
            sessionManager.removeBalLAfterPaddleHit = value;
        }

        private void UpdateShotsText(int value)
        {
            if (shotsPerSessionValueText != null) shotsPerSessionValueText.text = $"{value} shots";
        }

        private void UpdateIntervalText(float value)
        {
            if (shotIntervalValueText != null) shotIntervalValueText.text = $"{value:F1} sec";
        }

        private void OnDestroy()
        {
            if (shotsPerSessionSlider != null)
                shotsPerSessionSlider.onValueChanged.RemoveListener(OnShotsPerSessionChanged);

            if (shotIntervalSlider != null) shotIntervalSlider.onValueChanged.RemoveListener(OnShotIntervalChanged);

            if (removeBallAfterHitToggle != null)
                removeBallAfterHitToggle.onValueChanged.RemoveListener(OnRemoveBallToggleChanged);
        }

        #endregion
    }
}