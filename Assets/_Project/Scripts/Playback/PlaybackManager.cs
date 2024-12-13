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

            // ビジュアライザーとUIを初期化
            visualizer.Show();
            visualizer.ClearTrail();
            UpdateVisuals(0f);

            // タイムライン範囲を更新
            uiController.UpdateTimelineRange(0f, shotDuration);
        }

        private float GetShotDuration(TrainingShot shot)
        {
            var lastBallData = shot.BallMotionData.LastOrDefault();
            var lastRacketData = shot.RacketMotionData.LastOrDefault();

            var ballDuration = lastBallData?.TimeOffset ?? 0f;
            var racketDuration = lastRacketData?.TimeOffset ?? 0f;

            return Mathf.Max(ballDuration, racketDuration);
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

                // ラケットの更新
                if (currentShot.RacketMotionData.Count > 0)
                {
                    var racketData = MotionDataInterpolator.InterpolateMotionData(
                        currentShot.RacketMotionData, time);
                    visualizer.UpdateRacketTransform(racketData.Position, racketData.Rotation);
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