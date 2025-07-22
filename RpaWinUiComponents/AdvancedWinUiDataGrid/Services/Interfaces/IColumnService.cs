// SÚBOR: Services/Interfaces/IColumnService.cs - OPRAVENÉ event types
using System;
using System.Collections.Generic;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// INTERNAL interface - nie je súčasťou public API
    /// ✅ OPRAVA CS0738: Používa ComponentErrorEventArgs namiesto InternalComponentErrorEventArgs
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

        // ✅ OPRAVA CS0738: Správny event type - ComponentErrorEventArgs
        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}