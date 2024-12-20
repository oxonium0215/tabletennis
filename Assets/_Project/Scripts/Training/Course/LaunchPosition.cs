using UnityEngine;
using Random = UnityEngine.Random;

namespace StepUpTableTennis.Training.Course
{
    public class LaunchPosition
    {
        private const int POSITIONS = 3; // 左、中央、右
        
        private readonly bool[] enabledPositions = new bool[POSITIONS];
        private readonly float[] weights = new float[POSITIONS];
        
        public float Spacing { get; }
        public Vector3 BasePosition { get; }

        public LaunchPosition(Vector3 basePosition, float spacing)
        {
            BasePosition = basePosition;
            Spacing = spacing;
            
            // デフォルトですべての位置を有効化
            for (int i = 0; i < POSITIONS; i++)
            {
                enabledPositions[i] = true;
                weights[i] = 1.0f;
            }
        }

        public void SetPositionEnabled(int index, bool enabled)
        {
            if (!IsValidIndex(index)) return;
            enabledPositions[index] = enabled;
        }

        public void SetPositionWeight(int index, float weight)
        {
            if (!IsValidIndex(index)) return;
            weights[index] = Mathf.Max(0f, weight);
        }

        public Vector3 GetRandomPosition()
        {
            // 有効な位置の合計重みを計算
            float totalWeight = 0f;
            for (int i = 0; i < POSITIONS; i++)
            {
                if (enabledPositions[i])
                    totalWeight += weights[i];
            }

            if (totalWeight <= 0f)
                return BasePosition; // フォールバック：中央位置を返す

            // 重みに基づいてランダムな位置を選択
            float randomValue = Random.value * totalWeight;
            float accumWeight = 0f;

            for (int i = 0; i < POSITIONS; i++)
            {
                if (!enabledPositions[i]) continue;
                
                accumWeight += weights[i];
                if (randomValue <= accumWeight)
                {
                    return GetPositionAtIndex(i);
                }
            }

            return BasePosition; // フォールバック
        }

        private Vector3 GetPositionAtIndex(int index)
        {
            float offset = (index - 1) * Spacing; // 中央を0とし、左がマイナス、右がプラス
            return BasePosition + new Vector3(offset, 0f, 0f);
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < POSITIONS;
        }
    }
}