using System;
using System.IO;
using System.Linq;
using StepUpTableTennis.DataManagement.Core.Models;
using StepUpTableTennis.DataManagement.Storage;
using UnityEngine;

namespace StepUpTableTennis.Playback
{
    // セッション全体の管理を行う最上位のマネージャー
    public class SessionPlaybackManager : MonoBehaviour
    {
        [SerializeField] private PlaybackManager playbackManager;
        [SerializeField] private PlaybackUIController uiController;
        private TrainingDataStorage dataStorage;

        private void Start()
        {
            // データストレージの初期化
            dataStorage = new TrainingDataStorage(
                Path.Combine(Application.persistentDataPath, "TrainingData")
            );

            // 最新のセッションを読み込む
            LoadLatestSession();
        }

        private async void LoadLatestSession()
        {
            try
            {
                // セッションディレクトリから最新のセッションIDを取得
                var sessionDirectory = Path.Combine(Application.persistentDataPath, "TrainingData", "Sessions");
                var directories = Directory.GetDirectories(sessionDirectory);
                if (directories.Length == 0)
                {
                    Debug.Log("No session data found");
                    return;
                }

                var latestSessionDir = directories
                    .OrderByDescending(d => Directory.GetCreationTime(d))
                    .First();
                var sessionId = new SessionId(Path.GetFileName(latestSessionDir));

                // セッションデータを読み込み
                var session = await dataStorage.LoadSessionAsync(sessionId);
                Debug.Log($"Loaded session: {sessionId.Value}");

                // UIとPlaybackManagerを初期化
                Initialize(session);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load session: {e.Message}");
            }
        }

        public void Initialize(TrainingSession session)
        {
            // 実行済みのショットのみを表示
            var executedShots = session.Shots
                .Where(s => s.IsExecuted)
                .ToList();

            // UIの初期化
            uiController.InitializeShotGrid(executedShots);

            // 最初のショットを選択
            if (executedShots.Count > 0) playbackManager.Initialize(executedShots[0]);
        }
    }
}