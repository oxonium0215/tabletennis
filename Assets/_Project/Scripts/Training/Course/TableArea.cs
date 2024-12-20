using UnityEngine;
using Random = UnityEngine.Random;

namespace StepUpTableTennis.Training.Course
{
    public class TableArea
    {
        private const int ROWS = 3;
        private const int COLS = 3;
        
        private readonly bool[,] enabledAreas = new bool[ROWS, COLS];
        private readonly float[,] weights = new float[ROWS, COLS];
        
        public Vector2 Size { get; }
        public Vector3 Center { get; }

        public TableArea(Vector2 size, Vector3 center)
        {
            // Sizeは (X方向長さ, Z方向長さ) で受け取る前提
            // 例) Size.x = 2.74 (幅)、Size.y = 1.525 (奥行き)
            Size = size;   
            Center = center;
            
            // デフォルトで全エリア有効
            for (int r = 0; r < ROWS; r++)
            for (int c = 0; c < COLS; c++)
            {
                enabledAreas[r, c] = true;
                weights[r, c] = 1.0f;
            }
        }

        public void SetAreaEnabled(int row, int col, bool enabled)
        {
            if (!IsValidIndex(row, col)) return;
            enabledAreas[row, col] = enabled;
        }

        public void SetAreaWeight(int row, int col, float weight)
        {
            if (!IsValidIndex(row, col)) return;
            weights[row, col] = Mathf.Max(0f, weight);
        }

        public Vector3 GetRandomPosition(float ballRadius)
        {
            // 有効なエリアの合計重みを計算
            float totalWeight = 0f;
            for (int r = 0; r < ROWS; r++)
            for (int c = 0; c < COLS; c++)
            {
                if (enabledAreas[r, c])
                    totalWeight += weights[r, c];
            }

            if (totalWeight <= 0f)
                return GetPositionOnTableSurface(ballRadius, Center); // 有効エリアが無ければ中央上面

            // 重みに応じたランダム選択
            float randomValue = Random.value * totalWeight;
            float accumWeight = 0f;

            for (int r = 0; r < ROWS; r++)
            for (int c = 0; c < COLS; c++)
            {
                if (!enabledAreas[r, c]) continue;
                
                accumWeight += weights[r, c];
                if (randomValue <= accumWeight)
                {
                    return GetPositionInArea(r, c, ballRadius);
                }
            }

            // フォールバック
            return GetPositionOnTableSurface(ballRadius, Center);
        }

        public bool IsPositionValid(Vector3 position, float ballRadius)
        {
            var halfSize = Size * 0.5f;
            var localPos = position - Center;

            // テーブル境界チェック（XとZ）
            if (Mathf.Abs(localPos.x) > halfSize.x - ballRadius) return false;
            if (Mathf.Abs(localPos.z) > halfSize.y - ballRadius) return false;

            // 対応するエリアが有効かどうか
            var (row, col) = GetAreaIndices(position);
            return IsValidIndex(row, col) && enabledAreas[row, col];
        }

        private Vector3 GetPositionInArea(int row, int col, float ballRadius)
        {
            var halfSize = Size * 0.5f;
            // テーブルをCOLS×ROWSに等分
            float segmentWidth = Size.x / COLS;    // X方向1区画幅
            float segmentLength = Size.y / ROWS;   // Z方向1区画長さ

            // 各区画内でボール半径分を考慮してランダム位置を決定
            float minX = -halfSize.x + col * segmentWidth + ballRadius;
            float maxX = minX + segmentWidth - ballRadius * 2;
            float minZ = -halfSize.y + row * segmentLength + ballRadius;
            float maxZ = minZ + segmentLength - ballRadius * 2;

            float randomX = Random.Range(minX, maxX);
            float randomZ = Random.Range(minZ, maxZ);

            return GetPositionOnTableSurface(ballRadius, Center + new Vector3(randomX, 0f, randomZ));
        }

        private Vector3 GetPositionOnTableSurface(float ballRadius, Vector3 position)
        {
            // テーブル厚み0.065m、Center.yはテーブル中央高さ0.727mとすると
            // テーブル上面 = Center.y + 0.065/2 = 0.727 + 0.0325 = 0.7595m
            // そこにボール半径0.02mを加えると 0.7795m
            float tableThickness = 0.065f; 
            float tableTop = Center.y + (tableThickness * 0.5f); // = 0.7595
            float finalY = tableTop + ballRadius; // = 0.7795
            return new Vector3(position.x, finalY, position.z);
        }

        private (int row, int col) GetAreaIndices(Vector3 position)
        {
            var localPos = position - Center;
            var halfSize = Size * 0.5f;

            float segmentWidth = Size.x / COLS;  
            float segmentLength = Size.y / ROWS; 

            // 左下を(0,0)起点にするために +halfSize
            float normalizedX = localPos.x + halfSize.x; // 0～Size.x
            float normalizedZ = localPos.z + halfSize.y; // 0～Size.y

            int col = Mathf.FloorToInt(normalizedX / segmentWidth);
            int row = Mathf.FloorToInt(normalizedZ / segmentLength);

            col = Mathf.Clamp(col, 0, COLS - 1);
            row = Mathf.Clamp(row, 0, ROWS - 1);

            return (row, col);
        }

        private bool IsValidIndex(int row, int col)
        {
            return row >= 0 && row < ROWS && col >= 0 && col < COLS;
        }
    }
}
