// SÚBOR: Services/Interfaces/INavigationService.cs - OPRAVENÉ event types
using System;
using System.Collections.Generic;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// INTERNAL interface - nie je súčasťou public API
    /// ✅ OPRAVA CS0051, CS0738: internal types, správny event type
    /// </summary>
    internal interface INavigationService
    {
        // ✅ OPRAVA CS0051: internal parameters
        void Initialize(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns);

        void MoveToNextCell();
        void MoveToPreviousCell();
        void MoveToNextRow();
        void MoveToPreviousRow();
        void MoveToCell(int rowIndex, int columnIndex);

        DataGridCell? CurrentCell { get; }
        int CurrentRowIndex { get; }
        int CurrentColumnIndex { get; }

        event EventHandler<CellNavigationEventArgs> CellChanged;
        // ✅ OPRAVA CS0738: Správny event type
        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}