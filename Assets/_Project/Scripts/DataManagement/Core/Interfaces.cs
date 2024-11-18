using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StepUpTableTennis.DataManagement.Core.Models.Session;
using StepUpTableTennis.DataManagement.Core.Models.Shot;
using StepUpTableTennis.DataManagement.Core.Models.TimeSeries;

namespace StepUpTableTennis.DataManagement.Core.Interfaces
{
    public interface IDataRepository
    {
        // セッション関連
        Task<bool> SaveSessionAsync(SessionData session);
        Task<SessionData?> GetSessionAsync(SessionId sessionId);
        Task<IReadOnlyList<SessionData>> GetSessionsInRangeAsync(DateTime start, DateTime end);
        Task<bool> UpdateSessionResultsAsync(SessionId sessionId, SessionResults results);

        // ショット関連
        Task<bool> SaveShotResultAsync(ShotResult result);
        Task<ShotResult?> GetShotResultAsync(ShotId shotId);
        Task<IReadOnlyList<ShotResult>> GetShotResultsBySessionAsync(SessionId sessionId);

        // 時系列データ関連
        Task<bool> SaveMotionDataAsync(MotionData data);
        Task<MotionData?> GetMotionDataAsync(ShotId shotId, MotionData.TimeSeriesDataType type);
    }

    public interface ITimeSeriesBuffer
    {
        void StartRecording(ShotId shotId, MotionData.TimeSeriesDataType type);
        void RecordDataPoint(TimeSeriesDataPoint dataPoint);
        MotionData? StopRecording();
        void Clear();
    }

    public interface IDataValidator
    {
        ValidationResult ValidateSessionConfig(SessionConfig config);
        ValidationResult ValidateSessionData(SessionData data);
        ValidationResult ValidateShotResult(ShotResult result);
        ValidationResult ValidateMotionData(MotionData data);
    }

    public class ValidationResult
    {
        private ValidationResult(bool isValid, IReadOnlyList<string> errors)
        {
            IsValid = isValid;
            Errors = errors;
        }

        public bool IsValid { get; }
        public IReadOnlyList<string> Errors { get; }

        public static ValidationResult Success()
        {
            return new ValidationResult(true, Array.Empty<string>());
        }

        public static ValidationResult Failure(IEnumerable<string> errors)
        {
            return new ValidationResult(false, errors.ToList());
        }
    }

    public interface IDataCompression
    {
        byte[] CompressMotionData(MotionData data);
        MotionData DecompressMotionData(byte[] compressed);
    }
}