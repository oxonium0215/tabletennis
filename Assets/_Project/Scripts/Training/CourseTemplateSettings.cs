namespace StepUpTableTennis.Training
{
    public class CourseTemplateSettings
    {
        // テンプレートの種類
        public enum CourseTemplate
        {
            ForehandOnly, // レベル1: フォアハンドのみ
            ForehandAndBackhand, // レベル2: フォア・バック
            ThreeWay, // レベル3: 左中右
            AllAreas, // レベル4: 全エリア
            Random // レベル5: ランダム配置
        }

        // レベルに応じたテンプレートを適用
        public static void ApplyTemplate(CourseSettings settings, int level)
        {
            var template = GetTemplateForLevel(level);
            ApplyLaunchSettings(settings, template);
            ApplyBounceSettings(settings, template);
        }

        private static CourseTemplate GetTemplateForLevel(int level)
        {
            return level switch
            {
                1 => CourseTemplate.ForehandOnly,
                2 => CourseTemplate.ForehandAndBackhand,
                3 => CourseTemplate.ThreeWay,
                4 => CourseTemplate.AllAreas,
                5 => CourseTemplate.Random,
                _ => CourseTemplate.ForehandOnly
            };
        }

        private static void ApplyLaunchSettings(CourseSettings settings, CourseTemplate template)
        {
            // Launch位置の有効/無効設定
            switch (template)
            {
                case CourseTemplate.ForehandOnly:
                    settings.LaunchEnabled = new[] { true, true, true };
                    settings.LaunchWeights = new[] { 33.3f, 33.4f, 33.3f };
                    break;

                case CourseTemplate.ForehandAndBackhand:
                    settings.LaunchEnabled = new[] { true, true, true };
                    settings.LaunchWeights = new[] { 33.3f, 33.4f, 33.3f };
                    break;

                case CourseTemplate.ThreeWay:
                case CourseTemplate.AllAreas:
                case CourseTemplate.Random:
                    settings.LaunchEnabled = new[] { true, true, true };
                    settings.LaunchWeights = new[] { 33.3f, 33.4f, 33.3f };
                    break;
            }
        }

        private static void ApplyBounceSettings(CourseSettings settings, CourseTemplate template)
        {
            var enabled = new bool[9];
            var weights = new float[9];

            switch (template)
            {
                case CourseTemplate.ForehandOnly:
                    // プレイヤーから見て右側（フォアハンド）のみ
                    enabled = new[]
                    {
                        false, false, true, // 手前
                        false, false, true, // 中央
                        false, false, true // 奥
                    };
                    weights = new[]
                    {
                        0f, 0f, 40f, // 手前
                        0f, 0f, 40f, // 中央
                        0f, 0f, 20f // 奥
                    };
                    break;

                case CourseTemplate.ForehandAndBackhand:
                    // プレイヤーから見て左右（フォア・バック）
                    enabled = new[]
                    {
                        true, false, true, // 手前
                        true, false, true, // 中央
                        true, false, true // 奥
                    };
                    weights = new[]
                    {
                        30f, 0f, 30f, // 手前
                        20f, 0f, 20f, // 中央
                        0f, 0f, 0f // 奥
                    };
                    break;

                case CourseTemplate.ThreeWay:
                    // プレイヤーから見て左中右
                    enabled = new[]
                    {
                        true, true, true, // 手前
                        true, true, true, // 中央
                        false, false, false // 奥
                    };
                    weights = new[]
                    {
                        25f, 30f, 25f, // 手前
                        10f, 10f, 10f, // 中央
                        0f, 0f, 0f // 奥
                    };
                    break;

                case CourseTemplate.AllAreas:
                    // 全エリア（手前重視）
                    enabled = new[]
                    {
                        true, true, true, // 手前
                        true, true, true, // 中央
                        true, true, true // 奥
                    };
                    weights = new[]
                    {
                        15f, 20f, 15f, // 手前
                        10f, 15f, 10f, // 中央
                        5f, 5f, 5f // 奥
                    };
                    break;

                case CourseTemplate.Random:
                    // 全エリア均等
                    enabled = new[]
                    {
                        true, true, true, // 手前
                        true, true, true, // 中央
                        true, true, true // 奥
                    };
                    weights = new[]
                    {
                        11.1f, 11.1f, 11.1f, // 手前
                        11.1f, 11.1f, 11.1f, // 中央
                        11.1f, 11.1f, 11.1f // 奥
                    };
                    break;
            }

            // 配列をCourseSettingsに適用
            for (var i = 0; i < 9; i++)
            {
                settings.BounceEnabled[i] = enabled[i];
                settings.BounceWeights[i] = weights[i];
            }
        }
    }
}