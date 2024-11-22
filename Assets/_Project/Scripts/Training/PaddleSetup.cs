using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis.Training
{
    public class PaddleSetup : MonoBehaviour
    {
        [SerializeField] private PhysicsSettings physicsSettings;
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private float lastUpdateTime;
        public Paddle Paddle { get; private set; }

        private void Awake()
        {
            Paddle = new Paddle();
            UpdateState();
        }

        private void Update()
        {
            UpdateState();
        }

        private void UpdateState()
        {
            var currentTime = Time.time;
            var deltaTime = currentTime - lastUpdateTime;

            var currentPosition = transform.position;
            var currentRotation = transform.rotation;

            // 位置と回転の更新
            Paddle.Position = currentPosition;
            Paddle.Rotation = currentRotation;

            // 速度と角速度の計算（deltaTimeが有効な場合のみ）
            if (deltaTime > 0)
            {
                Paddle.Velocity = (currentPosition - lastPosition) / deltaTime;

                var rotationDelta = currentRotation * Quaternion.Inverse(lastRotation);
                float angle;
                Vector3 axis;
                rotationDelta.ToAngleAxis(out angle, out axis);
                if (angle > 180f) angle -= 360f;
                Paddle.AngularVelocity = angle * Mathf.Deg2Rad / deltaTime * axis.normalized;
            }

            // 状態の保存
            lastPosition = currentPosition;
            lastRotation = currentRotation;
            lastUpdateTime = currentTime;
        }
    }
}