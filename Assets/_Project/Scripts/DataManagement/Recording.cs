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
        private readonly OVREyeGaze eyeGaze;
        private readonly OVRFaceExpressions faceExpressions;
        private readonly float recordingInterval = 1f / 200f; // 60Hz でサンプリング
        private BallStateManager currentBallStateManager;
        private int currentShotIndex = -1;
        private float lastRecordTime;
        private IReadOnlyList<TrainingShot> sessionShots;
        private OVRPlugin.EyeGazesState eyeGazeState;
        private readonly SaccadeDetector saccadeDetector; // サッカード検出用

        public MotionRecorder(
            PaddleSetup paddleStateHandler,
            Transform headTransform,
            OVREyeGaze eyeGaze,
            OVRFaceExpressions faceExpressions,
            SaccadeDetector saccadeDetector)
        {
            _paddleStateHandler = paddleStateHandler;
            this.headTransform = headTransform;
            this.eyeGaze = eyeGaze;
            this.faceExpressions = faceExpressions;
            this.saccadeDetector = saccadeDetector;
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

            // ボール記録用（BallMotionRecordData を使用）
            BallMotionRecordData ballMotion = null;
            // ラケットとヘッドは通常の MotionRecordData を使用
            MotionRecordData racketMotion = null;
            MotionRecordData headMotion = null;
            GazeRecordData gazeData = null;

            // ボールの状態を記録
            if (currentBallStateManager != null && currentBallStateManager.Ball != null)
            {
                var ballState = currentBallStateManager.Ball;
                // MeshRenderer の状態を記録する
                var renderer = currentBallStateManager.gameObject.GetComponent<MeshRenderer>();
                bool isVisible = renderer != null && renderer.enabled;

                ballMotion = new BallMotionRecordData(
                    timestamp,
                    timeOffset,
                    ballState.Position,
                    ballState.Rotation,
                    ballState.Velocity,
                    ballState.Spin,
                    isVisible
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
                    Vector3.zero,
                    Vector3.zero
                );

            // 視線データの記録
            if (eyeGaze != null && eyeGaze.EyeTrackingEnabled)
            {
                if (!OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref eyeGazeState))
                {
                    return;
                }

                var leftState = eyeGazeState.EyeGazes[(int)OVRPlugin.Eye.Left];
                var rightState = eyeGazeState.EyeGazes[(int)OVRPlugin.Eye.Right];

                var leftPose = leftState.Pose.ToOVRPose();
                var rightPose = rightState.Pose.ToOVRPose();

                var leftWorldPose = leftPose.ToWorldSpacePose(Camera.main);
                var rightWorldPose = rightPose.ToWorldSpacePose(Camera.main);

                var leftGazeDir = leftWorldPose.orientation * Vector3.forward;
                var rightGazeDir = rightWorldPose.orientation * Vector3.forward;
                
                // SaccadeDetectorを使って角速度とサッカード状態を取得
                bool isSaccade = false;
                float angularVelocity = 0f;
                float angularAcceleration = 0f;
                
                if (saccadeDetector != null)
                {
                    // 左右の視線方向から更新
                    isSaccade = saccadeDetector.UpdateFromEyeDirections(leftGazeDir, rightGazeDir);
                    
                    // 角速度と角加速度を取得
                    saccadeDetector.GetGazeMetrics(out angularVelocity, out angularAcceleration);
                }

                float leftEyeClosed = 0f;
                float rightEyeClosed = 0f;

                if (faceExpressions != null && faceExpressions.FaceTrackingEnabled)
                {
                    leftEyeClosed = faceExpressions.TryGetFaceExpressionWeight(
                        OVRFaceExpressions.FaceExpression.EyesClosedL, 
                        out float leftWeight) ? leftWeight : 0f;

                    rightEyeClosed = faceExpressions.TryGetFaceExpressionWeight(
                        OVRFaceExpressions.FaceExpression.EyesClosedR, 
                        out float rightWeight) ? rightWeight : 0f;
                }

                gazeData = new GazeRecordData(
                    timestamp,
                    timeOffset,
                    leftWorldPose.orientation * Vector3.forward,
                    rightWorldPose.orientation * Vector3.forward,
                    leftWorldPose.position,
                    rightWorldPose.position,
                    leftEyeClosed,
                    rightEyeClosed,
                    isSaccade,
                    angularVelocity,
                    angularAcceleration
                );
            }

            // 記録の追加：BallMotionData のみボールの記録に IsBallVisible を持ち、他は通常 MotionRecordData
            if (currentShotIndex >= 0 && currentShotIndex < sessionShots.Count)
            {
                var currentShot = sessionShots[currentShotIndex];
                if (ballMotion != null) currentShot.BallMotionData.Add(ballMotion);
                if (racketMotion != null) currentShot.RacketMotionData.Add(racketMotion);
                if (headMotion != null) currentShot.HeadMotionData.Add(headMotion);
                if (gazeData != null) currentShot.GazeData.Add(gazeData);
            }
            else if (currentShotIndex == -1 && sessionShots.Count > 0)
            {
                var prevShot = sessionShots[0];
                if (ballMotion != null) prevShot.BallMotionData.Add(ballMotion);
                if (racketMotion != null) prevShot.RacketMotionData.Add(racketMotion);
                if (headMotion != null) prevShot.HeadMotionData.Add(headMotion);
                if (gazeData != null) prevShot.GazeData.Add(gazeData);
            }
        }
    }
}