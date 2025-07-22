// SÚBOR: Services/Interfaces/IDataService.cs - OPRAVENÉ event types
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// INTERNAL interface - nie je súčasťou public API
    /// ✅ OPRAVA CS0051: internal parameters, správne event types
    /// </summary>
    internal interface IDataService
    {
        // ✅ OPRAVA CS0051: internal parameters  
        Task InitializeAsync(IList<ColumnDefinition> columns, int initialRowCount = 100);

        Task LoadDataAsync(DataTable dataTable);
        Task LoadDataAsync(List<Dictionary<string, object?>> data);
        Task<DataTable> ExportToDataTableAsync();

        Task ClearAllDataAsync();
        Task RemoveEmptyRowsAsync();
        Task RemoveRowsByConditionAsync(string columnName, Func<object?, bool> condition);
        Task<int> RemoveRowsByValidationAsync(IList<ValidationRule> customRules);

        DataGridRow CreateEmptyRow();
        IList<DataGridRow> GetRows();
        int GetRowCount();

        // ✅ OPRAVA CS0738: Správne event types
        event EventHandler<DataChangeEventArgs> DataChanged;
        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}