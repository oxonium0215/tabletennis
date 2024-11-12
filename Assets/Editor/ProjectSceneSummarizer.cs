using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// ファイルを開くために使用

public class ProjectSceneSummarizer : EditorWindow
{
    private const int MaxRootObjects = 100; // 出力するルートオブジェクトの上限
    private const int MaxChildObjects = 7; // 各ルートオブジェクトの子オブジェクトの上限
    private static List<string> ignoreList = new();
    private List<string> excludePatterns = new();
    private string excludePatternsText = "";
    private List<string> includeExtensions = new();
    private string includeExtensionsText = "";

    // シーン解析用の変数
    private bool includeSceneSummary = true; // シーンのサマリーを出力するかどうか
    private long maxFileSize; // 0 は制限なし
    private int maxLinesPerFile = 1000;
    private string newProfileName = "";
    private string outputFileName = "ProjectSummary.md";

    // パターンプロファイルの管理
    private readonly Dictionary<string, PatternProfile> patternProfiles = new();
    private int previousSelectedProfileIndex = -1;
    private readonly List<string> profileNames = new();
    private Vector2 scrollPosition;
    private int selectedProfileIndex;
    private List<string> skipContentPatterns = new();
    private string skipContentPatternsText = "";

    // プロジェクトサマライザー用の変数
    private string targetDirectory = "Assets";

    private void OnEnable()
    {
        LoadPatternProfiles();
    }

