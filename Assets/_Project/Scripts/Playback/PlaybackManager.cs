using System;
using System.Linq;
using StepUpTableTennis.DataManagement.Core.Models;
using UnityEngine;

namespace StepUpTableTennis.Playback
{
    public class PlaybackManager : MonoBehaviour
    {
        [Header("Components")] [SerializeField]
        private PlaybackVisualizer visualizer;

        [Header("Playback Settings")] [SerializeField]
        private float defaultPlaybackSpeed = 1f;

        [SerializeField] private float maxPlaybackSpeed = 3f;
        [SerializeField] private PlaybackUIController uiController;
        private TrainingShot currentShot;
        private float currentTime;
        private bool isPlaying;
        private float playbackSpeed;
        private float shotDuration;

        private void Start()
        {
            playbackSpeed = defaultPlaybackSpeed;
            if (visualizer == null)
                visualizer = GetComponent<PlaybackVisualizer>();
            if (uiController == null)
                uiController = GetComponent<PlaybackUIController>();
        }

        private void Update()
        {
            if (!isPlaying || currentShot == null) return;

            currentTime += Time.deltaTime * playbackSpeed;

            if (currentTime >= shotDuration)
            {
                currentTime = shotDuration;
                isPlaying = false;
            }

            UpdateVisuals(currentTime);
            uiController.UpdateCurrentTime(currentTime);
        }

        public void Initialize(TrainingShot shot)
        {
            currentShot = shot;
            currentTime = 0f;
            shotDuration = GetShotDuration(shot);

            // ビジュアライザーの初期化を確実に行う
            if (visualizer != null)
            {
                visualizer.Show();
                visualizer.ClearTrail();

                // 初期状態を明示的に設定
                if (shot.BallMotionData.Count > 0)
                {
                    var initialBallData = shot.BallMotionData[0];
                    visualizer.UpdateBallTransform(initialBallData.Position, initialBallData.Rotation);
                }
            }
            else
            {
                Debug.LogError("Visualizer is not assigned in PlaybackManager");
            }

            // タイムライン範囲を更新
            if (uiController != null) uiController.UpdateTimelineRange(0f, shotDuration);
        }

        private float GetShotDuration(TrainingShot shot)
        {
            if (shot.BallMotionData.Count == 0 && shot.RacketMotionData.Count == 0)
                return 0f;

            var totalDuration = 0f;

            // ボールの動きの合計時間を計算
            if (shot.BallMotionData.Count > 0)
                // TimeOffsetを累積して合計時間を計算
                totalDuration = shot.BallMotionData.Sum(data => data.TimeOffset);

            // ラケットの動きの合計時間を計算
            if (shot.RacketMotionData.Count > 0)
            {
                var racketDuration = shot.RacketMotionData.Sum(data => data.TimeOffset);
                totalDuration = Mathf.Max(totalDuration, racketDuration);
            }

            Debug.Log(
                $"Calculated shot duration: {totalDuration} seconds from {shot.BallMotionData.Count} ball records");
            return totalDuration;
        }

        private void UpdateVisuals(float time)
        {
            try
            {
                // ボールの更新
                if (currentShot.BallMotionData.Count > 0)
                {
                    var ballData = MotionDataInterpolator.InterpolateMotionData(
                        currentShot.BallMotionData, time);
                    visualizer.UpdateBallTransform(ballData.Position, ballData.Rotation);
                }

                // パドルの更新
                if (currentShot.RacketMotionData.Count > 0)
                {
                    var racketData = MotionDataInterpolator.InterpolateMotionData(
                        currentShot.RacketMotionData, time);

                    // パドルの位置と回転を更新
                    visualizer.UpdateRacketTransform(
                        racketData.Position,
                        racketData.Rotation
                    );

                    // デバッグログを追加
                    Debug.Log($"Updating paddle - Position: {racketData.Position}, Rotation: {racketData.Rotation}");
                }
                else
                {
                    Debug.LogWarning("No racket motion data available");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating visuals: {e.Message}");
                Pause();
            }
        }

        public void Play()
        {
            if (currentShot == null) return;
            isPlaying = true;
            visualizer.ClearTrail();
        }

        public void Pause()
        {
            isPlaying = false;
        }

        public void SetTime(float time)
        {
            if (currentShot == null) return;

            currentTime = Mathf.Clamp(time, 0f, shotDuration);
            UpdateVisuals(currentTime);
            uiController.UpdateCurrentTime(currentTime);
        }

        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Clamp(speed, 0.1f, maxPlaybackSpeed);
            uiController.UpdatePlaybackSpeed(playbackSpeed);
        }

        public float GetPlaybackSpeed()
        {
            return playbackSpeed;
        }

        public bool IsPlaying()
        {
            return isPlaying;
        }
    }
}