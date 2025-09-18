using UnityEngine;
using UnityEngine.UIElements;

namespace StepUpTableTennis
{
    public class PlaybackUIToolkitController : MonoBehaviour
    {
        private Button _startButton1;
        private SliderInt _shotSlider;
        private SliderInt _intervalSlider;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            // Get references to the UI elements by name
            _startButton1 = root.Q<Button>("StartButton1");
            _shotSlider = root.Q<SliderInt>("ShotSlider"); // Assuming you name your slider in UXML
            _intervalSlider = root.Q<SliderInt>("IntervalSlider"); // Assuming you name your slider in UXML


            // Register event callbacks
            if (_startButton1 != null)
            {
                _startButton1.clicked += OnStartButton1Clicked;
            }

            if (_shotSlider != null)
            {
                _shotSlider.RegisterValueChangedCallback(OnShotSliderChanged);
            }

            if (_intervalSlider != null)
            {
                _intervalSlider.RegisterValueChangedCallback(OnIntervalSliderChanged);
            }
        }

        private void OnDisable()
        {
            if (_startButton1 != null)
            {
                _startButton1.clicked -= OnStartButton1Clicked;
            }
            // It's good practice to unregister callbacks, though for ValueChangedCallback it's not strictly necessary if the object is being destroyed.
        }

        private void OnStartButton1Clicked()
        {
            Debug.Log("Start Button 1 Clicked!");
            // Add your logic here for what happens when the button is clicked
        }

        private void OnShotSliderChanged(ChangeEvent<int> evt)
        {
            Debug.Log($"Shots per session changed to: {evt.newValue}");
            // Add your logic to update the session settings
        }

        private void OnIntervalSliderChanged(ChangeEvent<int> evt)
        {
            Debug.Log($"Shot interval changed to: {evt.newValue}");
            // Add your logic to update the session settings
        }
    }
}