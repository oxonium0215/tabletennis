using UnityEngine;

namespace StepUpTableTennis
{
    public class EyeGazeController : MonoBehaviour
    {
        public GameObject arrow;
        [SerializeField] private Vector3 normalScale = Vector3.one;
        [SerializeField] private Vector3 saccadeScale = new Vector3(2f, 2f, 2f);
        [SerializeField] private float scaleTransitionSpeed = 5f;

        private OVREyeGaze eyeGaze;
        private SaccadeDetector saccadeDetector;
        private Vector3 targetScale;

        void Start()
        {
            eyeGaze = GetComponent<OVREyeGaze>();
            
            saccadeDetector = gameObject.GetComponent<SaccadeDetector>();
            
            // 初期スケールを設定
            if (arrow != null)
            {
                arrow.transform.localScale = normalScale;
                targetScale = normalScale;
            }
        }

        void Update()
        {
            if (eyeGaze == null || arrow == null) return;

            if (eyeGaze.EyeTrackingEnabled)
            {
                // 視線方向の更新
                arrow.transform.rotation = eyeGaze.transform.rotation;

                // サッカード状態の更新
                bool isSaccade = saccadeDetector.UpdateSaccadeState(eyeGaze.transform.forward);

                // サッカード状態に応じてターゲットスケールを設定
                targetScale = isSaccade ? saccadeScale : normalScale;

                // スケールの補間
                arrow.transform.localScale = Vector3.Lerp(
                    arrow.transform.localScale, 
                    targetScale, 
                    Time.deltaTime * scaleTransitionSpeed
                );
            }
        }

        // Inspector上で設定を調整できるメソッド
        public void SetScales(Vector3 normal, Vector3 saccade)
        {
            normalScale = normal;
            saccadeScale = saccade;
        }

        public void SetTransitionSpeed(float speed)
        {
            scaleTransitionSpeed = speed;
        }
    }
}