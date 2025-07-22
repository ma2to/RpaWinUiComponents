//Services/Interfaces/IColumnService.cs - FINÁLNA OPRAVA: INTERNAL
using System;
using System.Collections.Generic;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// OPRAVENÉ: INTERNAL interface - nie je súčasťou public API
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

        event EventHandler<InternalComponentErrorEventArgs> ErrorOccurred;
    }
}