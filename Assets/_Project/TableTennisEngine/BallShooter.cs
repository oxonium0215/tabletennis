using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine
{
    public class BallShooter
    {
        private readonly TrajectoryOptimizer optimizer;
        private readonly PhysicsSettings settings;

        public BallShooter(PhysicsSettings settings)
        {
            this.settings = settings;
            optimizer = new TrajectoryOptimizer(settings);
        }

        public void ExecuteShot(PhysicsSimulationManager manager, ShotParameters parameters)
        {
            var optimalShot = optimizer.CalculateOptimalShot(
                parameters.StartPosition,
                parameters.TargetPosition,
                parameters.Speed,
                parameters.Spin
            );

            manager.ResetBall(new PhysicsSimulationManager.BallResetSettings(
                parameters.StartPosition,
                optimalShot.Velocity,
                optimalShot.Spin
            ));

            Debug.Log(
                $"Shot executed - Velocity: {optimalShot.Velocity:F3}, Spin: {optimalShot.Spin:F3}, Expected Error: {optimalShot.Error:F4}m");
        }

        public float GetEstimatedError(ShotParameters parameters)
        {
            var optimalShot = optimizer.CalculateOptimalShot(
                parameters.StartPosition,
                parameters.TargetPosition,
                parameters.Speed,
                parameters.Spin
            );
            return optimalShot.Error;
        }

        public (Vector3 position, Vector3 velocity)[] PredictTrajectory(ShotParameters parameters)
        {
            var optimalShot = optimizer.CalculateOptimalShot(
                parameters.StartPosition,
                parameters.TargetPosition,
                parameters.Speed,
                parameters.Spin
            );

            return optimizer.GetTrajectoryPoints(
                parameters.StartPosition,
                optimalShot.Velocity,
                optimalShot.Spin,
                0.016f, // 60FPSでの表示用
                100 // 最大ポイント数
            );
        }

        public class ShotParameters
        {
            public Vector3 StartPosition { get; set; }
            public Vector3 TargetPosition { get; set; }
            public float Speed { get; set; }
            public Vector3 Spin { get; set; }
        }

        public static class ShotPresets
        {
            public static ShotParameters TopSpin => new()
            {
                StartPosition = new Vector3(0, 1f, 1.8f),
                TargetPosition = new Vector3(0, 0.76f, -1.3f),
                Speed = 15f,
                Spin = new Vector3(100f, 0, 0)
            };

            public static ShotParameters BackSpin => new()
            {
                StartPosition = new Vector3(0, 1f, 1.8f),
                TargetPosition = new Vector3(0, 0.76f, -1.3f),
                Speed = 15f,
                Spin = new Vector3(-100f, 0, 0)
            };

            public static ShotParameters RightSpin => new()
            {
                StartPosition = new Vector3(0, 1f, 1.8f),
                TargetPosition = new Vector3(0, 0.76f, -1.3f),
                Speed = 15f,
                Spin = new Vector3(0, 100f, 0)
            };

            public static ShotParameters LeftSpin => new()
            {
                StartPosition = new Vector3(0, 1f, 1.8f),
                TargetPosition = new Vector3(0, 0.76f, -1.3f),
                Speed = 15f,
                Spin = new Vector3(0, -100f, 0)
            };
        }
    }
}