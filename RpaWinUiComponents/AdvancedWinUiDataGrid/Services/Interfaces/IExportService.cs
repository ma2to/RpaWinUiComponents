// SÚBOR: Services/Interfaces/IExportService.cs - OPRAVENÉ event types  
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
    /// ✅ OPRAVA CS0051: Všetky types sú internal
    /// </summary>
    internal interface IExportService
    {
        // ✅ OPRAVA CS0051: internal types v internal interface
        Task<DataTable> ExportToDataTableAsync(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns, bool includeValidAlerts = false);
        Task<string> ExportToCsvAsync(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns, bool includeValidAlerts = false);
        Task<byte[]> ExportToExcelAsync(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns, bool includeValidAlerts = false);

        Task<List<Dictionary<string, object?>>> ExportToDictionariesAsync(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns);

        // ✅ OPRAVA CS0738: Správny event type
        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}