using System.Threading.Tasks;
using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis.TableTennisEngine.Core.Interfaces
{
    public interface IShotManager
    {
        bool HasPendingShots { get; }
        Task<ShotParameters> CalculateTrajectoryAsync(ShotParameters parameters);
        void EnqueueShot(ShotParameters shot);
        void ExecuteNextShot(Ball ball);
        void ClearQueue();
        void ClearCache();
        Vector3[] GetPredictedPath(ShotParameters shot);
        float EvaluateTrajectoryAccuracy(ShotParameters shot);
    }
}