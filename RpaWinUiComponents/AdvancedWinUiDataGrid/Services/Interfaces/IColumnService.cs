//Services/Interfaces/IColumnService.cs - OPRAVENÉ
using System;
using System.Collections.Generic;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// INTERNAL interface - nie je súčasťou public API
    /// </summary>
    internal interface IColumnService
    {
        List<ColumnDefinition> ProcessColumnDefinitions(List<ColumnDefinition> columns);
        string GenerateUniqueColumnName(string baseName, List<string> existingNames);

        ColumnDefinition CreateDeleteActionColumn();
        ColumnDefinition CreateValidAlertsColumn();
        bool IsSpecialColumn(string columnName);

        List<ColumnDefinition> ReorderSpecialColumns(List<ColumnDefinition> columns);
        void ValidateColumnDefinitions(List<ColumnDefinition> columns);

        // OPRAVENÉ: Správne internal event type
        event EventHandler<InternalComponentErrorEventArgs> ErrorOccurred;
    }
}