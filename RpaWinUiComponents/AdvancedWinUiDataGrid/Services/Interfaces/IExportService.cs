//Services/Interfaces/IExportService.cs - OPRAVA TYPOV
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
// KĽÚČOVÁ OPRAVA: Používame internal typ
using ColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    public interface IExportService
    {
        Task<DataTable> ExportToDataTableAsync(List<DataGridRow> rows, List<ColumnDefinition> columns, bool includeValidAlerts = false);
        Task<string> ExportToCsvAsync(List<DataGridRow> rows, List<ColumnDefinition> columns, bool includeValidAlerts = false);
        Task<byte[]> ExportToExcelAsync(List<DataGridRow> rows, List<ColumnDefinition> columns, bool includeValidAlerts = false);

        Task<List<Dictionary<string, object?>>> ExportToDictionariesAsync(List<DataGridRow> rows, List<ColumnDefinition> columns);

        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}