//Services/Interfaces/IColumnService.cs - OPRAVA TYPOV  
using System;
using System.Collections.Generic;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    public interface IColumnService
    {
        List<ColumnDefinition> ProcessColumnDefinitions(List<ColumnDefinition> columns);
        string GenerateUniqueColumnName(string baseName, List<string> existingNames);

        ColumnDefinition CreateDeleteActionColumn();
        ColumnDefinition CreateValidAlertsColumn();
        bool IsSpecialColumn(string columnName);

        List<ColumnDefinition> ReorderSpecialColumns(List<ColumnDefinition> columns);
        void ValidateColumnDefinitions(List<ColumnDefinition> columns);

        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}