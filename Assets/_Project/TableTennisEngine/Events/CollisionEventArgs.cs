namespace StepUpTableTennis.TableTennisEngine.Events
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