// PUBLIC Event Args - NOVÝ SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/Events/PublicEventArgs.cs
using System;

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

// INTERNAL Event Args - zostávajú internal, použité iba vnútorne
namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Events
{
    /// <summary>
    /// INTERNAL - nie je vystavené v PUBLIC API
    /// </summary>
    internal class InternalComponentErrorEventArgs : EventArgs
    {
        public Exception Exception { get; }
        public string Operation { get; }
        public string? AdditionalInfo { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public InternalComponentErrorEventArgs(Exception exception, string operation, string? additionalInfo = null)
        {
            Exception = exception;
            Operation = operation;
            AdditionalInfo = additionalInfo;
        }

        // Konverzia na PUBLIC typ
        public ComponentErrorEventArgs ToPublic()
        {
            return new ComponentErrorEventArgs(Exception, Operation, AdditionalInfo);
        }
    }

    /// <summary>
    /// INTERNAL - pre navigation events
    /// </summary>
    internal class CellNavigationEventArgs : EventArgs
    {
        public int OldRowIndex { get; init; }
        public int OldColumnIndex { get; init; }
        public int NewRowIndex { get; init; }
        public int NewColumnIndex { get; init; }
        public object? OldCell { get; init; }
        public object? NewCell { get; init; }
        public string Direction { get; init; } = string.Empty;
    }

    /// <summary>
    /// INTERNAL - pre internal validation events
    /// </summary>
    internal class InternalValidationCompletedEventArgs : EventArgs
    {
        public object? Row { get; init; }
        public object? Cell { get; init; }
        public List<object> Results { get; init; } = new();
        public bool IsValid { get; init; }
        public TimeSpan TotalDuration { get; init; }
        public int AsyncValidationCount { get; init; }

        // Konverzia na PUBLIC typ
        public ValidationCompletedEventArgs ToPublic()
        {
            return new ValidationCompletedEventArgs(
                IsValid,
                Results.Count(r => !IsValid),
                TotalDuration,
                1
            );
        }
    }

    /// <summary>
    /// INTERNAL - pre data change events
    /// </summary>
    internal class InternalDataChangeEventArgs : EventArgs
    {
        public DataChangeType ChangeType { get; init; }
        public object? ChangedData { get; init; }
        public string? ColumnName { get; init; }
        public int RowIndex { get; init; } = -1;
        public int AffectedRowCount { get; init; }
        public TimeSpan OperationDuration { get; init; }

        // Konverzia na PUBLIC typ
        public DataChangeEventArgs ToPublic()
        {
            return new DataChangeEventArgs(
                ChangeType.ToString(),
                AffectedRowCount,
                OperationDuration
            );
        }
    }

    internal enum DataChangeType
    {
        Initialize,
        LoadData,
        ClearData,
        CellValueChanged,
        RemoveRows,
        RemoveEmptyRows,
        AddRows,
        RowValidationChanged
    }

    internal enum NavigationDirection
    {
        None,
        Next,
        Previous,
        Up,
        Down,
        Home,
        End
    }
}