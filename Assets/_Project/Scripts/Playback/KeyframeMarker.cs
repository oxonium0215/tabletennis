using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StepUpTableTennis.Playback
{
    public class PlaybackKeyframe
    {
        public enum KeyframeType
        {
            Start,
            Collision,
            End,
            Custom
        }

        public PlaybackKeyframe(float time, string label, KeyframeType type)
        {
            Time = time;
            Label = label;
            Type = type;
        }

        public float Time { get; }
        public string Label { get; }
        public KeyframeType Type { get; }
    }

    // キーフレームのUI表示を管理するコンポーネント
    public class KeyframeMarker : MonoBehaviour
    {
        [SerializeField] private RectTransform markerRect;
        [SerializeField] private TextMeshProUGUI label;

        public void Initialize(PlaybackKeyframe keyframe, float normalizedPosition)
        {
            if (markerRect != null)
                markerRect.anchorMin = markerRect.anchorMax =
                    new Vector2(normalizedPosition, 0.5f);

            if (label != null)
                label.text = keyframe.Label;

            // キーフレームタイプに応じた見た目の設定
            var image = GetComponent<Image>();
            if (image != null)
                image.color = keyframe.Type switch
                {
                    PlaybackKeyframe.KeyframeType.Start => Color.green,
                    PlaybackKeyframe.KeyframeType.Collision => Color.red,
                    PlaybackKeyframe.KeyframeType.End => Color.yellow,
                    _ => Color.white
                };
        }
    }
}