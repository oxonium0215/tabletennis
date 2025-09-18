using UnityEngine;
using UnityEngine.UIElements;

namespace StepUpTableTennis.Training.UI
{
    /// <summary>
    /// UIToolkit-based controller for the SettingsPanel, replacing the legacy SettingCanvas functionality.
    /// Handles slider inputs, button clicks, and VR interaction for training session settings.
    /// </summary>
    public class SettingsPanelController : MonoBehaviour
    {
        [Header("VR Components")] 
        [SerializeField] private GameObject controllerInteractors; // インタラクターの親オブジェクト
        [SerializeField] private GameObject rightControllerInteractor; // 右手用インタラクター
        [SerializeField] private GameObject leftControllerInteractor; // 左手用インタラクター

        [Header("Slider Settings")] 
        [SerializeField] private int minShots = 1;
        [SerializeField] private int maxShots = 50;
        [SerializeField] private int minInterval = 5;
        [SerializeField] private int maxInterval = 30;

        // UI Elements
        private SliderInt shotsPerSessionSlider;
        private SliderInt shotIntervalSlider;
        private Toggle removeBallToggle;
        private Button[] startButtons;

        // References
        private TrainingSessionManager sessionManager;
        private UIDocument uiDocument;
        private VisualElement rootElement;
        private bool isUIVisible = true;

        private void Start()
        {
            // Get required components
            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("SettingsPanelController requires a UIDocument component");
                return;
            }

            sessionManager = FindObjectOfType<TrainingSessionManager>();
            if (sessionManager == null)
            {
                Debug.LogError("TrainingSessionManager not found");
                return;
            }

            InitializeUI();
        }

        private void OnEnable()
        {
            if (uiDocument != null)
            {
                SetupUIElements();
                RegisterEventCallbacks();
            }
        }

        private void OnDisable()
        {
            UnregisterEventCallbacks();
        }

        private void SetupUIElements()
        {
            rootElement = uiDocument.rootVisualElement;

            // Get slider references
            shotsPerSessionSlider = rootElement.Q<SliderInt>("ShotsPerSessionSlider");
            shotIntervalSlider = rootElement.Q<SliderInt>("ShotIntervalSlider");
            removeBallToggle = rootElement.Q<Toggle>("RemoveBallToggle");

            // Get all start buttons
            var buttonList = new System.Collections.Generic.List<Button>();
            for (int i = 1; i <= 17; i++)
            {
                var button = rootElement.Q<Button>($"StartButton{i}");
                if (button != null)
                {
                    buttonList.Add(button);
                }
            }
            startButtons = buttonList.ToArray();

            if (shotsPerSessionSlider == null || shotIntervalSlider == null)
            {
                Debug.LogError("Required UI elements not found in UXML");
            }
        }

        private void InitializeUI()
        {
            if (sessionManager == null) return;

            // Configure sliders with proper ranges
            if (shotsPerSessionSlider != null)
            {
                shotsPerSessionSlider.lowValue = minShots;
                shotsPerSessionSlider.highValue = maxShots;
                shotsPerSessionSlider.value = sessionManager.shotsPerSession;
            }

            if (shotIntervalSlider != null)
            {
                shotIntervalSlider.lowValue = minInterval;
                shotIntervalSlider.highValue = maxInterval;
                // Convert float interval to int for slider (multiply by 10 for decimals)
                shotIntervalSlider.value = Mathf.RoundToInt(sessionManager.shotInterval * 10);
            }

            if (removeBallToggle != null)
            {
                removeBallToggle.value = sessionManager.removeBalLAfterPaddleHit;
            }
        }

        private void RegisterEventCallbacks()
        {
            if (shotsPerSessionSlider != null)
            {
                shotsPerSessionSlider.RegisterValueChangedCallback(OnShotsPerSessionChanged);
            }

            if (shotIntervalSlider != null)
            {
                shotIntervalSlider.RegisterValueChangedCallback(OnShotIntervalChanged);
            }

            if (removeBallToggle != null)
            {
                removeBallToggle.RegisterValueChangedCallback(OnRemoveBallToggleChanged);
            }

            // Register button callbacks
            if (startButtons != null)
            {
                for (int i = 0; i < startButtons.Length; i++)
                {
                    if (startButtons[i] != null)
                    {
                        int buttonIndex = i + 1; // Store button index for callback
                        startButtons[i].clicked += () => OnStartButtonClicked(buttonIndex);
                    }
                }
            }
        }

