using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace StepUpTableTennis.Training
{
    public enum SpinType
    {
        NoSpin,
        TopSpin,
        BackSpin,
        SideSpin,
        ComplexSpin
    }

    [Serializable]
    public class SpinParameters
    {
        public SpinType Type;
        public float RotationsPerSecond;
        public Vector3 SpinAxis;

        public SpinParameters(SpinType type, float rps, Vector3 axis)
        {
            Type = type;
            RotationsPerSecond = rps;
            SpinAxis = axis;
        }
    }

    [Serializable]
    public class DifficultySettings
    {
        [Range(1, 7)] public int SpeedLevel = 1;
        [Range(1, 5)] public int SpinLevel = 1;
        [Range(1, 5)] public int CourseLevel = 1;

        public float GetSpeedForLevel()
        {
            // スピードレベルに応じた値を返す（m/s）
            var baseSpeed = SpeedLevel switch
            {
                1 => Random.Range(18f, 18f),
                2 => Random.Range(20f, 20f),
                3 => Random.Range(22f, 22f),
                4 => Random.Range(24f, 24f),
                5 => Random.Range(26f, 26f),
                6 => Random.Range(28f, 28f),
                7 => Random.Range(40f, 40f),
                _ => 20f
            };

            // km/h から m/s に変換
            return baseSpeed / 3.6f;
        }

        public SpinParameters GetSpinForLevel()
        {
            SpinType spinType;
            float rps;
            Vector3 axis;

            switch (SpinLevel)
            {
                case 1: // 初級
                    spinType = SpinType.TopSpin;
                    rps = Random.Range(0f, 10f);
                    axis = Vector3.right;
                    break;

                case 2: // 初中級
                    float rand2 = Random.value;
                    if (rand2 < 0.3f) spinType = SpinType.NoSpin;
                    else if (rand2 < 0.7f) spinType = SpinType.TopSpin;
                    else spinType = SpinType.BackSpin;
                    rps = Random.Range(5f, 20f);
                    axis = spinType switch
                    {
                        SpinType.TopSpin => Vector3.right,
                        SpinType.BackSpin => -Vector3.right,
                        _ => Vector3.zero
                    };
                    break;

                case 3: // 中級
                    float rand3 = Random.value;
                    if (rand3 < 0.2f) spinType = SpinType.NoSpin;
                    else if (rand3 < 0.5f) spinType = SpinType.TopSpin;
                    else if (rand3 < 0.8f) spinType = SpinType.BackSpin;
                    else spinType = SpinType.SideSpin;
                    rps = Random.Range(15f, 35f);
                    axis = spinType switch
                    {
                        SpinType.TopSpin => Vector3.right,
                        SpinType.BackSpin => -Vector3.right,
                        SpinType.SideSpin => Vector3.forward,
                        _ => Vector3.zero
                    };
                    break;

                case 4: // 上級
                    float rand4 = Random.value;
                    if (rand4 < 0.1f) spinType = SpinType.NoSpin;
                    else if (rand4 < 0.35f) spinType = SpinType.TopSpin;
                    else if (rand4 < 0.6f) spinType = SpinType.BackSpin;
                    else if (rand4 < 0.8f) spinType = SpinType.SideSpin;
                    else spinType = SpinType.ComplexSpin;
                    rps = Random.Range(25f, 45f);
                    
                    if (spinType == SpinType.ComplexSpin)
                    {
                        axis = new Vector3(
                            Random.Range(-1f, 1f),
                            0,
                            Random.Range(-1f, 1f)
                        ).normalized;
                    }
                    else
                    {
                        axis = spinType switch
                        {
                            SpinType.TopSpin => Vector3.right,
                            SpinType.BackSpin => -Vector3.right,
                            SpinType.SideSpin => Vector3.forward,
                            _ => Vector3.zero
                        };
                    }
                    break;

                case 5: // 最上級
                    float rand5 = Random.value;
                    if (rand5 < 0.05f) spinType = SpinType.NoSpin;
                    else if (rand5 < 0.3f) spinType = SpinType.TopSpin;
                    else if (rand5 < 0.55f) spinType = SpinType.BackSpin;
                    else if (rand5 < 0.75f) spinType = SpinType.SideSpin;
                    else spinType = SpinType.ComplexSpin;
                    rps = Random.Range(35f, 60f);
                    
                    if (spinType == SpinType.ComplexSpin)
                    {
                        axis = new Vector3(
                            Random.Range(-1f, 1f),
                            0,
                            Random.Range(-1f, 1f)
                        ).normalized;
                    }
                    else
                    {
                        axis = spinType switch
                        {
                            SpinType.TopSpin => Vector3.right,
                            SpinType.BackSpin => -Vector3.right,
                            SpinType.SideSpin => Vector3.forward,
                            _ => Vector3.zero
                        };
                    }
                    break;

                default:
                    return new SpinParameters(SpinType.NoSpin, 0f, Vector3.zero);
            }

            return new SpinParameters(spinType, rps, axis);
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