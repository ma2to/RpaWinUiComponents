// SÚBOR: Services/Interfaces/IClipboardService.cs - OPRAVENÉ event types
using System.Collections.Generic;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// INTERNAL interface - nie je súčasťou public API
    /// ✅ OPRAVA CS0738: Správny event type
    /// </summary>
    internal interface IClipboardService
    {
        Task<string?> GetClipboardTextAsync();
        Task SetClipboardTextAsync(string text);
        Task<bool> HasClipboardTextAsync();

        string ConvertToExcelFormat(string[,] data);
        string[,] ParseFromExcelFormat(string clipboardData);

        // ✅ OPRAVA CS0051: internal parameters
        Task CopySelectedCellsAsync(IEnumerable<DataGridCell> selectedCells);
        Task<bool> PasteToPositionAsync(int startRowIndex, int startColumnIndex, IList<DataGridRow> rows, IList<ColumnDefinition> columns);

        // ✅ OPRAVA CS0738: Správny event type
        event EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}