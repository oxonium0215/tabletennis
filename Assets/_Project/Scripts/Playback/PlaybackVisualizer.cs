using UnityEngine;

namespace StepUpTableTennis.Playback
{
    public class PlaybackVisualizer : MonoBehaviour
    {
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private GameObject racketPrefab;
        [SerializeField] private TrailRenderer ballTrail;
        private GameObject playbackBall;
        private GameObject playbackRacket;
        private bool showTrail = true;

        private void Awake()
        {
            InitializeVisualObjects();
        }

        private void InitializeVisualObjects()
        {
            if (playbackBall == null)
                playbackBall = Instantiate(ballPrefab);

            if (playbackRacket == null)
                playbackRacket = Instantiate(racketPrefab);

            if (ballTrail != null)
                ballTrail.enabled = showTrail;
        }

        public void UpdateBallTransform(Vector3 position, Quaternion rotation)
        {
            if (playbackBall != null)
            {
                playbackBall.transform.position = position;
                playbackBall.transform.rotation = rotation;
            }
        }

        public void UpdateRacketTransform(Vector3 position, Quaternion rotation)
        {
            if (playbackRacket != null)
            {
                playbackRacket.transform.position = position;
                playbackRacket.transform.rotation = rotation;
            }
        }

        public void SetTrailEnabled(bool enabled)
        {
            showTrail = enabled;
            if (ballTrail != null)
                ballTrail.enabled = enabled;
        }

        public void ClearTrail()
        {
            if (ballTrail != null)
                ballTrail.Clear();
        }

        public void Hide()
        {
            if (playbackBall != null)
                playbackBall.SetActive(false);
            if (playbackRacket != null)
                playbackRacket.SetActive(false);
        }

        public void Show()
        {
            if (playbackBall != null)
                playbackBall.SetActive(true);
            if (playbackRacket != null)
                playbackRacket.SetActive(true);
        }
    }
}