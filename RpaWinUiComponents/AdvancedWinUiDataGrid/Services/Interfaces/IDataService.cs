﻿//Services/Interfaces/IDataService.cs - OPRAVA TYPOV
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
// KĽÚČOVÁ OPRAVA: Používame internal typ
using ColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    public interface IDataService
    {
        Task InitializeAsync(List<ColumnDefinition> columns, int initialRowCount = 100);

        Task LoadDataAsync(DataTable dataTable);
        Task LoadDataAsync(List<Dictionary<string, object?>> data);
        Task<DataTable> ExportToDataTableAsync();

        Task ClearAllDataAsync();
        Task RemoveEmptyRowsAsync();
        Task RemoveRowsByConditionAsync(string columnName, Func<object?, bool> condition);
        Task<int> RemoveRowsByValidationAsync(List<ValidationRule> customRules);

        DataGridRow CreateEmptyRow();
        List<DataGridRow> GetRows();
        int GetRowCount();

        event EventHandler<DataChangeEventArgs> DataChanged;
        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}