        private void UnregisterEventCallbacks()
        {
            if (shotsPerSessionSlider != null)
            {
                shotsPerSessionSlider.UnregisterValueChangedCallback(OnShotsPerSessionChanged);
            }

            if (shotIntervalSlider != null)
            {
                shotIntervalSlider.UnregisterValueChangedCallback(OnShotIntervalChanged);
            }

            if (removeBallToggle != null)
            {
                removeBallToggle.UnregisterValueChangedCallback(OnRemoveBallToggleChanged);
            }

            // Note: Button clicked events are automatically handled by UI Toolkit lifecycle
        }

        #region Event Handlers

        private void OnShotsPerSessionChanged(ChangeEvent<int> evt)
        {
            if (sessionManager != null)
            {
                sessionManager.shotsPerSession = evt.newValue;
                Debug.Log($"Shots per session changed to: {evt.newValue}");
            }
        }

        private void OnShotIntervalChanged(ChangeEvent<int> evt)
        {
            if (sessionManager != null)
            {
                // Convert back from int to float (divide by 10)
                float intervalValue = evt.newValue / 10f;
                sessionManager.shotInterval = intervalValue;
                Debug.Log($"Shot interval changed to: {intervalValue:F1}s");
            }
        }

        private void OnRemoveBallToggleChanged(ChangeEvent<bool> evt)
        {
            if (sessionManager != null)
            {
                sessionManager.removeBalLAfterPaddleHit = evt.newValue;
                Debug.Log($"Remove ball after hit: {evt.newValue}");
            }
        }

        private void OnStartButtonClicked(int buttonIndex)
        {
            Debug.Log($"Start Button {buttonIndex} clicked!");
            
            // Update difficulty level based on button pressed
            if (sessionManager != null && sessionManager.difficultySettings != null)
            {
                if (buttonIndex <= 7)
                {
                    // First row buttons: Set course level
                    sessionManager.difficultySettings.CourseLevel = buttonIndex;
                }
                else if (buttonIndex <= 12)
                {
                    // Second row buttons: Set speed level
                    sessionManager.difficultySettings.SpeedLevel = buttonIndex - 7;
                }
                else
                {
                    // Third row buttons: Set spin level
                    sessionManager.difficultySettings.SpinLevel = buttonIndex - 12;
                }
                
                Debug.Log($"Difficulty updated - Course: {sessionManager.difficultySettings.CourseLevel}, " +
                         $"Speed: {sessionManager.difficultySettings.SpeedLevel}, " +
                         $"Spin: {sessionManager.difficultySettings.SpinLevel}");

                // Start new session with updated settings
                sessionManager.StartNewSession();
            }
        }

        #endregion

        #region Public UI Control Methods

        /// <summary>
        /// UIの表示/非表示を切り替えます
        /// </summary>
        public void ToggleUI()
        {
            SetUIVisible(!isUIVisible);
        }

        /// <summary>
        /// UIの表示/非表示を設定します
        /// </summary>
        /// <param name="visible">表示する場合はtrue</param>
        public void SetUIVisible(bool visible)
        {
            isUIVisible = visible;
            
            // UIToolkit element visibility
            if (rootElement != null)
            {
                rootElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // UI表示時のみインタラクターを表示
            SetInteractorsVisibility(visible);
        }

        /// <summary>
        /// 全てのコントローラーインタラクターの表示を制御します
        /// </summary>
        public void SetInteractorsVisibility(bool visible)
        {
            if (controllerInteractors != null) 
                controllerInteractors.SetActive(visible);
        }

        /// <summary>
        /// 特定の手のインタラクターの表示を制御します
        /// </summary>
        /// <param name="isRightHand">制御する手 (true: 右手, false: 左手)</param>
        /// <param name="visible">表示する場合はtrue</param>
        public void SetInteractorVisibilityForHand(bool isRightHand, bool visible)
        {
            var targetInteractor = isRightHand ? rightControllerInteractor : leftControllerInteractor;
            if (targetInteractor != null) 
                targetInteractor.SetActive(visible);
        }

        /// <summary>
        /// Update UI values from session manager (useful for external updates)
        /// </summary>
        public void RefreshUIFromSessionManager()
        {
            InitializeUI();
        }

        #endregion
    }
}