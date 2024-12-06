using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis.Training
{
    public class PaddleStateHandler : MonoBehaviour
    {
        [SerializeField] private PhysicsSettings physicsSettings;
        public Paddle Paddle { get; private set; }

        private void Awake()
        {
            Paddle = new Paddle();
        }

        public void UpdatePaddleState(Vector3 position, Quaternion rotation,
            Vector3 velocity, Vector3 angularVelocity)
        {
            Paddle.Position = position;
            Paddle.Rotation = rotation;
            Paddle.Velocity = velocity;
            Paddle.AngularVelocity = angularVelocity;
        }
    }
}