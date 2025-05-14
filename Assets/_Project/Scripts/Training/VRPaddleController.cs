using UnityEngine;
using StepUpTableTennis.Training;
using StepUpTableTennis.TableTennisEngine.Objects; // 必要に応じて

namespace StepUpTableTennis.Training
{
    public class VRPaddleController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform racketVisual;
        [SerializeField] private PaddleSetup paddleSetup;

        [Header("Controller Settings")]
        [SerializeField] private OVRInput.Controller controllerType = OVRInput.Controller.RTouch;

        [Header("Prediction Settings")]
        [Tooltip("コントローラーの動きを何秒先まで予測して反映するか")]
        [SerializeField] private float predictionTimeSeconds = 0.05f;

        // --- ▼▼▼ 追加 ▼▼▼ ---
        [Tooltip("予測に加速度を含めるか")]
        [SerializeField] private bool includeAcceleration = true; // 加速度を使用するかどうかのフラグ
        // --- ▲▲▲ 追加 ▲▲▲ ---


        [Header("Adjustments")]
        [Tooltip("予測された位置・回転に適用されるオフセット")]
        [SerializeField] private Vector3 positionOffset = Vector3.zero;
        [Tooltip("予測された位置・回転に適用される回転オフセット")]
        [SerializeField] private Vector3 rotationOffset = Vector3.zero;

        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private float lastUpdateTime;

        private Transform trackingSpace;

        private void Awake()
        {
            var ovrRig = FindObjectOfType<OVRCameraRig>();
            if (ovrRig != null)
            {
                trackingSpace = ovrRig.trackingSpace;
            }
            else
            {
                Debug.LogWarning("OVRCameraRig not found. VR tracking might not work correctly.");
            }

            if (racketVisual == null)
            {
                racketVisual = transform;
            }

            lastUpdateTime = Time.time;

            // 初期位置を計算して前回値として設定
            UpdateRacketTransform();
            previousPosition = racketVisual.position;
            previousRotation = racketVisual.rotation;
        }

        private void Update()
        {
            UpdateRacketTransform();
            UpdatePaddlePhysics();
        }

        // --- ▼▼▼ 変更 (加速度対応) ▼▼▼ ---
        private void UpdateRacketTransform()
        {
            if (trackingSpace == null || racketVisual == null) return;

            // 1. コントローラーの現在の状態を取得 (ローカル)
            var controllerPositionLocal = OVRInput.GetLocalControllerPosition(controllerType);
            var controllerRotationLocal = OVRInput.GetLocalControllerRotation(controllerType);
            var controllerVelocityLocal = OVRInput.GetLocalControllerVelocity(controllerType);
            var controllerAngularVelocityLocal = OVRInput.GetLocalControllerAngularVelocity(controllerType);
            // --- 加速度データを取得 ---
            var controllerAccelerationLocal = includeAcceleration ? OVRInput.GetLocalControllerAcceleration(controllerType) : Vector3.zero;
            var controllerAngularAccelerationLocal = includeAcceleration ? OVRInput.GetLocalControllerAngularAcceleration(controllerType) : Vector3.zero;
            // ------------------------

            // 2. ワールド座標系に変換
            var currentWorldPosition = trackingSpace.TransformPoint(controllerPositionLocal);
            var currentWorldRotation = trackingSpace.rotation * controllerRotationLocal;
            var currentWorldVelocity = trackingSpace.TransformDirection(controllerVelocityLocal);
            var currentWorldAngularVelocity = trackingSpace.rotation * controllerAngularVelocityLocal;
            // --- 加速度データもワールド座標系へ ---
            var currentWorldAcceleration = trackingSpace.TransformDirection(controllerAccelerationLocal);
            var currentWorldAngularAcceleration = trackingSpace.rotation * controllerAngularAccelerationLocal;
            // ----------------------------------

            // 3. 未来予測計算
            Vector3 predictedWorldPosition;
            Quaternion predictedWorldRotation;
            float time = predictionTimeSeconds;
            float timeSq = time * time; // 時間の2乗を事前計算

            // --- 位置予測 (加速度考慮) ---
            predictedWorldPosition = currentWorldPosition
                                   + currentWorldVelocity * time
                                   + 0.5f * currentWorldAcceleration * timeSq;
            // --------------------------

            // --- 回転予測 (角加速度考慮) ---
            // 予測時間後の角速度を計算
            Vector3 predictedWorldAngularVelocity = currentWorldAngularVelocity + currentWorldAngularAcceleration * time;
            // 予測期間中の平均角速度を計算 (線形近似)
            Vector3 averageWorldAngularVelocity = (currentWorldAngularVelocity + predictedWorldAngularVelocity) * 0.5f;
            // 平均角速度ベクトルから予測時間分の回転差分(Quaternion)を計算
            // 角速度ベクトル (rad/s) * 時間 (s) = 回転ベクトル (rad)
            // Quaternion.Euler() は度数法のベクトルを期待するので変換
            Quaternion rotationDelta = Quaternion.Euler(averageWorldAngularVelocity * time * Mathf.Rad2Deg);
            // 現在の回転に回転差分を適用
            predictedWorldRotation = currentWorldRotation * rotationDelta;
            // --------------------------

            // 4. オフセットを予測された位置・回転に適用
            var finalPosition = predictedWorldPosition + predictedWorldRotation * positionOffset;
            var finalRotation = predictedWorldRotation * Quaternion.Euler(rotationOffset);

            // 5. ラケットの視覚的な更新 (racketVisual)
            racketVisual.position = finalPosition;
            racketVisual.rotation = finalRotation;
        }
        // --- ▲▲▲ 変更 (加速度対応) ▲▲▲ ---


        // UpdatePaddlePhysics は変更不要
        // (racketVisual の更新結果に基づいて計算するため、間接的に加速度予測の影響を受ける)
        private void UpdatePaddlePhysics()
        {
            if (paddleSetup == null || paddleSetup.Paddle == null || racketVisual == null) return;

            var currentTime = Time.time;
            var deltaTime = currentTime - lastUpdateTime;

            if (deltaTime <= Mathf.Epsilon) return;

            var currentPosition = racketVisual.position;
            var currentRotation = racketVisual.rotation;

            var velocity = (currentPosition - previousPosition) / deltaTime;

            var deltaRotation = currentRotation * Quaternion.Inverse(previousRotation);
            float angle;
            Vector3 axis;
            deltaRotation.ToAngleAxis(out angle, out axis);

            if (angle > 180f) angle -= 360f;

            Vector3 angularVelocity = Vector3.zero;
            if (axis.magnitude > Mathf.Epsilon)
            {
                angularVelocity = (angle * Mathf.Deg2Rad / deltaTime) * axis.normalized;
            }

            paddleSetup.Paddle.Position = currentPosition;
            paddleSetup.Paddle.Rotation = currentRotation;
            paddleSetup.Paddle.Velocity = velocity;
            paddleSetup.Paddle.AngularVelocity = angularVelocity;

            previousPosition = currentPosition;
            previousRotation = currentRotation;
            lastUpdateTime = currentTime;
        }
    }
}