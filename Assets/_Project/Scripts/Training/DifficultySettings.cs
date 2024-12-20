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
        [Header("Difficulty Levels")] [Range(1, 7)] [Tooltip("Speed Level from 1 to 7")]
        public int SpeedLevel = 1;

        [Range(1, 5)] [Tooltip("Spin Level from 1 to 5")]
        public int SpinLevel = 1;

        public CourseSettings CourseSettings = new();
        private int courseLevel = 1;

        [Range(1, 5)]
        [Tooltip("Course Level from 1 to 5")]
        public int CourseLevel
        {
            get => courseLevel;
            set
            {
                if (courseLevel != value)
                {
                    courseLevel = value;
                    // コースレベルが変更されたらテンプレートを適用
                    CourseTemplateSettings.ApplyTemplate(CourseSettings, courseLevel);
                }
            }
        }

        /// <summary>
        ///     テーブル上でのボール速度(初速)を難易度レベルに基づいて算出
        /// </summary>
        public float GetSpeedForLevel()
        {
            var baseSpeed = SpeedLevel switch
            {
                1 => 18f, // 18 km/h
                2 => 21f, // 21 km/h
                3 => 24f, // 24 km/h
                4 => 27f, // 27 km/h
                5 => 30f, // 30 km/h
                6 => 33f, // 33 km/h
                7 => 36f, // 36 km/h
                _ => 21f
            };

            // km/h から m/s に換算
            return baseSpeed / 3.6f;
        }

        public SpinParameters GetSpinForLevel()
        {
            SpinType spinType;
            float rps;
            Vector3 axis;

            switch (SpinLevel)
            {
                case 1:
                    spinType = SpinType.TopSpin;
                    rps = Random.Range(0f, 10f);
                    axis = Vector3.right;
                    break;

                case 2:
                {
                    var rand2 = Random.value;
                    if (rand2 < 0.3f)
                        spinType = SpinType.NoSpin;
                    else if (rand2 < 0.7f)
                        spinType = SpinType.TopSpin;
                    else
                        spinType = SpinType.BackSpin;

                    rps = Random.Range(5f, 20f);
                    axis = spinType switch
                    {
                        SpinType.TopSpin => Vector3.right,
                        SpinType.BackSpin => -Vector3.right,
                        _ => Vector3.zero
                    };
                }
                    break;

                case 3:
                {
                    var rand3 = Random.value;
                    if (rand3 < 0.2f)
                        spinType = SpinType.NoSpin;
                    else if (rand3 < 0.5f)
                        spinType = SpinType.TopSpin;
                    else if (rand3 < 0.8f)
                        spinType = SpinType.BackSpin;
                    else
                        spinType = SpinType.SideSpin;

                    rps = Random.Range(15f, 35f);
                    axis = spinType switch
                    {
                        SpinType.TopSpin => Vector3.right,
                        SpinType.BackSpin => -Vector3.right,
                        SpinType.SideSpin => Vector3.forward,
                        _ => Vector3.zero
                    };
                }
                    break;

                case 4:
                {
                    var rand4 = Random.value;
                    if (rand4 < 0.1f)
                        spinType = SpinType.NoSpin;
                    else if (rand4 < 0.35f)
                        spinType = SpinType.TopSpin;
                    else if (rand4 < 0.6f)
                        spinType = SpinType.BackSpin;
                    else if (rand4 < 0.8f)
                        spinType = SpinType.SideSpin;
                    else
                        spinType = SpinType.ComplexSpin;

                    rps = Random.Range(25f, 45f);

                    if (spinType == SpinType.ComplexSpin)
                        axis = new Vector3(
                            Random.Range(-1f, 1f),
                            0,
                            Random.Range(-1f, 1f)
                        ).normalized;
                    else
                        axis = spinType switch
                        {
                            SpinType.TopSpin => Vector3.right,
                            SpinType.BackSpin => -Vector3.right,
                            SpinType.SideSpin => Vector3.forward,
                            _ => Vector3.zero
                        };
                }
                    break;

                case 5:
                {
                    var rand5 = Random.value;
                    if (rand5 < 0.05f)
                        spinType = SpinType.NoSpin;
                    else if (rand5 < 0.3f)
                        spinType = SpinType.TopSpin;
                    else if (rand5 < 0.55f)
                        spinType = SpinType.BackSpin;
                    else if (rand5 < 0.75f)
                        spinType = SpinType.SideSpin;
                    else
                        spinType = SpinType.ComplexSpin;

                    rps = Random.Range(35f, 60f);

                    if (spinType == SpinType.ComplexSpin)
                        axis = new Vector3(
                            Random.Range(-1f, 1f),
                            0,
                            Random.Range(-1f, 1f)
                        ).normalized;
                    else
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
            return CourseLevel switch
            {
                1 => new Vector2(0.1f, 0.1f),
                2 => new Vector2(0.2f, 0.2f),
                3 => new Vector2(0.3f, 0.3f),
                4 => new Vector2(0.4f, 0.4f),
                5 => new Vector2(0.5f, 0.5f),
                _ => new Vector2(0.1f, 0.1f)
            };
        }

        /// <summary>
        ///     ランチ(発射)位置オフセットをCourseSettingsから取得
        /// </summary>
        public Vector3 GetLaunchOffset()
        {
            var index = CourseSettings.SelectLaunchIndex();
            var offsetX = index switch
            {
                0 => -0.6f,
                2 => 0.6f,
                _ => 0f
            };
            return new Vector3(offsetX, 0, 0);
        }

        /// <summary>
        ///     バウンス地点をCourseSettingsから取得し、指定したテーブル中心・サイズ・ボール半径から座標計算
        /// </summary>
        public Vector3 GetRandomBounceTarget(Vector3 center, Vector2 tableSize, float ballRadius)
        {
            var width = tableSize.x;
            var length = tableSize.y;

            var (row, col) = CourseSettings.SelectBounceIndex();

            return CourseSettings.GetBounceTargetPosition(center, width, length, ballRadius, row, col);
        }

        // 初期化時にもテンプレートを適用するメソッドを追加
        public void Initialize()
        {
            CourseTemplateSettings.ApplyTemplate(CourseSettings, CourseLevel);
        }
    }
}