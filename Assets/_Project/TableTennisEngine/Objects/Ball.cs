using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects.Base;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Objects
{
    public enum ForceMode
    {
        Force, // 継続的な力
        Impulse // 瞬間的な力
    }

    public class Ball : PhysicsObject
    {
        private Vector3 accumulatedForces = Vector3.zero;
        private Vector3 accumulatedTorques = Vector3.zero;
        private PhysicsSettings settings;
        private Vector3 spin;

        public Vector3 Spin
        {
            get => spin;
            private set => spin = value;
        }

        public void Initialize(PhysicsSettings physicsSettings)
        {
            settings = physicsSettings;
        }

        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            if (mode == ForceMode.Force)
                accumulatedForces += force;
            else if (mode == ForceMode.Impulse) Velocity += force / settings.BallMass;
        }

        public void AddTorque(Vector3 torque, ForceMode mode = ForceMode.Force)
        {
            var momentOfInertia = 2f / 5f * settings.BallMass * settings.BallRadius * settings.BallRadius;

            if (mode == ForceMode.Force)
                accumulatedTorques += torque;
            else if (mode == ForceMode.Impulse) spin += torque / momentOfInertia;
        }

        public void ResetState(Vector3 newPosition, Vector3 newVelocity, Vector3 newSpin)
        {
            Position = newPosition; // 基底クラスのプロパティを使用
            Velocity = newVelocity; // 基底クラスのプロパティを使用
            spin = newSpin;
            Rotation = Quaternion.identity; // 基底クラスのプロパティを使用

            accumulatedForces = Vector3.zero;
            accumulatedTorques = Vector3.zero;
        }

        public override void UpdatePhysics(float deltaTime, PhysicsSettings settings)
        {
            // Apply accumulated forces
            Velocity += accumulatedForces / settings.BallMass * deltaTime;
            accumulatedForces = Vector3.zero;

            // Apply accumulated torques
            var momentOfInertia = 2f / 5f * settings.BallMass * settings.BallRadius * settings.BallRadius;
            spin += accumulatedTorques / momentOfInertia * deltaTime;
            accumulatedTorques = Vector3.zero;

            // Apply gravity
            AddForce(settings.Gravity * settings.BallMass);

            // Compute speed and cross-sectional area
            var speed = Velocity.magnitude;
            var area = Mathf.PI * settings.BallRadius * settings.BallRadius;

            // Air resistance
            var dragForce = -0.5f * settings.AirDensity * speed * speed *
                            settings.BallDragCoefficient * area * Velocity.normalized;
            AddForce(dragForce);

            // Magnus effect
            if (speed > 0f && spin.magnitude > 0f)
            {
                var spinParameter = settings.BallRadius * spin.magnitude / speed;
                var liftCoefficient = settings.LiftCoefficientSlope * spinParameter;
                var magnusForce = 0.5f * settings.AirDensity * speed * speed * area *
                                  liftCoefficient * Vector3.Cross(spin.normalized, Velocity.normalized);
                AddForce(magnusForce);
            }

            // Spin damping
            spin *= Mathf.Exp(-settings.SpinDampingCoefficient * deltaTime);

            // 位置の更新は基底クラスの実装を使用
            base.UpdatePhysics(deltaTime, settings);

            // 回転の更新
            var rotationDelta = Quaternion.Euler(spin * Mathf.Rad2Deg * deltaTime);
            Rotation *= rotationDelta;
        }

        public Vector3 GetAngularMomentum()
        {
            var momentOfInertia = 2f / 5f * settings.BallMass * settings.BallRadius * settings.BallRadius;
            return spin * momentOfInertia;
        }

        public float GetRotationalEnergy()
        {
            var momentOfInertia = 2f / 5f * settings.BallMass * settings.BallRadius * settings.BallRadius;
            return 0.5f * momentOfInertia * spin.sqrMagnitude;
        }

        public float GetKineticEnergy()
        {
            return 0.5f * settings.BallMass * Velocity.sqrMagnitude;
        }

        public float GetTotalEnergy()
        {
            return GetKineticEnergy() + GetRotationalEnergy();
        }
    }
}