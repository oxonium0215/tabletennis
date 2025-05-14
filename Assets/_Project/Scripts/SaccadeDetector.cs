﻿using UnityEngine;
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

    // 角速度と角加速度を外部からアクセスできるようにするプロパティ
    public float CurrentAngularVelocity { get; private set; } = 0f;
    public float CurrentAngularAcceleration { get; private set; } = 0f;

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
        
        // 現在の値を保存
        CurrentAngularVelocity = angularVelocity;
        CurrentAngularAcceleration = angularAcceleration;
        
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
    
    /// <summary>
    /// 左右の目の視線方向から平均の視線方向を計算し、サッカード状態と角速度を更新します
    /// </summary>
    /// <param name="leftEyeDirection">左目の視線方向</param>
    /// <param name="rightEyeDirection">右目の視線方向</param>
    /// <returns>サッカード状態</returns>
    public bool UpdateFromEyeDirections(Vector3 leftEyeDirection, Vector3 rightEyeDirection)
    {
        // 左右の視線方向の平均を計算（正規化）
        Vector3 combinedGazeDirection = ((leftEyeDirection + rightEyeDirection) * 0.5f).normalized;
        
        // サッカード状態と角速度を更新
        return UpdateSaccadeState(combinedGazeDirection);
    }
    
    /// <summary>
    /// 現在の角速度と角加速度を取得します
    /// </summary>
    /// <param name="angularVelocity">出力: 角速度 (度/秒)</param>
    /// <param name="angularAcceleration">出力: 角加速度 (度/秒²)</param>
    public void GetGazeMetrics(out float angularVelocity, out float angularAcceleration)
    {
        angularVelocity = CurrentAngularVelocity;
        angularAcceleration = CurrentAngularAcceleration;
    }
}