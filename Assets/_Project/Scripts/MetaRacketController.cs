// MetaRacketController.cs

using System.Collections;
using StepUpTableTennis.TableTennisEngine;
using StepUpTableTennis.TableTennisEngine.Collisions.Events;
using StepUpTableTennis.TableTennisEngine.Core;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis
{
    /// <summary>
    ///     VRコントローラーとラケットの物理的な挙動を管理するクラス
    /// </summary>
    public class MetaRacketController : MonoBehaviour
    {
        [Header("Core References")] [SerializeField]
        private PhysicsSimulationManager physicsManager;

        [SerializeField] private Transform gripPoint;
        [SerializeField] private Transform racketVisual;
        [SerializeField] private Transform racketHead;

        [Header("Controller Settings")] [SerializeField]
        private OVRInput.Controller controllerType = OVRInput.Controller.RTouch;

        [SerializeField] private bool useRelativeTransform = true;

        [Header("Transform Adjustments")] [SerializeField]
        private Vector3 gripOffset = Vector3.zero;

        [SerializeField] private Vector3 gripRotation = Vector3.zero;
        [SerializeField] private Vector3 paddleOffset;

        [Header("Motion Settings")] [SerializeField]
        private float velocitySmoothing = 0.1f;

        [SerializeField] private float angularVelocitySmoothing = 0.1f;

        [Header("Haptic Feedback")] [SerializeField]
        private float hitVibrationDuration = 0.05f;

        [SerializeField] private float hitVibrationFrequency = 0.3f;
        [SerializeField] private float hitVibrationAmplitude = 0.5f;
        private Paddle paddlePhysics;
        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 smoothedAngularVelocity;
        private Vector3 smoothedVelocity;
        private Transform trackingSpace;

        private void Start()
        {
            if (!ValidateReferences()) return;

            var ovrCameraRig = FindObjectOfType<OVRCameraRig>();
            if (ovrCameraRig != null)
                trackingSpace = ovrCameraRig.trackingSpace;

            InitializePaddle();
            SubscribeToEvents();
        }

        private void Update()
        {
            if (!ValidateReferences()) return;
            UpdateRacketTransform();
            UpdatePhysicsState();
            HandleInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (gripPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(gripPoint.position, 0.02f);
                Gizmos.DrawLine(gripPoint.position, gripPoint.position + gripPoint.forward * 0.05f);
            }

            if (racketHead != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(racketHead.position, 0.02f);

                var paddlePhysicsPos = GetPaddlePhysicsPosition();
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(paddlePhysicsPos, 0.02f);
                Gizmos.DrawLine(racketHead.position, paddlePhysicsPos);
            }
        }
#endif

        private bool ValidateReferences()
        {
            if (physicsManager == null)
            {
                Debug.LogError("[MetaRacketController] PhysicsManager reference is missing!", this);
                return false;
            }

            if (gripPoint == null)
            {
                Debug.LogError("[MetaRacketController] GripPoint reference is missing!", this);
                return false;
            }

            if (racketVisual == null)
            {
                Debug.LogError("[MetaRacketController] RacketVisual reference is missing!", this);
                return false;
            }

            if (racketHead == null)
            {
                Debug.LogError("[MetaRacketController] RacketHead reference is missing!", this);
                return false;
            }

            return true;
        }

        private void InitializePaddle()
        {
            paddlePhysics = new Paddle
            {
                Position = GetPaddlePhysicsPosition(),
                Rotation = racketHead.rotation
            };

            physicsManager.GetPhysicsEngine().AddPaddle(paddlePhysics);
            previousPosition = paddlePhysics.Position;
            previousRotation = paddlePhysics.Rotation;
        }

        private void UpdateRacketTransform()
        {
            if (trackingSpace == null) return;

            var position = OVRInput.GetLocalControllerPosition(controllerType);
            var rotation = OVRInput.GetLocalControllerRotation(controllerType);

            if (useRelativeTransform && trackingSpace != null)
            {
                position = trackingSpace.TransformPoint(position);
                rotation = trackingSpace.rotation * rotation;
            }

            gripPoint.position = position;
            gripPoint.rotation = rotation * Quaternion.Euler(gripRotation);
            gripPoint.position += gripPoint.TransformVector(gripOffset);

            if (racketVisual != null)
            {
                racketVisual.position = gripPoint.position;
                racketVisual.rotation = gripPoint.rotation;
            }

            if (paddlePhysics != null)
            {
                paddlePhysics.Position = GetPaddlePhysicsPosition();
                paddlePhysics.Rotation = racketHead.rotation;
            }
        }

        private Vector3 GetPaddlePhysicsPosition()
        {
            return racketHead.position + racketHead.TransformVector(paddleOffset);
        }

        private void UpdatePhysicsState()
        {
            var currentPosition = GetPaddlePhysicsPosition();
            var currentRotation = racketHead.rotation;

            var velocity = (currentPosition - previousPosition) / Time.deltaTime;
            smoothedVelocity = Vector3.Lerp(smoothedVelocity, velocity, velocitySmoothing);

            var angularVelocity = CalculateAngularVelocity(previousRotation, currentRotation);
            smoothedAngularVelocity = Vector3.Lerp(smoothedAngularVelocity, angularVelocity, angularVelocitySmoothing);

            if (paddlePhysics != null)
            {
                paddlePhysics.Velocity = smoothedVelocity;
                paddlePhysics.AngularVelocity = smoothedAngularVelocity;
            }

            previousPosition = currentPosition;
            previousRotation = currentRotation;
        }

        private void SubscribeToEvents()
        {
            if (physicsManager != null && physicsManager.GetPhysicsEngine() != null)
                physicsManager.GetPhysicsEngine().OnCollision += HandleCollision;
        }

        private void UnsubscribeFromEvents()
        {
            if (physicsManager != null && physicsManager.GetPhysicsEngine() != null)
                physicsManager.GetPhysicsEngine().OnCollision -= HandleCollision;
        }

        private void HandleInput()
        {
            // ここにボタン入力などの処理を追加
        }

        private void HandleCollision(CollisionEventArgs args)
        {
            if (args.CollisionInfo.Target == paddlePhysics)
            {
                var impactForce = args.CollisionInfo.GetImpactForce(physicsManager.GetPhysicsSettings());
                var intensity = Mathf.Clamp01(impactForce / 10f);

                OVRInput.SetControllerVibration(
                    hitVibrationFrequency * intensity,
                    hitVibrationAmplitude * intensity,
                    controllerType
                );

                StartCoroutine(StopVibrationAfterDelay(hitVibrationDuration));
            }
        }

        private IEnumerator StopVibrationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            OVRInput.SetControllerVibration(0, 0, controllerType);
        }

        private Vector3 CalculateAngularVelocity(Quaternion from, Quaternion to)
        {
            var deltaRotation = to * Quaternion.Inverse(from);
            deltaRotation.ToAngleAxis(out var angle, out var axis);

            if (float.IsInfinity(axis.x))
                return Vector3.zero;

            if (angle > 180f)
                angle -= 360f;

            return axis.normalized * (angle * Mathf.Deg2Rad / Time.deltaTime);
        }

        public Paddle GetPaddlePhysics()
        {
            return paddlePhysics;
        }
    }
}