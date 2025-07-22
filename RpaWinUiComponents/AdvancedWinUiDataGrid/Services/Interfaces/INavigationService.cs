//Services/Interfaces/INavigationService.cs - OPRAVENÉ event types  
using System;
using System.Collections.Generic;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// INTERNAL interface - nie je súčasťou public API
    /// </summary>
    internal interface INavigationService
    {
        void Initialize(List<DataGridRow> rows, List<ColumnDefinition> columns);

        void MoveToNextCell();
        void MoveToPreviousCell();
        void MoveToNextRow();
        void MoveToPreviousRow();
        void MoveToCell(int rowIndex, int columnIndex);

        DataGridCell? CurrentCell { get; }
        int CurrentRowIndex { get; }
        int CurrentColumnIndex { get; }

        event EventHandler<CellNavigationEventArgs> CellChanged;
        // OPRAVENÉ: Správne internal event type
        event EventHandler<InternalComponentErrorEventArgs> ErrorOccurred;
    }
}