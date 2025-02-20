using UnityEngine;

namespace StepUpTableTennis.Playback
{
    /// <summary>
    /// 左右の目の位置、視線方向、開閉度を視覚的に表示するコンポーネントです。
    /// ・各目の位置にスフィアを表示し、視線方向は LineRenderer で描画します。
    /// ・目の開閉度はスフィアの大きさおよび色で表現し、視線レイはサッカード状態に応じて色を変更します。
    /// </summary>
    public class GazeVisualizer : MonoBehaviour
    {
        [Header("Eye Visuals")]
        [Tooltip("左目の表示用スフィア（Prefabアセットの場合は実行時にインスタンス化されます）")]
        public GameObject leftEyeSphere;
        [Tooltip("右目の表示用スフィア（Prefabアセットの場合は実行時にインスタンス化されます）")]
        public GameObject rightEyeSphere;
        
        [Header("Gaze Ray Settings")]
        [Tooltip("左目の視線方向を描画するLineRenderer（Prefabアセットの場合は実行時にインスタンス化されます）")]
        public LineRenderer leftGazeLine;
        [Tooltip("右目の視線方向を描画するLineRenderer（Prefabアセットの場合は実行時にインスタンス化されます）")]
        public LineRenderer rightGazeLine;
        [Tooltip("視線レイの長さ")]
        public float gazeLineLength = 2.0f;
        
        [Header("Eye Appearance")]
        [Tooltip("目が開いているときのスケール")]
        public float openScale = 1.0f;
        [Tooltip("目が閉じているときのスケール")]
        public float closedScale = 0.3f;
        [Tooltip("目が開いているときの色")]
        public Color openColor = Color.white;
        [Tooltip("目が閉じているときの色")]
        public Color closedColor = Color.gray;
        
        [Header("Saccade Settings")]
        [Tooltip("サッカード時に使用する色（視線レイ用）")]
        public Color saccadeColor = Color.magenta;
        
        // サッカード状態の保持フラグ
        private bool currentSaccade = false;
        
        private void Awake()
        {
            // プレハブアセットの場合は、シーン上のインスタンスに置き換える
            if (leftEyeSphere != null && !leftEyeSphere.scene.IsValid())
            {
                leftEyeSphere = Instantiate(leftEyeSphere, transform);
            }
            if (rightEyeSphere != null && !rightEyeSphere.scene.IsValid())
            {
                rightEyeSphere = Instantiate(rightEyeSphere, transform);
            }
            if (leftGazeLine != null && !leftGazeLine.gameObject.scene.IsValid())
            {
                GameObject lrObj = Instantiate(leftGazeLine.gameObject, transform);
                leftGazeLine = lrObj.GetComponent<LineRenderer>();
            }
            if (rightGazeLine != null && !rightGazeLine.gameObject.scene.IsValid())
            {
                GameObject lrObj = Instantiate(rightGazeLine.gameObject, transform);
                rightGazeLine = lrObj.GetComponent<LineRenderer>();
            }
            
            // LineRenderer をワールド空間で描画するように設定
            if (leftGazeLine != null)
            {
                leftGazeLine.useWorldSpace = true;
            }
            if (rightGazeLine != null)
            {
                rightGazeLine.useWorldSpace = true;
            }
            
            if (leftEyeSphere == null)
            {
                Debug.LogError("Left Eye Sphere is not assigned.");
            }
            if (rightEyeSphere == null)
            {
                Debug.LogError("Right Eye Sphere is not assigned.");
            }
            if (leftGazeLine == null)
            {
                Debug.LogError("Left Gaze LineRenderer is not assigned.");
            }
            if (rightGazeLine == null)
            {
                Debug.LogError("Right Gaze LineRenderer is not assigned.");
            }
        }
        
        /// <summary>
        /// 視線データの更新を反映します。
        /// </summary>
        /// <param name="leftEyePos">左目の位置</param>
        /// <param name="leftEyeDir">左目の視線方向（正規化済み）</param>
        /// <param name="leftEyeClosed">左目の閉じ具合（0＝完全に開、1＝完全に閉）</param>
        /// <param name="rightEyePos">右目の位置</param>
        /// <param name="rightEyeDir">右目の視線方向（正規化済み）</param>
        /// <param name="rightEyeClosed">右目の閉じ具合（0＝完全に開、1＝完全に閉）</param>
        public void UpdateGazeData(Vector3 leftEyePos, Vector3 leftEyeDir, float leftEyeClosed,
                                   Vector3 rightEyePos, Vector3 rightEyeDir, float rightEyeClosed)
        {
            // 左目の更新
            if (leftEyeSphere != null)
            {
                leftEyeSphere.transform.position = leftEyePos;
                float leftScale = Mathf.Lerp(openScale, closedScale, leftEyeClosed);
                leftEyeSphere.transform.localScale = Vector3.one * leftScale;
                // 左目スフィアの色は通常の補間で設定
                SetGameObjectColor(leftEyeSphere, Color.Lerp(openColor, closedColor, leftEyeClosed));
            }
            
            // 右目の更新
            if (rightEyeSphere != null)
            {
                rightEyeSphere.transform.position = rightEyePos;
                float rightScale = Mathf.Lerp(openScale, closedScale, rightEyeClosed);
                rightEyeSphere.transform.localScale = Vector3.one * rightScale;
                SetGameObjectColor(rightEyeSphere, Color.Lerp(openColor, closedColor, rightEyeClosed));
            }
            
            // 視線レイの更新（LineRenderer の色も更新）
            if (leftGazeLine != null)
            {
                leftGazeLine.SetPosition(0, leftEyePos);
                leftGazeLine.SetPosition(1, leftEyePos + leftEyeDir * gazeLineLength);
                Color leftLineColor = currentSaccade ? saccadeColor : Color.Lerp(openColor, closedColor, leftEyeClosed);
                leftGazeLine.startColor = leftLineColor;
                leftGazeLine.endColor = leftLineColor;
            }
            
            if (rightGazeLine != null)
            {
                rightGazeLine.SetPosition(0, rightEyePos);
                rightGazeLine.SetPosition(1, rightEyePos + rightEyeDir * gazeLineLength);
                Color rightLineColor = currentSaccade ? saccadeColor : Color.Lerp(openColor, closedColor, rightEyeClosed);
                rightGazeLine.startColor = rightLineColor;
                rightGazeLine.endColor = rightLineColor;
            }
        }
        
        /// <summary>
        /// サッカード状態を設定します。
        /// </summary>
        /// <param name="isSaccade">サッカード中なら true</param>
        public void SetSaccadeState(bool isSaccade)
        {
            currentSaccade = isSaccade;
        }
        
        private void SetGameObjectColor(GameObject obj, Color color)
        {
            // 対象が LineRenderer なら専用処理
            var lineRenderer = obj.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                lineRenderer.startColor = color;
                lineRenderer.endColor = color;
                return;
            }
            
            // 通常の Renderer の場合
            var rend = obj.GetComponent<Renderer>();
            if (rend != null)
            {
                if (obj.scene.IsValid())
                {
                    if (rend.material == rend.sharedMaterial)
                    {
                        rend.material = Instantiate(rend.sharedMaterial);
                    }
                    rend.material.color = color;
                }
                else
                {
                    rend.sharedMaterial.color = color;
                }
            }
        }
    }
}
