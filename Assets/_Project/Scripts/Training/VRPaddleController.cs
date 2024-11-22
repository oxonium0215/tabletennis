using UnityEngine;

namespace StepUpTableTennis.Training
{
    public class VRPaddleController : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Transform racketVisual;

        [SerializeField] private PaddleSetup paddleSetup;

        [Header("Controller Settings")] [SerializeField]
        private OVRInput.Controller controllerType = OVRInput.Controller.RTouch;

        [Header("Adjustments")] [SerializeField]
        private Vector3 positionOffset = Vector3.zero;

        [SerializeField] private Vector3 rotationOffset = Vector3.zero;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Transform trackingSpace;

        private void Start()
        {
            var ovrRig = FindObjectOfType<OVRCameraRig>();
            if (ovrRig != null)
                trackingSpace = ovrRig.trackingSpace;

            // 初期位置と回転を保存
            previousPosition = transform.position;
            previousRotation = transform.rotation;
        }

        private void Update()
        {
            UpdateRacketTransform();
            UpdatePaddlePhysics();
        }

        private void UpdateRacketTransform()
        {
            if (trackingSpace == null) return;

            // コントローラーの位置と回転を取得
            var controllerPosition = OVRInput.GetLocalControllerPosition(controllerType);
            var controllerRotation = OVRInput.GetLocalControllerRotation(controllerType);

            // トラッキングスペースに変換
            var worldPosition = trackingSpace.TransformPoint(controllerPosition);
            var worldRotation = trackingSpace.rotation * controllerRotation;

            // オフセットを適用
            worldPosition += worldRotation * positionOffset;
            var finalRotation = worldRotation * Quaternion.Euler(rotationOffset);

            // ラケットの視覚的な更新
            if (racketVisual != null)
            {
                racketVisual.position = worldPosition;
                racketVisual.rotation = finalRotation;
            }
        }

        private void UpdatePaddlePhysics()
        {
            if (paddleSetup == null || paddleSetup.Paddle == null) return;

            // 現在の位置と回転
            var currentPosition = transform.position;
            var currentRotation = transform.rotation;

            // 速度と角速度の計算
            var velocity = (currentPosition - previousPosition) / Time.deltaTime;
            var deltaRotation = currentRotation * Quaternion.Inverse(previousRotation);
            float angle;
            Vector3 axis;
            deltaRotation.ToAngleAxis(out angle, out axis);
            if (angle > 180f) angle -= 360f;
            var angularVelocity = angle * Mathf.Deg2Rad / Time.deltaTime * axis.normalized;

            // パドルの物理状態を更新
            paddleSetup.Paddle.Position = currentPosition;
            paddleSetup.Paddle.Rotation = currentRotation;
            paddleSetup.Paddle.Velocity = velocity;
            paddleSetup.Paddle.AngularVelocity = angularVelocity;

            // 前フレームの状態を保存
            previousPosition = currentPosition;
            previousRotation = currentRotation;
        }
    }
}