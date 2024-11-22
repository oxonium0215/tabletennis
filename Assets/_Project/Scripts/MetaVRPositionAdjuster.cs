using UnityEngine;
namespace StepUpTableTennis
{

    public class MetaVRPositionAdjuster : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraRig; // OVRCameraRigのルート
        [SerializeField] private Transform centerEyeAnchor; // CenterEyeAnchor
        
        [Header("Target Position Settings")]
        [SerializeField] private Transform targetPosition; // プレイヤーを移動させたい目標位置
        [SerializeField] private float adjustmentSpeed = 5f; // 位置調整の速度
        [SerializeField] private float rotationSpeed = 180f; // 回転調整の速度
        
        [Header("Button Settings")]
        [SerializeField] private float requiredHoldTime = 1f; // 必要な長押し時間（秒）
        [SerializeField] private bool showDebugLog = false; // デバッグログの表示設定

        private bool isAdjusting = false;
        private float holdStartTime = -1f;
        private bool isHolding = false;
        
        private void Update()
        {
            HandleInput();
            
            if (isAdjusting)
            {
                AdjustPosition();
                AdjustRotation();
            }
        }

        private void HandleInput()
        {
            bool isButtonPressed = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);

            // ボタンが押され始めた瞬間
            if (isButtonPressed && !isHolding)
            {
                holdStartTime = Time.time;
                isHolding = true;
                if (showDebugLog) Debug.Log("ボタン押し始め");
            }
            // ボタンが離された瞬間
            else if (!isButtonPressed && isHolding)
            {
                isHolding = false;
                isAdjusting = false;
                holdStartTime = -1f;
                if (showDebugLog) Debug.Log("ボタン離し");
            }

            // 長押し時間のチェック
            if (isHolding && !isAdjusting)
            {
                float holdDuration = Time.time - holdStartTime;
                
                // デバッグログ
                if (showDebugLog && holdDuration % 0.1f < Time.deltaTime)
                {
                    Debug.Log($"長押し中: {holdDuration:F1}秒");
                }

                // 必要な長押し時間に達した
                if (holdDuration >= requiredHoldTime)
                {
                    isAdjusting = true;
                    if (showDebugLog) Debug.Log("位置調整開始");
                }
            }
        }
        
        private void AdjustPosition()
        {
            // 現在のヘッドセット位置と目標位置の差分を計算（Y軸を除く）
            Vector3 currentPos = new Vector3(centerEyeAnchor.position.x, 0, centerEyeAnchor.position.z);
            Vector3 targetPos = new Vector3(targetPosition.position.x, 0, targetPosition.position.z);
            Vector3 positionDifference = targetPos - currentPos;
            
            // Camera Rigの位置を調整（Y軸は維持）
            if (positionDifference.magnitude > 0.01f)
            {
                Vector3 newPosition = cameraRig.position + (positionDifference * adjustmentSpeed * Time.deltaTime);
                newPosition.y = cameraRig.position.y; // Y軸の位置を維持
                cameraRig.position = newPosition;
            }
        }
        
        private void AdjustRotation()
        {
            // 現在のY軸回転と目標のY軸回転の差分を計算
            float currentYRotation = centerEyeAnchor.eulerAngles.y;
            float targetYRotation = targetPosition.eulerAngles.y;
            
            // 角度の差分を-180°から180°の範囲に正規化
            float rotationDifference = Mathf.DeltaAngle(currentYRotation, targetYRotation);
            
            // 回転の調整
            if (Mathf.Abs(rotationDifference) > 0.1f)
            {
                float rotationStep = Mathf.Sign(rotationDifference) * rotationSpeed * Time.deltaTime;
                Vector3 newRotation = cameraRig.eulerAngles;
                newRotation.y += rotationStep;
                cameraRig.eulerAngles = newRotation;
            }
        }

        private void OnDrawGizmos()
        {
            if (centerEyeAnchor != null && targetPosition != null)
            {
                // 現在の頭の位置
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(centerEyeAnchor.position, 0.1f);
                
                // 目標位置
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(targetPosition.position, 0.1f);
                
                // 両者を結ぶ線
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(centerEyeAnchor.position, targetPosition.position);
            }
        }
    }
}