    private void OnGUI()
    {
        // 全体をスクロール可能にする
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("プロジェクトサマライザーの設定", EditorStyles.boldLabel);

        // ターゲットディレクトリ
        GUILayout.Label("ターゲットディレクトリ（プロジェクトルートからの相対パス）:");
        targetDirectory = EditorGUILayout.TextField(targetDirectory);

        // 出力ファイル名
        GUILayout.Label("出力ファイル名:");
        outputFileName = EditorGUILayout.TextField(outputFileName);

        // 各ファイルの最大行数
        GUILayout.Label("各ファイルの最大行数:");
        maxLinesPerFile = EditorGUILayout.IntField(maxLinesPerFile);

        // 最大ファイルサイズ
        GUILayout.Label("最大ファイルサイズ（バイト、0は制限なし）:");
        maxFileSize = EditorGUILayout.LongField(maxFileSize);

        GUILayout.Space(10);

        // パターンプロファイルの選択
        GUILayout.Label("パターンプロファイルの選択・管理", EditorStyles.boldLabel);
        if (profileNames.Count == 0)
        {
            profileNames.Add("デフォルト");
            patternProfiles["デフォルト"] = new PatternProfile();
        }

        var newSelectedProfileIndex = EditorGUILayout.Popup("選択中のプロファイル", selectedProfileIndex, profileNames.ToArray());
        var selectedProfileName = profileNames[selectedProfileIndex];

        if (newSelectedProfileIndex != selectedProfileIndex)
        {
            // 現在の入力内容を保存
            SaveCurrentProfile();

            selectedProfileIndex = newSelectedProfileIndex;
            selectedProfileName = profileNames[selectedProfileIndex];

            // 新しいプロファイルの内容を読み込み
            LoadCurrentProfile();
        }

        // プロファイルの保存
        GUILayout.BeginHorizontal();
        newProfileName = EditorGUILayout.TextField("新しいプロファイル名", newProfileName);
        if (GUILayout.Button("プロファイルを保存"))
            if (!string.IsNullOrEmpty(newProfileName))
            {
                SavePatternProfile(newProfileName);
                newProfileName = "";
            }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // 除外パターン入力
        GUILayout.Label("除外パターン（1行に1つ）:");
        excludePatternsText = EditorGUILayout.TextArea(excludePatternsText, GUILayout.Height(100));

        // スキップパターン入力
        GUILayout.Label("内容をスキップするパターン（1行に1つ）:");
        skipContentPatternsText = EditorGUILayout.TextArea(skipContentPatternsText, GUILayout.Height(100));

        // 含める拡張子入力
        GUILayout.Label("含める拡張子（1行に1つ、例：.cs、空白の場合はすべて）:");
        includeExtensionsText = EditorGUILayout.TextArea(includeExtensionsText, GUILayout.Height(100));

        // シーン解析の設定
        GUILayout.Space(10);
        GUILayout.Label("シーンサマライザーの設定", EditorStyles.boldLabel);

        // シーンのサマリーを出力するかどうか
        includeSceneSummary = EditorGUILayout.Toggle("シーンのサマリーを出力する", includeSceneSummary);

        // IgnoreList.txt のパスを表示
        if (includeSceneSummary) GUILayout.Label("IgnoreList.txt のパスは 'Assets/Editor/IgnoreList.txt' です。");

        // サマリー生成ボタン
        GUILayout.Space(10);
        if (GUILayout.Button("サマリーを生成"))
        {
            // パターンの更新
            excludePatterns = excludePatternsText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim()).ToList();
            skipContentPatterns = skipContentPatternsText
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).ToList();
            includeExtensions = includeExtensionsText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim()).ToList();

            // 選択中のプロファイルを更新して保存
            SaveCurrentProfile();

            GenerateSummary();
        }

        EditorGUILayout.EndScrollView();
    }

    [MenuItem("Tools/Project and Scene Summarizer")]
    public static void ShowWindow()
    {
        GetWindow<ProjectSceneSummarizer>("Project and Scene Summarizer");
    }

    private void GenerateSummary()
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var fullPath = Path.Combine(projectRoot, targetDirectory);
        var outputPath = Path.Combine(projectRoot, outputFileName);

        if (!Directory.Exists(fullPath))
        {
            EditorUtility.DisplayDialog("エラー", $"ターゲットディレクトリが存在しません: {fullPath}", "OK");
            return;
        }

        var outputLines = new List<string>();

        // プロジェクトのディレクトリ構造の生成
        outputLines.Add("# プロジェクトサマリー");
        outputLines.Add("```");
        GenerateDirectoryTree(fullPath, fullPath, outputLines);
        outputLines.Add("```");

        // ファイルの処理
        var allFiles = new List<string>();
        CollectAllFiles(fullPath, fullPath, allFiles);

        foreach (var filePath in allFiles)
        {
            var relativePath = GetRelativePath(filePath);
            outputLines.Add($"### {relativePath}");
            outputLines.Add("```");

            var skipContent = ShouldSkipContent(filePath);

            var fileContent = GetFileContents(filePath, skipContent);
            outputLines.Add(fileContent);
            outputLines.Add("```");
        }

        // シーンのサマリーを含める場合
        if (includeSceneSummary)
        {
            // 現在のシーンのデータを追加
            outputLines.Add("\n# シーンサマリー");

            // IgnoreList.txt を読み込む
            LoadIgnoreList();

            var scene = SceneManager.GetActiveScene();
            outputLines.Add($"## シーン名: {scene.name}\n");

            var rootObjects = scene.GetRootGameObjects()
                .Where(go => !ignoreList.Contains(go.name))
                .Take(MaxRootObjects)
                .ToList();

            // シーン構造をMarkdown形式で出力
            outputLines.Add("### シーン構造");
            foreach (var rootObject in rootObjects)
                AppendGameObjectStructure(outputLines, rootObject, 0, MaxChildObjects);
            if (scene.GetRootGameObjects().Length > MaxRootObjects) outputLines.Add("  ..."); // 省略表示

            // 各オブジェクトの詳細情報をMarkdown形式で出力
            outputLines.Add("\n### ゲームオブジェクトの詳細");
            foreach (var rootObject in rootObjects) AppendGameObjectDetails(outputLines, rootObject, MaxChildObjects);
        }

        // ファイルへの書き込み
        try
        {
            File.WriteAllText(outputPath, string.Join("\n", outputLines), Encoding.UTF8);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("サマリー生成完了", $"サマリーが生成されました: {outputPath}", "OK");

            // ファイルを開く
#if UNITY_EDITOR_WIN
            Process.Start(new ProcessStartInfo(outputPath) { UseShellExecute = true });
#elif UNITY_EDITOR_OSX
            Process.Start("open", outputPath);
#elif UNITY_EDITOR_LINUX
            Process.Start("xdg-open", outputPath);
#endif
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("エラー", $"出力ファイルへの書き込みに失敗しました: {e.Message}", "OK");
        }
    }

    // 以下、プロジェクトサマライザーのメソッド
    private void GenerateDirectoryTree(string rootPath, string currentPath, List<string> outputLines, int level = 0)
    {
        var indent = new string(' ', 4 * level);
        var dirName = Path.GetFileName(currentPath);
        outputLines.Add($"{indent}{dirName}/");

        // サブディレクトリの処理
        var directories = Directory.GetDirectories(currentPath);
        foreach (var directory in directories)
        {
            if (ShouldExclude(directory))
                continue;

            GenerateDirectoryTree(rootPath, directory, outputLines, level + 1);
        }

        // ファイルの処理
        var files = Directory.GetFiles(currentPath);
        foreach (var file in files)
        {
            if (ShouldExclude(file))
                continue;
            if (includeExtensions.Count > 0 && !includeExtensions.Contains(Path.GetExtension(file)))
                continue;

            var fileName = Path.GetFileName(file);
            var fileIndent = new string(' ', 4 * (level + 1));
            outputLines.Add($"{fileIndent}{fileName}");
        }
    }

    private void CollectAllFiles(string rootPath, string currentPath, List<string> allFiles)
    {
        // サブディレクトリの処理
        var directories = Directory.GetDirectories(currentPath);
        foreach (var directory in directories)
        {
            if (ShouldExclude(directory))
                continue;

            CollectAllFiles(rootPath, directory, allFiles);
        }

        // ファイルの処理
        var files = Directory.GetFiles(currentPath);
        foreach (var file in files)
        {
            if (ShouldExclude(file))
                continue;
            if (includeExtensions.Count > 0 && !includeExtensions.Contains(Path.GetExtension(file)))
                continue;

            allFiles.Add(file);
        }
    }

    private bool ShouldExclude(string path)
    {
        var relativePath = GetRelativePath(path);
        foreach (var pattern in excludePatterns)
            if (Regex.IsMatch(relativePath, WildcardToRegex(pattern)))
                return true;
        return false;
    }

    private bool ShouldSkipContent(string path)
    {
        var relativePath = GetRelativePath(path);
        foreach (var pattern in skipContentPatterns)
            if (Regex.IsMatch(relativePath, WildcardToRegex(pattern)))
                return true;
        return false;
    }

    private string GetRelativePath(string fullPath)
    {
        var projectRoot = Directory.GetParent(Application.dataPath).FullName;
        var relativePath = fullPath.Replace(projectRoot, "").Replace("\\", "/");
        if (relativePath.StartsWith("/"))
            relativePath = relativePath.Substring(1);
        return relativePath;
    }

    private string GetFileContents(string filePath, bool skipContent)
    {
        var fileInfo = new FileInfo(filePath);

        if (skipContent) return "(内容はスキップされました)";

        if (maxFileSize > 0 && fileInfo.Length > maxFileSize) return "(ファイルサイズが制限を超えています。内容はスキップされました)";

        try
        {
            var lines = File.ReadAllLines(filePath);
            if (lines.Length > maxLinesPerFile)
            {
                var limitedLines = new List<string>(lines).GetRange(0, maxLinesPerFile);
                limitedLines.Add("...");
                limitedLines.Add("(内容は省略されました)");
                return string.Join("\n", limitedLines);
            }

            return string.Join("\n", lines);
        }
        catch (Exception e)
        {
            Debug.LogError($"ファイルの読み込みエラー {filePath}: {e.Message}");
            return $"ファイルの読み込みエラー: {e.Message}";
        }
    }

    private string WildcardToRegex(string pattern)
    {
        return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
    }

    // 以下、シーンサマライザーのメソッド
    private void AppendGameObjectStructure(List<string> outputLines, GameObject gameObject, int depth, int maxChildren)
    {
        var indent = new string(' ', depth * 2);

        // 名前が "--->" で始まる場合、セクションヘッダーとして扱う
        if (gameObject.name.StartsWith("---> "))
        {
            var sectionName = gameObject.name.Replace("---> ", "").Trim();
            outputLines.Add($"\n### {sectionName}\n");
        }
        else
        {
            // 標準的なオブジェクトリスト
            outputLines.Add($"{indent}- {gameObject.name}");
        }

        var childObjects = gameObject.transform.Cast<Transform>()
            .Where(t => !ignoreList.Contains(t.name))
            .Take(maxChildren)
            .ToList();

        foreach (var child in childObjects)
            AppendGameObjectStructure(outputLines, child.gameObject, depth + 1, maxChildren);

        // 子オブジェクトが最大数を超える場合、省略表示
        if (gameObject.transform.childCount > maxChildren) outputLines.Add($"{indent}  ...");
    }

    private void AppendGameObjectDetails(List<string> outputLines, GameObject gameObject, int maxChildren)
    {
        outputLines.Add($"#### {gameObject.name}");
        outputLines.Add($"- **Position:** {gameObject.transform.position}");
        outputLines.Add("- **Components:**");

        foreach (var component in gameObject.GetComponents<Component>().Where(c => c != null))
            outputLines.Add($"  - {component.GetType().Name}");

        var childObjects = gameObject.transform.Cast<Transform>()
            .Where(t => !ignoreList.Contains(t.name))
            .Take(maxChildren)
            .ToList();

        foreach (var child in childObjects) AppendGameObjectDetails(outputLines, child.gameObject, maxChildren);

        // 子オブジェクトの省略表示
        if (gameObject.transform.childCount > maxChildren) outputLines.Add("  ...");

        outputLines.Add("");
    }

    private void LoadIgnoreList()
    {
        var ignoreFilePath = Path.Combine(Application.dataPath, "Editor/IgnoreList.txt");
        ignoreList = new List<string>();

        if (File.Exists(ignoreFilePath))
        {
            var lines = File.ReadAllLines(ignoreFilePath);
            ignoreList.AddRange(lines.Select(line => line.Trim()).Where(line => !string.IsNullOrEmpty(line)));
            Debug.Log("IgnoreList.txt loaded with " + ignoreList.Count + " items.");
        }
        else
        {
            Debug.LogWarning("IgnoreList.txt not found. No objects will be ignored.");
        }
    }

    private void SaveCurrentProfile()
    {
        var profileName = profileNames[selectedProfileIndex];
        var profile = new PatternProfile
        {
            ExcludePatternsText = excludePatternsText,
            SkipContentPatternsText = skipContentPatternsText,
            IncludeExtensionsText = includeExtensionsText
        };

        patternProfiles[profileName] = profile;

        SavePatternProfiles();
    }

    private void LoadCurrentProfile()
    {
        var profileName = profileNames[selectedProfileIndex];
        if (patternProfiles.ContainsKey(profileName))
        {
            excludePatternsText = patternProfiles[profileName].ExcludePatternsText;
            skipContentPatternsText = patternProfiles[profileName].SkipContentPatternsText;
            includeExtensionsText = patternProfiles[profileName].IncludeExtensionsText;
        }
    }

    private void SavePatternProfile(string profileName)
    {
        var profile = new PatternProfile
        {
            ExcludePatternsText = excludePatternsText,
            SkipContentPatternsText = skipContentPatternsText,
            IncludeExtensionsText = includeExtensionsText
        };

        patternProfiles[profileName] = profile;

        if (!profileNames.Contains(profileName)) profileNames.Add(profileName);

        selectedProfileIndex = profileNames.IndexOf(profileName);

        SavePatternProfiles();
    }

    private void LoadPatternProfiles()
    {
        patternProfiles.Clear();
        profileNames.Clear();

        // EditorPrefsからデータを読み込む
        var profileCount = EditorPrefs.GetInt("PSS_ProfileCount", 0);
        for (var i = 0; i < profileCount; i++)
        {
            var profileName = EditorPrefs.GetString($"PSS_ProfileName_{i}", "");
            if (!string.IsNullOrEmpty(profileName))
            {
                var profile = new PatternProfile
                {
                    ExcludePatternsText = EditorPrefs.GetString($"PSS_ExcludePatterns_{profileName}", ""),
                    SkipContentPatternsText = EditorPrefs.GetString($"PSS_SkipPatterns_{profileName}", ""),
                    IncludeExtensionsText = EditorPrefs.GetString($"PSS_IncludeExtensions_{profileName}", "")
                };
                patternProfiles[profileName] = profile;
                profileNames.Add(profileName);
            }
        }

        if (profileNames.Count == 0)
        {
            profileNames.Add("デフォルト");
            patternProfiles["デフォルト"] = new PatternProfile();
        }

        selectedProfileIndex = EditorPrefs.GetInt("PSS_SelectedProfileIndex", 0);
        if (selectedProfileIndex >= profileNames.Count)
            selectedProfileIndex = 0;

        // プロファイルの内容を読み込み
        LoadCurrentProfile();
    }

    private void SavePatternProfiles()
    {
        // EditorPrefsにデータを保存
        EditorPrefs.SetInt("PSS_ProfileCount", patternProfiles.Count);
        var index = 0;
        foreach (var kvp in patternProfiles)
        {
            var profileName = kvp.Key;
            var profile = kvp.Value;

            EditorPrefs.SetString($"PSS_ProfileName_{index}", profileName);
            EditorPrefs.SetString($"PSS_ExcludePatterns_{profileName}", profile.ExcludePatternsText);
            EditorPrefs.SetString($"PSS_SkipPatterns_{profileName}", profile.SkipContentPatternsText);
            EditorPrefs.SetString($"PSS_IncludeExtensions_{profileName}", profile.IncludeExtensionsText);

            index++;
        }

        EditorPrefs.SetInt("PSS_SelectedProfileIndex", selectedProfileIndex);
    }

    // パターンプロファイルのクラス
    [Serializable]
    private class PatternProfile
    {
        public string ExcludePatternsText = "";
        public string SkipContentPatternsText = "";
        public string IncludeExtensionsText = "";
    }
}