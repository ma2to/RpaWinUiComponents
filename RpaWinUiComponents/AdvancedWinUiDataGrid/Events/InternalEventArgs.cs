//Events/InternalEventArgs.cs - OPRAVENÉ: ODSTRÁNENÉ DUPLIKÁTY
using System;
using System.Collections.Generic;
using System.Linq;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Events
{
    /// <summary>
    /// INTERNAL Event Args pre data changes - nie je súčasťou public API
    /// </summary>
    internal class InternalDataChangeEventArgs : EventArgs
    {
        public string ChangeType { get; init; } = string.Empty;
        public object? ChangedData { get; init; }
        public string? ColumnName { get; init; }
        public int RowIndex { get; init; } = -1;
        public int AffectedRowCount { get; init; }
        public TimeSpan OperationDuration { get; init; }
    }

    /// <summary>
    /// INTERNAL Event Args pre component errors - nie je súčasťou public API
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

        public override string ToString()
        {
            return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Operation}: {Exception.Message}" +
                   (string.IsNullOrEmpty(AdditionalInfo) ? "" : $" - {AdditionalInfo}");
        }
    }

    /// <summary>
    /// INTERNAL Event Args pre validation completed - nie je súčasťou public API
    /// </summary>
    internal class InternalValidationCompletedEventArgs : EventArgs
    {
        public DataGridRow? Row { get; init; }
        public DataGridCell? Cell { get; init; }
        public List<ValidationResult> Results { get; init; } = new();
        public bool IsValid => Results.All(r => r.IsValid);
        public TimeSpan TotalDuration { get; init; }
        public int AsyncValidationCount { get; init; }
    }

    // OPRAVENÉ: ODSTRÁNENÉ DUPLIKÁTY - enum-y sú definované v špecializovaných súboroch
    // DataChangeType je v DataChangeEventArgs.cs
    // NavigationDirection je v CellNavigationEventArgs.cs
}