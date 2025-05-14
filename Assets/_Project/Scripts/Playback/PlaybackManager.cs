using System;
using System.Linq;
using System.Collections.Generic;
using StepUpTableTennis.DataManagement.Core.Models;
using UnityEngine;

namespace StepUpTableTennis.Playback
{
    public class PlaybackManager : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private PlaybackVisualizer visualizer;

        [Header("Playback Settings")]
        [SerializeField]
        private float defaultPlaybackSpeed = 1f;
        [SerializeField]
        private float maxPlaybackSpeed = 3f;
        [SerializeField]
        private PlaybackUIController uiController;

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
            if (!isPlaying || currentShot == null)
                return;

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

            if (visualizer != null)
            {
                visualizer.Show();
                visualizer.ClearTrail();

                if (shot.BallMotionData.Count > 0)
                {
                    var initialBallData = shot.BallMotionData[0];
                    visualizer.UpdateBallTransform(initialBallData.Position, initialBallData.Rotation);
                    visualizer.SetBallVisibility(initialBallData.IsBallVisible);
                }
            }
            else
            {
                Debug.LogError("Visualizer is not assigned in PlaybackManager");
            }

            if (uiController != null)
                uiController.UpdateTimelineRange(0f, shotDuration);
        }

        private float GetShotDuration(TrainingShot shot)
        {
            if (shot.BallMotionData.Count == 0 && shot.RacketMotionData.Count == 0)
                return 0f;

            float totalDuration = 0f;
            if (shot.BallMotionData.Count > 0)
                totalDuration = shot.BallMotionData.Sum(data => data.TimeOffset);
            if (shot.RacketMotionData.Count > 0)
            {
                float racketDuration = shot.RacketMotionData.Sum(data => data.TimeOffset);
                totalDuration = Mathf.Max(totalDuration, racketDuration);
            }

            Debug.Log($"Calculated shot duration: {totalDuration} seconds from {shot.BallMotionData.Count} ball records");
            return totalDuration;
        }

        private void UpdateVisuals(float time)
        {
            try
            {
                // ボールの更新：BallMotionRecordData の補間を使用
                if (currentShot.BallMotionData.Count > 0)
                {
                    var ballData = InterpolateBallMotionData(currentShot.BallMotionData, time);
                    visualizer.UpdateBallTransform(ballData.Position, ballData.Rotation);
                    visualizer.SetBallVisibility(ballData.IsBallVisible);
                }

                // ラケットの更新
                if (currentShot.RacketMotionData.Count > 0)
                {
                    var racketData = MotionDataInterpolator.InterpolateMotionData(
                        currentShot.RacketMotionData, time);
                    visualizer.UpdateRacketTransform(racketData.Position, racketData.Rotation);
                }
                else
                {
                    Debug.LogWarning("No racket motion data available");
                }

                // 視線の更新
                if (currentShot.GazeData.Count > 0)
                {
                    var gazeData = InterpolateGazeData(currentShot.GazeData, time);
                    visualizer.UpdateGazeVisualization(
                        gazeData.LeftEyePosition,
                        gazeData.LeftEyeDirection,
                        gazeData.LeftEyeClosedAmount,
                        gazeData.RightEyePosition,
                        gazeData.RightEyeDirection,
                        gazeData.RightEyeClosedAmount,
                        gazeData.AngularVelocity
                    );
                    visualizer.SetGazeSaccadeState(gazeData.IsSaccade);
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
            if (currentShot == null)
                return;
            isPlaying = true;
            visualizer.ClearTrail();
        }

        public void Pause()
        {
            isPlaying = false;
        }

        public void SetTime(float time)
        {
            if (currentShot == null)
                return;

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

        /// <summary>
        /// BallMotionRecordData 用の線形補間メソッド
        /// </summary>
        /// <param name="data">BallMotionRecordData のリスト</param>
        /// <param name="targetTime">再生開始からの経過時間</param>
        /// <returns>補間後の BallMotionRecordData</returns>
        private DataManagement.Core.Models.BallMotionRecordData InterpolateBallMotionData(IReadOnlyList<DataManagement.Core.Models.BallMotionRecordData> data, float targetTime)
        {
            if (data == null || data.Count == 0)
                throw new ArgumentException("No ball motion data available");

            if (data.Count == 1)
                return data[0];

            float[] cumulativeTime = new float[data.Count];
            cumulativeTime[0] = data[0].TimeOffset;
            for (int i = 1; i < data.Count; i++)
            {
                cumulativeTime[i] = cumulativeTime[i - 1] + data[i].TimeOffset;
            }

            int startIndex = -1;
            for (int i = 0; i < data.Count - 1; i++)
            {
                if (cumulativeTime[i] <= targetTime && targetTime <= cumulativeTime[i + 1])
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex == -1)
                return targetTime <= cumulativeTime[0] ? data[0] : data[data.Count - 1];

            var a = data[startIndex];
            var b = data[startIndex + 1];
            float t = Mathf.InverseLerp(cumulativeTime[startIndex], cumulativeTime[startIndex + 1], targetTime);

            Vector3 pos = Vector3.Lerp(a.Position, b.Position, t);
            Quaternion rot = Quaternion.Slerp(a.Rotation, b.Rotation, t);
            Vector3 vel = Vector3.Lerp(a.Velocity, b.Velocity, t);
            Vector3 angVel = Vector3.Lerp(a.AngularVelocity, b.AngularVelocity, t);
            bool isVisible = t < 0.5f ? a.IsBallVisible : b.IsBallVisible;

            return new DataManagement.Core.Models.BallMotionRecordData(a.Timestamp, targetTime, pos, rot, vel, angVel, isVisible);
        }

        /// <summary>
        /// GazeRecordData の線形補間
        /// </summary>
        private GazeRecordData InterpolateGazeData(IReadOnlyList<GazeRecordData> data, float targetTime)
        {
            if (data == null || data.Count == 0)
                throw new ArgumentException("No gaze data available");

            if (data.Count == 1)
                return data[0];

            float[] cumulativeTime = new float[data.Count];
            cumulativeTime[0] = data[0].TimeOffset;
            for (int i = 1; i < data.Count; i++)
            {
                cumulativeTime[i] = cumulativeTime[i - 1] + data[i].TimeOffset;
            }

            int startIndex = -1;
            for (int i = 0; i < data.Count - 1; i++)
            {
                if (cumulativeTime[i] <= targetTime && targetTime <= cumulativeTime[i + 1])
                {
                    startIndex = i;
                    break;
                }
            }

            if (startIndex == -1)
                return targetTime <= cumulativeTime[0] ? data[0] : data[data.Count - 1];

            var a = data[startIndex];
            var b = data[startIndex + 1];
            float t = Mathf.InverseLerp(cumulativeTime[startIndex], cumulativeTime[startIndex + 1], targetTime);

            Vector3 leftEyePos = Vector3.Lerp(a.LeftEyePosition, b.LeftEyePosition, t);
            Vector3 rightEyePos = Vector3.Lerp(a.RightEyePosition, b.RightEyePosition, t);
            Vector3 leftEyeDir = Vector3.Lerp(a.LeftEyeDirection, b.LeftEyeDirection, t).normalized;
            Vector3 rightEyeDir = Vector3.Lerp(a.RightEyeDirection, b.RightEyeDirection, t).normalized;
            float leftEyeClosed = Mathf.Lerp(a.LeftEyeClosedAmount, b.LeftEyeClosedAmount, t);
            float rightEyeClosed = Mathf.Lerp(a.RightEyeClosedAmount, b.RightEyeClosedAmount, t);
            bool isSaccade = t < 0.5f ? a.IsSaccade : b.IsSaccade;
            
            // 角速度と角加速度の補間
            float angularVelocity = Mathf.Lerp(a.AngularVelocity, b.AngularVelocity, t);
            float angularAcceleration = Mathf.Lerp(a.AngularAcceleration, b.AngularAcceleration, t);

            return new GazeRecordData(a.Timestamp, targetTime, leftEyeDir, rightEyeDir, leftEyePos, rightEyePos, leftEyeClosed, rightEyeClosed, isSaccade, angularVelocity, angularAcceleration);
        }
    }
}