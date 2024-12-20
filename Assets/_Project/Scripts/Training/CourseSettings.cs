using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StepUpTableTennis.Training
{
    [Serializable]
    public class CourseSettings
    {
        [Header("Launch Settings")] [SerializeField]
        public bool[] LaunchEnabled = new bool[3] { true, true, true };

        [SerializeField] public float[] LaunchWeights = new float[3] { 33.3f, 33.4f, 33.3f };

        [Header("Bounce Settings")] [SerializeField]
        public bool[] BounceEnabled = new bool[9]
        {
            true, true, true, // 手前
            true, true, true, // 中央
            true, true, true // 奥
        };

        [SerializeField] public float[] BounceWeights = new float[9]
        {
            11.1f, 11.1f, 11.1f, // 手前
            11.1f, 11.1f, 11.1f, // 中央
            11.1f, 11.1f, 11.1f // 奥
        };

        // 2次元配列としてアクセスするためのプロパティ
        public bool[,] BounceEnabled2D
        {
            get
            {
                var result = new bool[3, 3];
                for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    result[i, j] = BounceEnabled[i * 3 + j];
                return result;
            }
        }

        public float[,] BounceWeights2D
        {
            get
            {
                var result = new float[3, 3];
                for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                    result[i, j] = BounceWeights[i * 3 + j];
                return result;
            }
        }

        // バウンド目標位置を計算
        public Vector3 GetBounceTargetPosition(Vector3 center, float width, float length, float ballRadius, int row,
            int col)
        {
            var halfLength = length * 0.5f;
            var halfWidth = width * 0.5f;
            var segmentZ = halfLength / 3f; // テーブルを3分割
            var zOffset = -row * segmentZ - halfLength * 0.4f; // プレイヤー側に少し寄せる
            var segmentX = halfWidth / 1.5f;
            var xOffset = (col - 1) * segmentX;

            var targetPos = center + new Vector3(
                xOffset,
                ballRadius + center.y - 0.71f, // テーブル高さの調整
                zOffset
            );

            return targetPos;
        }

        // バウンド位置のインデックスを選択
        public (int row, int col) SelectBounceIndex()
        {
            var candidates = new List<(int r, int c, float w)>();
            for (var r = 0; r < 3; r++)
            for (var c = 0; c < 3; c++)
                if (BounceEnabled[r * 3 + c])
                    candidates.Add((r, c, BounceWeights[r * 3 + c]));

            var total = 0f;
            foreach (var c in candidates)
                total += c.w;

            if (total <= 0f)
                return (1, 1); // デフォルトは中央

            var rand = Random.value * total;
            var acc = 0f;
            foreach (var e in candidates)
            {
                acc += e.w;
                if (rand <= acc)
                    return (e.r, e.c);
            }

            return (1, 1); // デフォルトは中央
        }

        // 発射位置のインデックスを選択
        public int SelectLaunchIndex()
        {
            var candidates = new List<(int i, float w)>();
            for (var i = 0; i < 3; i++)
                if (LaunchEnabled[i])
                    candidates.Add((i, LaunchWeights[i]));

            var total = 0f;
            foreach (var c in candidates)
                total += c.w;

            if (total <= 0f)
                return 1; // デフォルトは中央

            var rand = Random.value * total;
            var acc = 0f;
            foreach (var e in candidates)
            {
                acc += e.w;
                if (rand <= acc)
                    return e.i;
            }

            return 1; // デフォルトは中央
        }

#if UNITY_EDITOR
        // エディタ用のセル描画メソッド
        private static bool DrawEnabledCell(Rect rect, bool value)
        {
            return EditorGUI.Toggle(rect, value);
        }

        private static float DrawWeightCell(Rect rect, float value)
        {
            return EditorGUI.FloatField(rect, value);
        }
#endif
    }
}