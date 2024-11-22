using UnityEngine;

namespace StepUpTableTennis.Training
{
    public class BallLauncher : MonoBehaviour
    {
        [SerializeField] private Transform targetAreaVisualizer;
        [SerializeField] private Vector2 targetAreaSize = new(1f, 0.5f); // 幅とと奥行き
        [SerializeField] private bool showGizmos = true;

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            // 発射位置を表示
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.02f);

            // 発射方向を表示
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);

            // ターゲットエリアを表示
            if (targetAreaVisualizer != null)
            {
                Gizmos.color = Color.green;
                var targetPos = targetAreaVisualizer.position;
                Gizmos.matrix = Matrix4x4.TRS(
                    targetPos,
                    targetAreaVisualizer.rotation,
                    Vector3.one
                );
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(targetAreaSize.x, 0.01f, targetAreaSize.y));
            }
        }

        // BallSpawner用のアクセサ
        public Vector3 GetRandomTargetPosition()
        {
            if (targetAreaVisualizer == null)
                return transform.position + transform.forward;

            var centerPos = targetAreaVisualizer.position;
            var randomOffset = new Vector3(
                Random.Range(-targetAreaSize.x * 0.5f, targetAreaSize.x * 0.5f),
                0,
                Random.Range(-targetAreaSize.y * 0.5f, targetAreaSize.y * 0.5f)
            );

            return centerPos + randomOffset;
        }
    }
}