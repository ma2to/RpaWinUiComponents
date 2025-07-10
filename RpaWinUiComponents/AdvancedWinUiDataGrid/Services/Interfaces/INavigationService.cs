﻿//Services/Interfaces/INavigationService.cs - OPRAVA TYPOV
using System;
using System.Collections.Generic;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
// KĽÚČOVÁ OPRAVA: Používame internal typ
using ColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    public interface INavigationService
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
        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}