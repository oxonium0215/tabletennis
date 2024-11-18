using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Core
{
    [CreateAssetMenu(fileName = "PhysicsSettings", menuName = "TableTennis/Physics Settings")]
    public class PhysicsSettings : ScriptableObject
    {
        [Header("Simulation")] public Vector3 Gravity = new(0, -9.81f, 0);
        public float AirDensity = 1.204f;
        public float TimeStep = 1f / 240f;
        public int MaxSubsteps = 8;
        [Header("Ball")] public float BallMass = 0.0027f;
        public float BallRadius = 0.02f;
        public float BallDragCoefficient = 0.47f;
        public float SpinDampingCoefficient = 0.1f;
        public float LiftCoefficientSlope = 0.25f;
        [Header("Table")] public float TableRestitution = 0.8f;
        public float TableFriction = 0.2f;
        public Vector3 TableSize = new(2.74f, 0.076f, 1.525f);
        [Header("Paddle")] public float PaddleRestitution = 0.7f;
        public float PaddleFriction = 0.5f;
        public Vector2 PaddleSize = new(0.15f, 0.15f);
        public float PaddleThickness = 0.015f;
    }
}