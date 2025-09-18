using UnityEngine;
using UnityEngine.UIElements;

namespace StepUpTableTennis.Training.UI
{
    /// <summary>
    /// Simple test script to validate SettingsPanelController functionality
    /// </summary>
    public class SettingsPanelTest : MonoBehaviour
    {
        [Header("Test Components")]
        [SerializeField] private SettingsPanelController settingsPanel;
        [SerializeField] private TrainingSessionManager sessionManager;
        
        [Header("Test Values")]
        [SerializeField] private int testShotsValue = 25;
        [SerializeField] private float testIntervalValue = 2.0f;
        [SerializeField] private bool testRemoveBallValue = true;

        private void Start()
        {
            if (settingsPanel == null)
                settingsPanel = FindObjectOfType<SettingsPanelController>();
                
            if (sessionManager == null)
                sessionManager = FindObjectOfType<TrainingSessionManager>();
                
            if (settingsPanel == null || sessionManager == null)
            {
                Debug.LogError("SettingsPanelTest: Required components not found");
                return;
            }
            
            Debug.Log("SettingsPanelTest: All components found, test ready");
        }

        [ContextMenu("Test UI Visibility Toggle")]
        public void TestUIVisibilityToggle()
        {
            if (settingsPanel != null)
            {
                settingsPanel.ToggleUI();
                Debug.Log("SettingsPanelTest: UI visibility toggled");
            }
        }

        [ContextMenu("Test Settings Update")]
        public void TestSettingsUpdate()
        {
            if (sessionManager == null) return;

            // Store original values
            int originalShots = sessionManager.shotsPerSession;
            float originalInterval = sessionManager.shotInterval;
            bool originalRemoveBall = sessionManager.removeBalLAfterPaddleHit;

            Debug.Log($"Original values - Shots: {originalShots}, Interval: {originalInterval}, RemoveBall: {originalRemoveBall}");

            // Update values
            sessionManager.shotsPerSession = testShotsValue;
            sessionManager.shotInterval = testIntervalValue;
            sessionManager.removeBalLAfterPaddleHit = testRemoveBallValue;

            Debug.Log($"Updated values - Shots: {sessionManager.shotsPerSession}, Interval: {sessionManager.shotInterval}, RemoveBall: {sessionManager.removeBalLAfterPaddleHit}");

            // Refresh UI to show new values
            if (settingsPanel != null)
            {
                settingsPanel.RefreshUIFromSessionManager();
                Debug.Log("SettingsPanelTest: UI refreshed with new values");
            }
        }

        [ContextMenu("Test Difficulty Settings")]
        public void TestDifficultySettings()
        {
            if (sessionManager?.difficultySettings == null) return;

            Debug.Log($"Current difficulty - Course: {sessionManager.difficultySettings.CourseLevel}, " +
                     $"Speed: {sessionManager.difficultySettings.SpeedLevel}, " +
                     $"Spin: {sessionManager.difficultySettings.SpinLevel}");

            // Test updating difficulty
            sessionManager.difficultySettings.CourseLevel = 3;
            sessionManager.difficultySettings.SpeedLevel = 4;
            sessionManager.difficultySettings.SpinLevel = 2;
            sessionManager.difficultySettings.UpdateCourseTemplate();

            Debug.Log($"Updated difficulty - Course: {sessionManager.difficultySettings.CourseLevel}, " +
                     $"Speed: {sessionManager.difficultySettings.SpeedLevel}, " +
                     $"Spin: {sessionManager.difficultySettings.SpinLevel}");
        }

        [ContextMenu("Test VR Interactor Control")]
        public void TestVRInteractorControl()
        {
            if (settingsPanel == null) return;

            // Test controller visibility
            settingsPanel.SetInteractorsVisibility(true);
            Debug.Log("SettingsPanelTest: VR interactors set to visible");

            // Test individual hand control
            settingsPanel.SetInteractorVisibilityForHand(true, false); // Hide right hand
            settingsPanel.SetInteractorVisibilityForHand(false, true); // Show left hand
            Debug.Log("SettingsPanelTest: Individual hand visibility tested");
        }
    }
}