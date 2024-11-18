using System;
using System.Collections.Generic;
using StepUpTableTennis.DataManagement.Core.Interfaces;
using StepUpTableTennis.DataManagement.Core.Models.Shot;
using StepUpTableTennis.DataManagement.Core.Models.TimeSeries;
using UnityEngine;

namespace StepUpTableTennis.DataManagement.Storage.Memory
{
    /// <summary>
    ///     時系列データをメモリ上にバッファリングするためのクラス。
    ///     高頻度のデータ記録に対応し、一定量のデータを保持します。
    /// </summary>
    public class TimeSeriesBuffer : ITimeSeriesBuffer
    {
        private readonly int initialCapacity;
        private readonly int maxDataPoints;
        private readonly List<TimeSeriesDataPoint> buffer;
        private MotionData.TimeSeriesDataType currentDataType;
        private ShotId? currentShotId;
        private long recordingStartTimeMs;

        /// <summary>
        ///     TimeSeriesBufferを初期化します。
        /// </summary>
        /// <param name="initialCapacity">バッファの初期容量（データポイント数）</param>
        /// <param name="maxDataPoints">バッファの最大容量（データポイント数）</param>
        public TimeSeriesBuffer(int initialCapacity = 1000, int maxDataPoints = 10000)
        {
            this.initialCapacity = initialCapacity;
            this.maxDataPoints = maxDataPoints;
            buffer = new List<TimeSeriesDataPoint>(initialCapacity);
        }

        public int CurrentBufferSize => buffer.Count;
        public bool IsRecording { get; private set; }
        public MotionData.TimeSeriesDataType? CurrentDataType => IsRecording ? currentDataType : null;

        /// <summary>
        ///     1フレーム分のデータポイントを記録します。
        ///     通常、FixedUpdateで呼び出されます。
        /// </summary>
        /// <param name="dataPoint">記録するデータポイント</param>
        public void RecordDataPoint(TimeSeriesDataPoint dataPoint)
        {
            if (!IsRecording)
                throw new InvalidOperationException("Not currently recording. Call StartRecording first.");

            if (buffer.Count >= maxDataPoints)
            {
                Debug.LogWarning($"Buffer full ({maxDataPoints} points). Discarding oldest data point.");
                buffer.RemoveAt(0);
            }

            try
            {
                dataPoint.Validate();
                buffer.Add(dataPoint);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to record data point: {e.Message}");
                throw;
            }
        }

        /// <summary>
        ///     記録を停止し、記録されたデータを返します。
        /// </summary>
        /// <returns>記録された時系列データ。記録中でない場合はnull。</returns>
        public MotionData? StopRecording()
        {
            if (!IsRecording || currentShotId == null)
            {
                Debug.LogWarning("Not currently recording or no shot ID set.");
                return null;
            }

            try
            {
                var motionData = new MotionData(
                    currentShotId,
                    currentDataType,
                    new List<TimeSeriesDataPoint>(buffer)
                );

                Debug.Log($"Stopped recording. Recorded {buffer.Count} data points for shot {currentShotId}");

                Clear();
                return motionData;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create motion data: {e.Message}");
                return null;
            }
        }

        /// <summary>
        ///     バッファをクリアし、記録状態をリセットします。
        /// </summary>
        public void Clear()
        {
            currentShotId = null;
            buffer.Clear();
            IsRecording = false;
        }

        /// <summary>
        ///     指定されたショットIDとデータタイプで記録を開始します。
        /// </summary>
        /// <param name="shotId">記録するショットのID</param>
        /// <param name="type">記録するデータの種類</param>
        public void StartRecording(ShotId shotId, MotionData.TimeSeriesDataType type)
        {
            if (IsRecording) throw new InvalidOperationException("Already recording. Stop current recording first.");

            currentShotId = shotId ?? throw new ArgumentNullException(nameof(shotId));
            currentDataType = type;
            buffer.Clear();
            IsRecording = true;
            recordingStartTimeMs = GetCurrentTimestampMs();

            Debug.Log($"Started recording time series data for shot {shotId}, type: {type}");
        }

        /// <summary>
        ///     記録開始からの経過時間（ミリ秒）を取得します。
        /// </summary>
        private long GetElapsedTimeMs()
        {
            return GetCurrentTimestampMs() - recordingStartTimeMs;
        }

        /// <summary>
        ///     現在時刻のタイムスタンプ（ミリ秒）を取得します。
        /// </summary>
        private static long GetCurrentTimestampMs()
        {
            return (long)(Time.realtimeSinceStartup * 1000);
        }
    }
}