//Services/Interfaces/IClipboardService.cs - FINÁLNA OPRAVA: INTERNAL + ODSTRÁNENÉ DUPLIKÁTY
using System.Collections.Generic;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// OPRAVENÉ: INTERNAL interface - nie je súčasťou public API
    /// OPRAVENÉ: Odstránené všetky duplicitné definície
    /// </summary>
    internal interface IClipboardService
    {
        Task<string?> GetClipboardTextAsync();
        Task SetClipboardTextAsync(string text);
        Task<bool> HasClipboardTextAsync();

        string ConvertToExcelFormat(string[,] data);
        string[,] ParseFromExcelFormat(string clipboardData);

        Task CopySelectedCellsAsync(IEnumerable<DataGridCell> selectedCells);
        Task<bool> PasteToPositionAsync(int startRowIndex, int startColumnIndex, List<DataGridRow> rows, List<ColumnDefinition> columns);

        event System.EventHandler<InternalComponentErrorEventArgs> ErrorOccurred;
    }
}