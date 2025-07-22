//Services/Interfaces/IDataService.cs - FINÁLNA OPRAVA: INTERNAL
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// OPRAVENÉ: INTERNAL interface - nie je súčasťou public API
    /// </summary>
    internal interface IDataService
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

        event EventHandler<InternalDataChangeEventArgs> DataChanged;
        event EventHandler<InternalComponentErrorEventArgs> ErrorOccurred;
    }
}