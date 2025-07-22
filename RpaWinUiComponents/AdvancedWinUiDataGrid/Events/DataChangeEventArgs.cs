//Events/DataChangeEventArgs.cs - OPRAVENÉ bez duplikátov
using System;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Events
{
    /// <summary>
    /// INTERNAL - Data change event args
    /// </summary>
    internal class DataChangeEventArgs : EventArgs
    {
        public DataChangeType ChangeType { get; init; }
        public object? ChangedData { get; init; }
        public string? ColumnName { get; init; }
        public int RowIndex { get; init; } = -1;
        public int AffectedRowCount { get; init; }
        public TimeSpan OperationDuration { get; init; }
    }

    /// <summary>
    /// INTERNAL - Data change type enum (JEDINÁ DEFINÍCIA)
    /// </summary>
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
}