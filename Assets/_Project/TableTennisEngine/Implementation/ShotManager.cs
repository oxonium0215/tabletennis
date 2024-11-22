using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Trajectory;

namespace StepUpTableTennis.TableTennisEngine.Implementation
{
    public class ShotManager : IShotManager
    {
        private readonly Queue<ShotParameters> pendingShots = new();
        private readonly IBallTrajectoryCalculator trajectoryCalculator;

        public ShotManager(IBallTrajectoryCalculator trajectoryCalculator)
        {
            this.trajectoryCalculator = trajectoryCalculator
                                        ?? throw new ArgumentNullException(nameof(trajectoryCalculator));
        }

        public bool HasPendingShots => pendingShots.Count > 0;

        public async Task<ShotParameters> CalculateTrajectoryAsync(ShotParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            return await trajectoryCalculator.CalculateTrajectoryAsync(parameters);
        }

        public void EnqueueShot(ShotParameters shot)
        {
            if (shot == null)
                throw new ArgumentNullException(nameof(shot));

            if (!shot.IsCalculated)
                throw new InvalidOperationException("Cannot enqueue shot without calculated trajectory");

            pendingShots.Enqueue(shot);
        }

        public void ExecuteNextShot(Ball ball)
        {
            if (ball == null)
                throw new ArgumentNullException(nameof(ball));

            if (!HasPendingShots)
                throw new InvalidOperationException("No pending shots available");

            var nextShot = pendingShots.Dequeue();

            ball.ResetState(
                nextShot.LaunchPosition,
                nextShot.InitialVelocity.Value,
                nextShot.InitialAngularVelocity.Value
            );
        }

        public void ClearQueue()
        {
            pendingShots.Clear();
        }
    }

    public interface IShotManager
    {
        bool HasPendingShots { get; }
        Task<ShotParameters> CalculateTrajectoryAsync(ShotParameters parameters);
        void EnqueueShot(ShotParameters shot);
        void ExecuteNextShot(Ball ball);
        void ClearQueue();
    }
}