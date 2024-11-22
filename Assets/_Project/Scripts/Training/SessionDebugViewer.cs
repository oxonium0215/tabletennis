using System;
using System.IO;
using System.Linq;
using StepUpTableTennis.DataManagement.Core.Models;
using StepUpTableTennis.DataManagement.Storage;
using UnityEngine;

namespace StepUpTableTennis.Training
{
    public class SessionDebugViewer : MonoBehaviour
    {
        private string dataPath;
        private TrainingDataStorage dataStorage;

        private void Start()
        {
            dataPath = Path.Combine(Application.persistentDataPath, "TrainingData");
            dataStorage = new TrainingDataStorage(dataPath);

            Debug.Log($"Session data directory: {dataPath}");
        }

        [ContextMenu("Load And Display Latest Session")]
        public async void LoadAndDisplayLatestSession()
        {
            var sessionDirectory = Path.Combine(dataPath, "Sessions");
            if (!Directory.Exists(sessionDirectory))
            {
                Debug.Log("No sessions directory found");
                return;
            }

            var directories = Directory.GetDirectories(sessionDirectory);
            if (directories.Length == 0)
            {
                Debug.Log("No session data found");
                return;
            }

            // 最新のセッションディレクトリを取得
            var latestSessionDir = directories
                .OrderByDescending(d => Directory.GetCreationTime(d))
                .First();
            var sessionId = new SessionId(Path.GetFileName(latestSessionDir));

            try
            {
                var session = await dataStorage.LoadSessionAsync(sessionId);
                DisplaySessionInfo(session);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load session: {e.Message}");
            }
        }

        [ContextMenu("Display All Sessions Summary")]
        public async void DisplayAllSessionsSummary()
        {
            var sessionDirectory = Path.Combine(dataPath, "Sessions");
            if (!Directory.Exists(sessionDirectory))
            {
                Debug.Log("No sessions directory found");
                return;
            }

            var directories = Directory.GetDirectories(sessionDirectory);
            Debug.Log($"Found {directories.Length} sessions:");

            foreach (var dir in directories)
            {
                var sessionId = new SessionId(Path.GetFileName(dir));
                try
                {
                    var session = await dataStorage.LoadSessionAsync(sessionId);
                    DisplaySessionSummary(session);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load session {sessionId.Value}: {e.Message}");
                }
            }
        }

        private void DisplaySessionInfo(TrainingSession session)
        {
            Debug.Log("\n=== Detailed Session Info ===");
            Debug.Log($"Session ID: {session.Id.Value}");
            Debug.Log($"Start Time: {session.StartTime}");
            Debug.Log("\nDifficulty Settings:");
            Debug.Log($"- Speed Level: {session.Config.Difficulty.SpeedLevel}");
            Debug.Log($"- Spin Level: {session.Config.Difficulty.SpinLevel}");
            Debug.Log($"- Course Level: {session.Config.Difficulty.CourseLevel}");

            Debug.Log("\nShot Data:");
            Debug.Log($"Total Shots Configured: {session.Shots.Count}");

            var executedShots = session.Shots.Count(s => s.IsExecuted);
            Debug.Log($"Executed Shots: {executedShots}");

            var successfulShots = session.Shots.Count(s => s.WasSuccessful == true);
            Debug.Log($"Successful Shots: {successfulShots}");

            if (session.IsCompleted && session.Statistics != null)
            {
                Debug.Log("\nSession Statistics:");
                Debug.Log($"Success Rate: {session.Statistics.SuccessRate:P2}");
                Debug.Log($"Completed At: {session.Statistics.CompletedAt}");
            }

            // 詳細なショットデータの表示
            Debug.Log("\nDetailed Shot Information:");
            for (var i = 0; i < session.Shots.Count; i++)
            {
                var shot = session.Shots[i];
                if (shot.IsExecuted)
                {
                    Debug.Log($"\nShot {i + 1}:");
                    Debug.Log($"- Executed At: {shot.ExecutedAt}");
                    Debug.Log($"- Success: {shot.WasSuccessful}");
                    Debug.Log($"- Ball Motion Records: {shot.BallMotionData.Count}");
                    Debug.Log($"- Racket Motion Records: {shot.RacketMotionData.Count}");
                    Debug.Log($"- Head Motion Records: {shot.HeadMotionData.Count}");
                    Debug.Log($"- Gaze Records: {shot.GazeData.Count}");
                }
            }
        }

        private void DisplaySessionSummary(TrainingSession session)
        {
            var executedShots = session.Shots.Count(s => s.IsExecuted);
            var successfulShots = session.Shots.Count(s => s.WasSuccessful == true);

            Debug.Log($"\nSession {session.Id.Value}:");
            Debug.Log($"- Date: {session.StartTime}");
            Debug.Log($"- Difficulty: Speed={session.Config.Difficulty.SpeedLevel}, " +
                      $"Spin={session.Config.Difficulty.SpinLevel}, " +
                      $"Course={session.Config.Difficulty.CourseLevel}");
            Debug.Log($"- Shots: {executedShots}/{session.Shots.Count} " +
                      $"(Success: {successfulShots})");
        }
    }
}