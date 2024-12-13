using UnityEngine;

namespace StepUpTableTennis.Playback
{
    public class PlaybackVisualizer : MonoBehaviour
    {
        [SerializeField] private GameObject ballPrefab;
        [SerializeField] private GameObject racketPrefab;
        [SerializeField] private TrailRenderer ballTrail;
        [SerializeField] private bool useSmoothing;
        [SerializeField] private float smoothSpeed = 10f;
        [SerializeField] private bool showTrail = true;
        [SerializeField] private Vector3 paddlePositionOffset;
        [SerializeField] private Vector3 paddleRotationOffset;
        [SerializeField] private bool showDebugVisuals;
        private GameObject playbackBall;
        private GameObject playbackRacket;

        private void Awake()
        {
            InitializeVisualObjects();
            Hide();
        }

        private void OnDrawGizmos()
        {
            if (playbackRacket != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(playbackRacket.transform.position, 0.1f);
                Gizmos.DrawRay(playbackRacket.transform.position,
                    playbackRacket.transform.forward * 0.2f);
            }
        }

        private void InitializeVisualObjects()
        {
            // ボールの初期化
            if (playbackBall == null && ballPrefab != null) playbackBall = Instantiate(ballPrefab);

            if (playbackRacket == null && racketPrefab != null)
                playbackRacket = Instantiate(racketPrefab);

            if (ballTrail != null) ballTrail.enabled = showTrail;
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

                if (showDebugVisuals)
                {
                    // デバッグ表示用
                    Debug.DrawLine(position, position + rotation * Vector3.forward * 0.2f, Color.blue);
                    Debug.DrawLine(position, position + rotation * Vector3.up * 0.2f, Color.green);
                    Debug.DrawLine(position, position + rotation * Vector3.right * 0.2f, Color.red);
                }
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