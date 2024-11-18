using StepUpTableTennis.TableTennisEngine.Collisions.System;

namespace StepUpTableTennis.TableTennisEngine.Collisions.Events
{
    public class CollisionEventArgs
    {
        public CollisionEventArgs(CollisionInfo collisionInfo)
        {
            CollisionInfo = collisionInfo;
        }

        public CollisionInfo CollisionInfo { get; }
    }
}