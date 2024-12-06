using UnityEngine;

namespace StepUpTableTennis.Training
{
    public class VRPaddleController : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Transform paddleVisual;

        [SerializeField] private PaddleStateHandler paddleState;

        [Header("Controller Settings")] [SerializeField]
        private OVRInput.Controller controllerType = OVRInput.Controller.RTouch;

        [Header("Visual Adjustments")] [SerializeField]
        private Vector3 positionOffset = Vector3.zero;

        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        [Header("Collider Adjustments")] [SerializeField]
        private Vector3 colliderPositionOffset = Vector3.zero;

        [SerializeField] private Vector3 colliderRotationOffset = Vector3.zero;

        [Header("Velocity Settings")] [SerializeField]
        private float velocitySmoothing = 0.5f;

        [SerializeField] private bool showDebugGizmos;
        private Vector3 smoothedAngularVelocity;
        private Vector3 smoothedVelocity;
        private Transform trackingSpace;

        private void Start()
        {
            var ovrRig = FindObjectOfType<OVRCameraRig>();
            if (ovrRig != null)
                trackingSpace = ovrRig.trackingSpace;
        }

        private void Update()
        {
            if (trackingSpace == null) return;

            var controllerPosition = OVRInput.GetLocalControllerPosition(controllerType);
            var controllerRotation = OVRInput.GetLocalControllerRotation(controllerType);

            var worldPosition = trackingSpace.TransformPoint(controllerPosition);
            var worldRotation = trackingSpace.rotation * controllerRotation;

            // 視覚的な位置の更新（transformとpaddleVisual）
            UpdateVisualTransforms(worldPosition, worldRotation);

            // 物理的な位置の更新（Paddleクラスのみ）
            UpdatePaddlePhysics(worldPosition, worldRotation);
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!showDebugGizmos || paddleState?.Paddle == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label($"Velocity: {smoothedVelocity:F2} m/s");
            GUILayout.Label($"Angular Velocity: {smoothedAngularVelocity:F2} rad/s");
            GUILayout.Label($"Visual Position: {transform.position:F2}");
            GUILayout.Label($"Collider Position: {paddleState.Paddle.Position:F2}");
            GUILayout.EndArea();
        }
#endif

        private void OnDrawGizmos()
        {
            if (!showDebugGizmos || !Application.isPlaying || trackingSpace == null) return;

            // 視覚的な位置（青）
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.02f);
            DrawAxis(transform.position, transform.rotation, 0.1f);

            // 物理的な位置（赤）
            if (paddleState != null && paddleState.Paddle != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(paddleState.Paddle.Position, 0.02f);
                DrawAxis(paddleState.Paddle.Position, paddleState.Paddle.Rotation, 0.1f);
            }

            // 速度ベクトル（緑）
            Gizmos.color = Color.green;
            Gizmos.DrawLine(paddleState.Paddle.Position,
                paddleState.Paddle.Position + smoothedVelocity * 0.1f);
        }

        private void UpdateVisualTransforms(Vector3 worldPosition, Quaternion worldRotation)
        {
            // 視覚的なオフセットを適用
            var visualPosition = worldPosition + worldRotation * positionOffset;
            var visualRotation = worldRotation * Quaternion.Euler(rotationOffset);

            // transformとpaddleVisualを更新
            transform.position = visualPosition;
            transform.rotation = visualRotation;

            if (paddleVisual != null)
            {
                paddleVisual.position = visualPosition;
                paddleVisual.rotation = visualRotation;
            }
        }

        private void UpdatePaddlePhysics(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (paddleState == null || paddleState.Paddle == null) return;

            // 物理的なオフセットを適用（Paddleクラスのみ）
            var colliderPosition = worldPosition + worldRotation * colliderPositionOffset;
            var colliderRotation = worldRotation * Quaternion.Euler(colliderRotationOffset);

            // コントローラーの速度を取得して変換
            var localVelocity = OVRInput.GetLocalControllerVelocity(controllerType);
            var localAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(controllerType);

            var worldVelocity = trackingSpace.TransformVector(localVelocity);
            var worldAngularVelocity = trackingSpace.TransformVector(localAngularVelocity);

            // スムージングを適用
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, worldVelocity, velocitySmoothing);
            smoothedAngularVelocity = Vector3.Lerp(smoothedAngularVelocity, worldAngularVelocity, velocitySmoothing);

            // パドルの物理状態を更新
            paddleState.Paddle.Position = colliderPosition;
            paddleState.Paddle.Rotation = colliderRotation;
            paddleState.Paddle.Velocity = smoothedVelocity;
            paddleState.Paddle.AngularVelocity = smoothedAngularVelocity;
        }

        private void DrawAxis(Vector3 position, Quaternion rotation, float size)
        {
            // X軸（赤）
            Gizmos.color = new Color(1, 0, 0, 0.8f);
            Gizmos.DrawRay(position, rotation * Vector3.right * size);

            // Y軸（緑）
            Gizmos.color = new Color(0, 1, 0, 0.8f);
            Gizmos.DrawRay(position, rotation * Vector3.up * size);

            // Z軸（青）
            Gizmos.color = new Color(0, 0, 1, 0.8f);
            Gizmos.DrawRay(position, rotation * Vector3.forward * size);
        }
    }
}