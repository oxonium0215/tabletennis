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

        [Header("Gaze Visualization")]
        [SerializeField] private GazeVisualizer gazeVisualizer;

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
            if (playbackBall == null && ballPrefab != null)
                playbackBall = Instantiate(ballPrefab);
            if (playbackRacket == null && racketPrefab != null)
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
                // ラケットローカル方向を-0.17だけ下にずらす
                position -= playbackRacket.transform.up * 0.17f;
                playbackRacket.transform.position = position;
                playbackRacket.transform.rotation = rotation;

                if (showDebugVisuals)
                {
                    Debug.DrawLine(position, position + rotation * Vector3.forward * 0.2f, Color.blue);
                    Debug.DrawLine(position, position + rotation * Vector3.up * 0.2f, Color.green);
                    Debug.DrawLine(position, position + rotation * Vector3.right * 0.2f, Color.red);
                }
            }
        }

        public void UpdateGazeVisualization(
            Vector3 leftEyePos, Vector3 leftEyeDir, float leftEyeClosed,
            Vector3 rightEyePos, Vector3 rightEyeDir, float rightEyeClosed,
            float angularVelocity)
        {
            if (gazeVisualizer != null)
            {
                gazeVisualizer.UpdateGazeVisualization(
                    leftEyePos, leftEyeDir, leftEyeClosed,
                    rightEyePos, rightEyeDir, rightEyeClosed,
                    angularVelocity);
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

        /// <summary>
        /// 録画時に記録された IsBallVisible の情報をもとに、
        /// ボールの表示状態を再現します。
        /// </summary>
        /// <param name="isVisible">ボールが見えるなら true</param>
        public void SetBallVisibility(bool isVisible)
        {
            if (playbackBall != null)
            {
                MeshRenderer renderer = playbackBall.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = isVisible;
                }
            }
        }

        /// <summary>
        /// 録画時のサッカード状態をもとに、目線表示に反映します。
        /// </summary>
        /// <param name="isSaccade">サッカード中なら true</param>
        public void SetGazeSaccadeState(bool isSaccade)
        {
            if (gazeVisualizer != null)
            {
                gazeVisualizer.SetGazeSaccadeState(isSaccade);
            }
        }
    }
}