namespace StepUpTableTennis.TableTennisEngine.Collisions
{
    public interface ICollisionHandler
    {
        bool DetectCollision(Ball ball, PhysicsObject target, PhysicsSettings settings, out CollisionInfo info);
        void ResolveCollision(CollisionInfo info, PhysicsSettings settings);
    }
}