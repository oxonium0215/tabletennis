using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StepUpTableTennis.Training
{
    [Serializable]
    public class DifficultySettings
    {
        [Range(1, 5)] public int SpeedLevel = 1;
        [Range(1, 5)] public int SpinLevel = 1;
        [Range(1, 5)] public int CourseLevel = 1;

        public float GetSpeedForLevel()
        {
            // スピードレベルに応じた値を返す（m/s）
            var baseSpeed = SpeedLevel switch
            {
                1 => Random.Range(18f, 22f), // 20 ±2 km/h
                2 => Random.Range(25f, 35f), // 30 ±5 km/h
                3 => Random.Range(30f, 50f), // 40 ±10 km/h
                4 => Random.Range(35f, 65f), // 50 ±15 km/h
                5 => Random.Range(40f, 80f), // 60 ±20 km/h
                _ => 20f
            };

            // km/h から m/s に変換
            return baseSpeed / 3.6f;
        }

        public float GetSpinForLevel()
        {
            // 回転レベルに応じた値を返す（rps - revolutions per second）
            return SpinLevel switch
            {
                1 => Random.Range(0f, 5f), // 0-5 rps
                2 => Random.Range(5f, 15f), // 5-15 rps
                3 => Random.Range(15f, 25f), // 15-25 rps
                4 => Random.Range(25f, 35f), // 25-35 rps
                5 => Random.Range(35f, 50f), // 35-50 rps
                _ => 0f
            };
        }

        public Vector2 GetCourseVariationForLevel()
        {
            // コースレベルに応じたばらつきの範囲を返す（メートル）
            return CourseLevel switch
            {
                1 => new Vector2(0.1f, 0.1f), // 小さなばらつき
                2 => new Vector2(0.2f, 0.2f), // やや小さなばらつき
                3 => new Vector2(0.3f, 0.3f), // 中程度のばらつき
                4 => new Vector2(0.4f, 0.4f), // やや大きなばらつき
                5 => new Vector2(0.5f, 0.5f), // 大きなばらつき
                _ => new Vector2(0.1f, 0.1f)
            };
        }
    }
}