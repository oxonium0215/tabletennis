using UnityEngine;
using System;
using System.Collections.Generic;

public class SaccadeDetector : MonoBehaviour
{
    [SerializeField] private float saccadeStartVelocityThreshold = 50f;
    [SerializeField] private float saccadeStartAccelThreshold = 1000f;
    [SerializeField] private float saccadeEndVelocityThreshold = 50f;
    [SerializeField] private float minimumSaccadeDuration = 0.02f; // 最小サッカード持続時間（秒）

    private Queue<Vector3> recentGazeDirections = new Queue<Vector3>();
    private Vector3 previousGazeDirection = Vector3.zero;
    private float previousVelocity = 0f;
    private bool isSaccade = false;
    private float saccadeStartTime = 0f; // サッカード開始時刻を記録

    public event Action OnSaccadeStarted;
    public event Action OnSaccadeEnded;

    public bool IsSaccade => isSaccade;

    public bool UpdateSaccadeState(Vector3 currentGazeDirection)
    {
        recentGazeDirections.Enqueue(currentGazeDirection);
        if (recentGazeDirections.Count > 5)
        {
            recentGazeDirections.Dequeue();
        }

        Vector3 averageGazeDirection = Vector3.zero;
        foreach (var direction in recentGazeDirections)
        {
            averageGazeDirection += direction;
        }
        averageGazeDirection /= recentGazeDirections.Count;

        float angularVelocity = Vector3.Angle(previousGazeDirection, averageGazeDirection) / Time.deltaTime;
        float angularAcceleration = (angularVelocity - previousVelocity) / Time.deltaTime;
        bool previousSaccadeState = isSaccade;

        if (!isSaccade)
        {
            // サッカード開始判定
            if (Mathf.Abs(angularVelocity) >= saccadeStartVelocityThreshold && 
                angularAcceleration > saccadeStartAccelThreshold)
            {
                isSaccade = true;
                saccadeStartTime = Time.time; // サッカード開始時刻を記録
            }
        }
        else
        {
            // サッカード終了判定
            float elapsedTime = Time.time - saccadeStartTime;
            
            // 最小持続時間を経過していない場合は、強制的にサッカード状態を維持
            if (elapsedTime < minimumSaccadeDuration)
            {
                isSaccade = true;
            }
            else
            {
                // 最小持続時間経過後は速度に基づいて判定
                isSaccade = Mathf.Abs(angularVelocity) >= saccadeEndVelocityThreshold;
            }
        }

        if (!previousSaccadeState && isSaccade)
        {
            OnSaccadeStarted?.Invoke();
        }
        else if (previousSaccadeState && !isSaccade)
        {
            OnSaccadeEnded?.Invoke();
        }

        previousGazeDirection = averageGazeDirection;
        previousVelocity = angularVelocity;

        return isSaccade;
    }

    public void SetThresholds(float startVelocity, float startAccel, float endVelocity)
    {
        saccadeStartVelocityThreshold = startVelocity;
        saccadeStartAccelThreshold = startAccel;
        saccadeEndVelocityThreshold = endVelocity;
    }
}