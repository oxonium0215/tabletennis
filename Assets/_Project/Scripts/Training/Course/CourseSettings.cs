using UnityEngine;
using StepUpTableTennis.TableTennisEngine.Components;

namespace StepUpTableTennis.Training.Course
{
    public class CourseSettings
    {
        private readonly TableArea tableArea;
        private readonly LaunchPosition launchPosition;
        private readonly float ballRadius;

        public CourseSettings(
            BoxColliderComponent tableCollider, 
            Transform launcherTransform,
            float ballRadius,
            float launchSpacing = 0.6f)
        {
            this.ballRadius = ballRadius;

            // テーブルのサイズと中心位置を取得
            var tableSize = new Vector2(1.525f,1.2f) * 0.85f;
            var tableCenter = new Vector3(0, 0.727f, -0.7f);

            tableArea = new TableArea(tableSize, tableCenter);
            launchPosition = new LaunchPosition(launcherTransform.position, launchSpacing);
        }

        public Vector3 GetRandomBouncePosition()
        {
            return tableArea.GetRandomPosition(ballRadius);
        }

        public Vector3 GetLaunchPosition()
        {
            return launchPosition.GetRandomPosition();
        }

        public void SetTemplate(int level)
        {
            // レベルに応じてテンプレートを設定
            // 修正：全レベルでZマイナス側（row=0）のみ有効
            // 必要に応じて重みを変えることは可能だが、ここではシンプルにすべて有効・均等重みとする

            switch (level)
            {
                case 1: // レベル1: row=0（プレイヤー側）限定
                    SetupLevel1Template();
                    break;
                
                case 2: // レベル2: row=0限定
                    SetupLevel2Template();
                    break;
                
                case 3: // レベル3: row=0限定
                    SetupLevel3Template();
                    break;
                
                case 4: // レベル4: row=0限定
                    SetupLevel4Template();
                    break;
                
                case 5: // レベル5: row=0限定
                    SetupLevel5Template();
                    break;
                
                default:
                    SetupLevel1Template();
                    break;
            }
        }

        private void SetupLevel1Template()
        {
            // 全レベル共通でrow=0のみ有効化
            // ここでは例として中央列(c=1)に重みを高く、左右(c=0,2)は低めにしてみる
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                bool enabled = (r == 0); // row=0のみ有効
                float weight = 0f;
                if (enabled)
                {
                    // 例: 中央列に重みを高く
                    if (c == 2) weight = 1f; else weight = 0.1f;
                }
                tableArea.SetAreaEnabled(r, c, enabled);
                tableArea.SetAreaWeight(r, c, weight);
            }

            // 発射位置は均等に
            for (int i = 0; i < 3; i++)
            {
                launchPosition.SetPositionEnabled(i, true);
                launchPosition.SetPositionWeight(i, 1f);
            }
        }

        private void SetupLevel2Template()
        {
            // レベル2: row=0のみ有効、全列均等
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                bool enabled = (r == 0);
                float weight = enabled ? 1f : 0f;
                tableArea.SetAreaEnabled(r, c, enabled);
                tableArea.SetAreaWeight(r, c, weight);
            }

            // 発射位置は均等
            for (int i = 0; i < 3; i++)
            {
                launchPosition.SetPositionEnabled(i, true);
                launchPosition.SetPositionWeight(i, 1f);
            }
        }

        private void SetupLevel3Template()
        {
            // レベル3: row=0のみ有効、左右と中央で重み変化可
            // ここでは左右(c=0,2)に比べて中央(c=1)をやや重くする例
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                bool enabled = (r == 0);
                float weight = 0f;
                if (enabled)
                {
                    if (c == 1) weight = 1.0f; // 中央を重く
                    else weight = 0.8f;
                }
                tableArea.SetAreaEnabled(r, c, enabled);
                tableArea.SetAreaWeight(r, c, weight);
            }

            // 発射位置は均等
            for (int i = 0; i < 3; i++)
            {
                launchPosition.SetPositionEnabled(i, true);
                launchPosition.SetPositionWeight(i, 1f);
            }
        }

        private void SetupLevel4Template()
        {
            // レベル4: row=0のみ有効、均等な重み付け
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                bool enabled = (r == 0);
                float weight = enabled ? 1f : 0f;
                tableArea.SetAreaEnabled(r, c, enabled);
                tableArea.SetAreaWeight(r, c, weight);
            }

            // 発射位置は均等に
            for (int i = 0; i < 3; i++)
            {
                launchPosition.SetPositionEnabled(i, true);
                launchPosition.SetPositionWeight(i, 1f);
            }
        }

        private void SetupLevel5Template()
        {
            // レベル5: row=0のみ有効、全く同じ重み
            for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
            {
                bool enabled = (r == 0);
                float weight = enabled ? 1f : 0f;
                tableArea.SetAreaEnabled(r, c, enabled);
                tableArea.SetAreaWeight(r, c, weight);
            }

            // 発射位置も均等に
            for (int i = 0; i < 3; i++)
            {
                launchPosition.SetPositionEnabled(i, true);
                launchPosition.SetPositionWeight(i, 1f);
            }
        }
    }
}
