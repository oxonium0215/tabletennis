using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StepUpTableTennis.Training.UI
{
    public enum DifficultyType
    {
        Speed,
        Spin,
        Course
    }

    [RequireComponent(typeof(Button))]
    public class DifficultyButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text buttonText;
        [SerializeField] private int buttonLevel;
        [SerializeField] private Image buttonBackground;

        [Header("Text Colors")] [SerializeField]
        private Color defaultTextColor = new(0.85f, 0.85f, 0.83f);

        [SerializeField] private Color selectedTextColor = Color.white;

        [Header("Button Colors")] [SerializeField]
        private Color defaultBackgroundColor = Color.white;

        [SerializeField] private Color selectedBackgroundColor = Color.blue;
        [SerializeField] private DifficultyType difficultyType;
        private Button button;
        private bool isSelected;
        private TrainingSessionManager sessionManager;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClick);
        }

        private void Start()
        {
            sessionManager = FindObjectOfType<TrainingSessionManager>();
            if (buttonBackground == null)
            {
                buttonBackground = GetComponent<Image>();
                if (buttonBackground == null) buttonBackground = gameObject.AddComponent<Image>();
            }

            UpdateVisuals();
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnButtonClick);
        }

        private void UpdateVisuals()
        {
            if (buttonText == null) return;

            isSelected = GetCurrentLevel() == buttonLevel;

            if (buttonBackground != null)
                buttonBackground.color = isSelected ? selectedBackgroundColor : defaultBackgroundColor;

            var textColor = isSelected ? selectedTextColor : defaultTextColor;
            var value = GetValueForLevel();
            var unit = GetUnitForLevel();

            buttonText.text = string.Format(
                "<size=40><color=#{0}>Level {1}</color></size>\n" +
                "<size=30><color=#{0}>{2}</color></size>\n" +
                "<size=25><color=#{0}>{3}</color></size>",
                ColorUtility.ToHtmlStringRGBA(textColor),
                buttonLevel,
                value,
                unit
            );

            buttonText.alignment = TextAlignmentOptions.Center;
        }

        private void OnButtonClick()
        {
            switch (difficultyType)
            {
                case DifficultyType.Speed:
                    sessionManager.difficultySettings.SpeedLevel = buttonLevel;
                    break;
                case DifficultyType.Spin:
                    sessionManager.difficultySettings.SpinLevel = buttonLevel;
                    break;
                case DifficultyType.Course:
                    sessionManager.difficultySettings.CourseLevel = buttonLevel;
                    break;
            }

            UpdateAllButtonsOfType(difficultyType);
        }

        private void UpdateAllButtonsOfType(DifficultyType type)
        {
            var buttons = FindObjectsOfType<DifficultyButton>();
            foreach (var button in buttons)
                if (button.difficultyType == type)
                    button.UpdateVisuals();
        }

        private int GetCurrentLevel()
        {
            return difficultyType switch
            {
                DifficultyType.Speed => sessionManager.difficultySettings.SpeedLevel,
                DifficultyType.Spin => sessionManager.difficultySettings.SpinLevel,
                DifficultyType.Course => sessionManager.difficultySettings.CourseLevel,
                _ => 1
            };
        }

        private string GetValueForLevel()
        {
            return difficultyType switch
            {
                DifficultyType.Speed => $"{GetSpeedValue(buttonLevel):F1}",
                DifficultyType.Spin => $"{GetSpinValue(buttonLevel)}",
                DifficultyType.Course => $"±{GetCourseValue(buttonLevel):F1}",
                _ => ""
            };
        }

        private string GetUnitForLevel()
        {
            return difficultyType switch
            {
                DifficultyType.Speed => "m/s",
                DifficultyType.Spin => "rps",
                DifficultyType.Course => "m",
                _ => ""
            };
        }

        private float GetSpeedValue(int level)
        {
            return level switch
            {
                1 => 5.0f, // 18 km/h
                2 => 5.5f, // 20 km/h
                3 => 6.1f, // 22 km/h
                4 => 6.7f, // 24 km/h
                5 => 7.2f, // 26 km/h
                6 => 7.8f, // 28 km/h
                7 => 11.1f, // 40 km/h
                _ => 5.0f
            };
        }

        private string GetSpinValue(int level)
        {
            return level switch
            {
                1 => "0-10",
                2 => "5-20",
                3 => "15-35",
                4 => "25-45",
                5 => "35-60",
                _ => "0-10"
            };
        }

        private float GetCourseValue(int level)
        {
            return level switch
            {
                1 => 0.1f,
                2 => 0.2f,
                3 => 0.3f,
                4 => 0.4f,
                5 => 0.5f,
                _ => 0.1f
            };
        }
    }
}