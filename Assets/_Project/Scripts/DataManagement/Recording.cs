using System;
using System.Collections.Generic;
using StepUpTableTennis.DataManagement.Core.Models;
using StepUpTableTennis.TableTennisEngine.Visualization;
using StepUpTableTennis.Training;
using UnityEngine;

namespace StepUpTableTennis.DataManagement.Recording
{
    public interface IMotionRecorder
    {
        bool IsRecording { get; }
        void StartSession(IReadOnlyList<TrainingShot> shots);
        void StopSession();
        void UpdateRecording();
        void SetCurrentShot(int shotIndex);
        void TrackBall(BallStateManager ballStateManager);
    }

    public class MotionRecorder : IMotionRecorder
    {
        private readonly PaddleSetup _paddleStateHandler;
        private readonly Transform headTransform;
        private readonly float recordingInterval = 1f / 60f; // 60Hz でサンプリング
        private BallStateManager currentBallStateManager;
        private int currentShotIndex = -1;
        private float lastRecordTime;
        private IReadOnlyList<TrainingShot> sessionShots;

        public MotionRecorder(
            PaddleSetup paddleStateHandler,
            Transform headTransform)
        {
            _paddleStateHandler = paddleStateHandler;
            this.headTransform = headTransform;
        }

        public bool IsRecording => sessionShots != null;

        public void TrackBall(BallStateManager ballStateManager)
        {
            currentBallStateManager = ballStateManager;
            Debug.Log($"Now tracking ball: {(ballStateManager != null ? ballStateManager.gameObject.name : "null")}");
        }

        public void StartSession(IReadOnlyList<TrainingShot> shots)
        {
            sessionShots = shots;
            currentShotIndex = -1;
            currentBallStateManager = null;
            lastRecordTime = Time.time;
        }

        public void StopSession()
        {
            sessionShots = null;
            currentShotIndex = -1;
            currentBallStateManager = null;
        }

        public void SetCurrentShot(int shotIndex)
        {
            if (!IsRecording) return;

            if (shotIndex >= 0 && shotIndex < sessionShots.Count)
                currentShotIndex = shotIndex;
            else
                Debug.LogWarning($"Invalid shot index: {shotIndex}");
        }

        public void UpdateRecording()
        {
            if (!IsRecording || Time.time - lastRecordTime < recordingInterval)
                return;

            RecordCurrentState();
            lastRecordTime = Time.time;
        }

        private void RecordCurrentState()
        {
            var timestamp = DateTime.Now;
            var timeOffset = Time.time - lastRecordTime;

            // モーションデータの作成
            MotionRecordData ballMotion = null;
            MotionRecordData racketMotion = null;
            MotionRecordData headMotion = null;

            // ボールの状態を記録
            if (currentBallStateManager != null && currentBallStateManager.Ball != null)
            {
                var ballState = currentBallStateManager.Ball;
                ballMotion = new MotionRecordData(
                    timestamp,
                    timeOffset,
                    ballState.Position,
                    ballState.Rotation,
                    ballState.Velocity,
                    ballState.Spin
                );
            }

            // ラケットの状態を記録
            if (_paddleStateHandler != null && _paddleStateHandler.Paddle != null)
            {
                var paddleState = _paddleStateHandler.Paddle;
                racketMotion = new MotionRecordData(
                    timestamp,
                    timeOffset,
                    paddleState.Position,
                    paddleState.Rotation,
                    paddleState.Velocity,
                    paddleState.AngularVelocity
                );
            }

            // 頭の位置を記録
            if (headTransform != null)
                headMotion = new MotionRecordData(
                    timestamp,
                    timeOffset,
                    headTransform.position,
                    headTransform.rotation,
                    Vector3.zero, // 速度は必要に応じて計算
                    Vector3.zero // 角速度は必要に応じて計算
                );

            // データの記録
            // 現在のショットが設定されている場合はそこに記録
            if (currentShotIndex >= 0 && currentShotIndex < sessionShots.Count)
            {
                var currentShot = sessionShots[currentShotIndex];
                if (ballMotion != null) currentShot.BallMotionData.Add(ballMotion);
                if (racketMotion != null) currentShot.RacketMotionData.Add(racketMotion);
                if (headMotion != null) currentShot.HeadMotionData.Add(headMotion);
            }
            // 未実行のショットの場合は直前のショットに記録を継続
            else if (currentShotIndex == -1 && sessionShots.Count > 0)
            {
                var prevShot = sessionShots[0];
                if (ballMotion != null) prevShot.BallMotionData.Add(ballMotion);
                if (racketMotion != null) prevShot.RacketMotionData.Add(racketMotion);
                if (headMotion != null) prevShot.HeadMotionData.Add(headMotion);
            }
        }
    }
}