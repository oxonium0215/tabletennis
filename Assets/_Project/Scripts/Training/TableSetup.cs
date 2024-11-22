using StepUpTableTennis.TableTennisEngine.Core.Models;
using StepUpTableTennis.TableTennisEngine.Objects;
using UnityEngine;

namespace StepUpTableTennis.Training
{
    public class TableSetup : MonoBehaviour
    {
        [SerializeField] private PhysicsSettings physicsSettings;
        public Table Table { get; private set; }

        private void Awake()
        {
            // テーブルの初期化
            Table = new Table
            {
                Position = transform.position,
                Rotation = transform.rotation,
                Size = physicsSettings.TableSize
            };
        }
    }
}