using DG.Tweening;
using UnityEngine;

namespace StepUpTableTennis
{
    public class MetaVRPositionAdjuster : MonoBehaviour
    {
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Transform centerEyeAnchor;
        [SerializeField] private Transform targetPosition;
        [SerializeField] private float requiredHoldTime = 1f;
        [SerializeField] private float adjustmentDuration = 0.2f;
        private float holdStartTime;
        private bool isAdjusting;

        private void Update()
        {
            if (OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch))
            {
                if (holdStartTime == 0f)
                    holdStartTime = Time.time;

                if (!isAdjusting && Time.time - holdStartTime >= requiredHoldTime)
                    AdjustPosition();
            }
            else
            {
                holdStartTime = 0f;
                isAdjusting = false;
            }
        }

        private void AdjustPosition()
        {
            isAdjusting = true;

            // centerEyeAnchorとtargetPositionの差分を計算（Y軸を除く）
            var eyePos = new Vector3(centerEyeAnchor.position.x, 0, centerEyeAnchor.position.z);
            var targetPos = new Vector3(targetPosition.position.x, 0, targetPosition.position.z);
            var offset = targetPos - eyePos;

            // cameraRigの現在位置に差分を適用
            var newRigPosition = cameraRig.position + offset;
            newRigPosition.y = cameraRig.position.y; // Y軸は維持

            // 移動と回転を実行
            cameraRig.DOMove(newRigPosition, adjustmentDuration).SetEase(Ease.OutQuad);

            var rotationDiff = Mathf.DeltaAngle(centerEyeAnchor.eulerAngles.y, targetPosition.eulerAngles.y);
            var newRotation = cameraRig.eulerAngles;
            newRotation.y += rotationDiff;
            cameraRig.DORotate(newRotation, adjustmentDuration).SetEase(Ease.OutQuad);
        }
    }
}