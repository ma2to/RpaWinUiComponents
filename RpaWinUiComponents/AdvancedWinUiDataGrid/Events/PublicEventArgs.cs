// Events/PublicEventArgs.cs - CLEAN PUBLIC API
using System;
using System.Collections.Generic;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// PUBLIC Event Args pre error handling - vystavené v PUBLIC API
    /// </summary>
    public class ComponentErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Operation { get; }
        public string? AdditionalInfo { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public ComponentErrorEventArgs(Exception exception, string operation, string? additionalInfo = null)
        {
            Exception = exception;
            Operation = operation;
            AdditionalInfo = additionalInfo;
        }

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Operation}: {Exception.Message}" +
                   (string.IsNullOrEmpty(AdditionalInfo) ? "" : $" - {AdditionalInfo}");
        }
    }

    /// <summary>
    /// PUBLIC Event Args pre data change notifications
    /// </summary>
    public class DataChangeEventArgs : EventArgs
    {
        public string ChangeType { get; init; } = string.Empty;
        public int AffectedRowCount { get; init; }
        public TimeSpan OperationDuration { get; init; }

        public DataChangeEventArgs(string changeType, int affectedRowCount, TimeSpan duration)
        {
            ChangeType = changeType;
            AffectedRowCount = affectedRowCount;
            OperationDuration = duration;
        }
    }

    /// <summary>
    /// PUBLIC Event Args pre validation completed
    /// </summary>
    public class ValidationCompletedEventArgs : EventArgs
    {
        public bool IsValid { get; init; }
        public int TotalErrors { get; init; }
        public TimeSpan TotalDuration { get; init; }
        public int ProcessedRows { get; init; }

        public ValidationCompletedEventArgs(bool isValid, int totalErrors, TimeSpan duration, int processedRows)
        {
            IsValid = isValid;
            TotalErrors = totalErrors;
            TotalDuration = duration;
            ProcessedRows = processedRows;
        }
    }
}