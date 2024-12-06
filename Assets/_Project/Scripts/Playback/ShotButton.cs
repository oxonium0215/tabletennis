using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StepUpTableTennis.Playback
{
    public class ShotButton : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text shotText;

        public void Setup(int shotNumber, bool wasSuccessful)
        {
            shotText.text = $"Shot {shotNumber}";
            background.color = wasSuccessful ? Color.green : Color.red;
        }
    }
}