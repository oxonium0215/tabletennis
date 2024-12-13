using System;
using System.Linq;
using UnityEngine;

namespace StepUpTableTennis.Training
{
    [Serializable]
    public class CourseSettings
    {
        // 上段(発射ポジション)の3レーン設定
        public bool[] LaunchEnabled = new bool[3] { true, true, true };
        public float[] LaunchWeights = new float[3] { 50f, 50f, 50f };

        // 下段(バウンドエリア)3x3合計9領域
        // 配置は(プレイヤー視点)
        // 左上(0), 中上(1), 右上(2)
        // 左中(3), 中中(4), 右中(5)
        // 左下(6), 中下(7), 右下(8)
        public bool[] BounceEnabled = new bool[9] 
        { 
            true,true,true,
            true,true,true,
            true,true,true
        };

        public float[] BounceWeights = new float[9]
        {
            50f,50f,50f,
            50f,50f,50f,
            50f,50f,50f
        };

        /// <summary>
        /// 有効な要素の重みに基づいてインデックスをランダム選択
        /// </summary>
        private int SelectIndexByWeight(bool[] enabledArray, float[] weightArray)
        {
            float total = 0f;
            for (int i = 0; i < weightArray.Length; i++)
            {
                if (enabledArray[i])
                    total += weightArray[i];
            }

            if (total <= 0f)
            {
                // 全て無効、または全ウェイト0ならランダムに選ぶ
                var validIndices = enabledArray.Select((b, idx) => (b, idx)).Where(x => x.b).Select(x => x.idx).ToArray();
                if (validIndices.Length == 0)
                {
                    // 全部無効ならとりあえず0を返す
                    return 0;
                }
                return validIndices[UnityEngine.Random.Range(0, validIndices.Length)];
            }

            float r = UnityEngine.Random.Range(0f, total);
            float cumulative = 0f;
            for (int i = 0; i < weightArray.Length; i++)
            {
                if (!enabledArray[i]) continue;
                cumulative += weightArray[i];
                if (r <= cumulative)
                    return i;
            }

            return weightArray.Length - 1; // 保険
        }

        /// <summary>
        /// 発射位置（3レーン）からランダム選択してVector3補正を返す
        /// 発射基準位置から左右にオフセットするなどの処理をする想定
        /// </summary>
        public Vector3 GetLaunchOffset()
        {
            int index = SelectIndexByWeight(LaunchEnabled, LaunchWeights);
            // index: 0→左、1→中央、2→右 とする
            // ここでは例として0→-0.6m,1→0m,2→+0.6mオフセット
            float offsetX = 0f;
            if (index == 0) offsetX = -0.6f;
            else if (index == 2) offsetX = 0.6f;

            return new Vector3(offsetX, 0f, 0f);
        }

        /// <summary>
        /// テーブル上の3x3エリアからランダムにコースターゲットを決める
        /// ここではテーブルの中心を(0,0,0)として、±幅方向、±奥行で分割するイメージ
        /// 一般には TableSetupやballLauncher情報からテーブル上の座標を受け取り、その上でグリッド分割する
        /// </summary>
        public Vector3 GetBounceTargetPosition(Vector3 tableCenter, Vector3 tableForward, Vector3 tableRight, Vector2 tableSize)
        {
            int index = SelectIndexByWeight(BounceEnabled, BounceWeights);

            // 3x3グリッド上での行列インデックス算出
            // index=0:左上,1:中上,2:右上
            // index=3:左中...以下同様
            int row = index / 3; // 0=上段,1=中段,2=下段
            int col = index % 3; // 0=左,1=中,2=右

            // テーブルはx方向がwidth、z方向がdepthとし、tableRightが幅方向(tableSize.x)、tableForwardが奥行方向(tableSize.z)とする
            // グリッド分割: 幅(X)を3分割、奥行(Z)を3分割し、それぞれのセル中心を計算
            float cellWidth = tableSize.x / 3f;
            float cellDepth = tableSize.y / 3f; 
            // ※tableSizeは (Length=2.74 along Z, Width=1.525 along X) などの場合もあるので要確認。
            // ここでは tableSize.xを幅、tableSize.yを奥行としているが、PhysicsSettingsでZが1.525fが奥行、Xが2.74fが長辺の場合があるので注意
            // ユーザ要件に従い、ここではtableSizeを(Vector2 width,depth)として扱う

            // 左上セルは x: -1*cellWidth, z: -1*cellDepth
            // col=0(左) => x方向は -cellWidth 〜 -(cellWidth/2)
            // ただしここでは中央中心を(0,0)として、左=-1、中=0、右=+1、上=-1、中=0、下=+1とする
            float offsetXFromCenter = (col - 1) * cellWidth;
            float offsetZFromCenter = (row - 1) * cellDepth;

            // テーブル中心を基準に、tableRight,tableForwardで位置を計算
            Vector3 targetPos = tableCenter + tableRight * offsetXFromCenter + tableForward * offsetZFromCenter;
            return targetPos;
        }
    }
}
