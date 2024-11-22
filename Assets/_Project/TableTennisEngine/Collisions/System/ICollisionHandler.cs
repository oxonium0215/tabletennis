using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using StepUpTableTennis.TableTennisEngine.Objects.Base;

namespace StepUpTableTennis.TableTennisEngine.Collisions.System
{
    public interface ICollisionHandler
    {
        bool DetectCollision(Ball ball, PhysicsObject target, PhysicsSettings settings, out CollisionInfo info);
        void ResolveCollision(CollisionInfo info, PhysicsSettings settings);
    }
}