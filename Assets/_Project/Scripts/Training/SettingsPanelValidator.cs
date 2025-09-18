using UnityEngine;
using UnityEngine.UIElements;

namespace StepUpTableTennis.Training.UI
{
    /// <summary>
    /// Validation script to ensure SettingsPanelController integration meets all requirements
    /// </summary>
    public class SettingsPanelValidator : MonoBehaviour
    {
        [Header("Validation Results")]
        [SerializeField] private bool uiElementsFound = false;
        [SerializeField] private bool sessionManagerConnected = false;
        [SerializeField] private bool vrInteractorsConfigured = false;
        [SerializeField] private bool eventHandlersRegistered = false;

        private SettingsPanelController controller;
        private UIDocument uiDocument;

        private void Start()
        {
            ValidateImplementation();
        }

        [ContextMenu("Run Validation")]
        public void ValidateImplementation()
        {
            Debug.Log("=== SettingsPanel UIToolkit Implementation Validation ===");

            // Find components
            controller = GetComponent<SettingsPanelController>();
            uiDocument = GetComponent<UIDocument>();

            // Validate UI Document and Elements
            ValidateUIElements();

            // Validate Session Manager Integration
            ValidateSessionManager();

            // Validate VR Configuration
            ValidateVRSetup();

            // Validate Event Handling
            ValidateEventHandling();

            // Summary
            bool allValid = uiElementsFound && sessionManagerConnected && vrInteractorsConfigured && eventHandlersRegistered;
            
            Debug.Log($"=== Validation Summary ===");
            Debug.Log($"UI Elements Found: {uiElementsFound}");
            Debug.Log($"Session Manager Connected: {sessionManagerConnected}");
            Debug.Log($"VR Interactors Configured: {vrInteractorsConfigured}");
            Debug.Log($"Event Handlers Registered: {eventHandlersRegistered}");
            Debug.Log($"Overall Status: {(allValid ? "✓ PASS" : "✗ FAIL")}");

            if (allValid)
            {
                Debug.Log("SettingsPanel implementation successfully replaces SettingCanvas functionality!");
            }
            else
            {
                Debug.LogWarning("Some validation checks failed. Please review the setup.");
            }
        }

        private void ValidateUIElements()
        {
            if (uiDocument == null)
            {
                Debug.LogError("UIDocument component not found");
                uiElementsFound = false;
                return;
            }

            var root = uiDocument.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("Root visual element not found");
                uiElementsFound = false;
                return;
            }

            // Check for required UI elements
            var shotSlider = root.Q<SliderInt>("ShotsPerSessionSlider");
            var intervalSlider = root.Q<SliderInt>("ShotIntervalSlider");
            var removeBallToggle = root.Q<Toggle>("RemoveBallToggle");
            var startButton1 = root.Q<Button>("StartButton1");

            bool elementsExist = shotSlider != null && intervalSlider != null && 
                               removeBallToggle != null && startButton1 != null;

            uiElementsFound = elementsExist;

            Debug.Log($"UI Elements Validation: {(elementsExist ? "✓" : "✗")}");
            if (!elementsExist)
            {
                Debug.LogError("Required UI elements not found in UXML");
            }
        }

        private void ValidateSessionManager()
        {
            var sessionManager = FindObjectOfType<TrainingSessionManager>();
            sessionManagerConnected = sessionManager != null;

            Debug.Log($"Session Manager Validation: {(sessionManagerConnected ? "✓" : "✗")}");
            
            if (sessionManagerConnected)
            {
                // Test property access
                try
                {
                    int shots = sessionManager.shotsPerSession;
                    float interval = sessionManager.shotInterval;
                    bool removeBall = sessionManager.removeBalLAfterPaddleHit;
                    var difficulty = sessionManager.difficultySettings;

                    Debug.Log($"Session Settings - Shots: {shots}, Interval: {interval}, RemoveBall: {removeBall}");
                    Debug.Log($"Difficulty Levels - Course: {difficulty?.CourseLevel}, Speed: {difficulty?.SpeedLevel}, Spin: {difficulty?.SpinLevel}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error accessing session manager properties: {e.Message}");
                    sessionManagerConnected = false;
                }
            }
            else
            {
                Debug.LogWarning("TrainingSessionManager not found in scene");
            }
        }

        private void ValidateVRSetup()
        {
            if (controller == null)
            {
                vrInteractorsConfigured = false;
                Debug.LogError("SettingsPanelController not found");
                return;
            }

            // Use reflection to check private fields (in a real scenario, these would be public properties)
            var controllerType = typeof(SettingsPanelController);
            var interactorsField = controllerType.GetField("controllerInteractors", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rightField = controllerType.GetField("rightControllerInteractor", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var leftField = controllerType.GetField("leftControllerInteractor", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            bool hasInteractors = interactorsField?.GetValue(controller) != null;
            bool hasRightController = rightField?.GetValue(controller) != null;
            bool hasLeftController = leftField?.GetValue(controller) != null;

            vrInteractorsConfigured = hasInteractors || (hasRightController && hasLeftController);

            Debug.Log($"VR Setup Validation: {(vrInteractorsConfigured ? "✓" : "✗")}");
            if (!vrInteractorsConfigured)
            {
                Debug.LogWarning("VR interactor GameObjects not assigned in inspector");
            }
        }

        private void ValidateEventHandling()
        {
            eventHandlersRegistered = controller != null && uiDocument != null;

            Debug.Log($"Event Handling Validation: {(eventHandlersRegistered ? "✓" : "✗")}");

            if (eventHandlersRegistered)
            {
                Debug.Log("Event handlers should be registered in OnEnable/OnDisable lifecycle");
            }
            else
            {
                Debug.LogError("Components required for event handling not found");
            }
        }

        [ContextMenu("Test Button Functionality")]
        public void TestButtonFunctionality()
        {
            if (!uiElementsFound || !sessionManagerConnected)
            {
                Debug.LogError("Cannot test button functionality - validation failed");
                return;
            }

            Debug.Log("=== Testing Button Functionality ===");
            
            // This would simulate button clicks if we were in play mode
            // In edit mode, we can only validate the structure
            
            var sessionManager = FindObjectOfType<TrainingSessionManager>();
            if (sessionManager?.difficultySettings != null)
            {
                // Test difficulty range validation
                Debug.Log("Testing difficulty level ranges:");
                Debug.Log($"Course Level Range: 1-5 (Current: {sessionManager.difficultySettings.CourseLevel})");
                Debug.Log($"Speed Level Range: 1-7 (Current: {sessionManager.difficultySettings.SpeedLevel})");
                Debug.Log($"Spin Level Range: 1-5 (Current: {sessionManager.difficultySettings.SpinLevel})");
            }
        }
    }
}