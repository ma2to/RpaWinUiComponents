//Services/Interfaces/IClipboardService.cs - OPRAVA TYPOV
using System.Collections.Generic;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
// KĽÚČOVÁ OPRAVA: Používame internal typ
using ColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    public interface IClipboardService
    {
        Task<string?> GetClipboardTextAsync();
        Task SetClipboardTextAsync(string text);
        Task<bool> HasClipboardTextAsync();

        string ConvertToExcelFormat(string[,] data);
        string[,] ParseFromExcelFormat(string clipboardData);

        Task CopySelectedCellsAsync(IEnumerable<DataGridCell> selectedCells);
        Task<bool> PasteToPositionAsync(int startRowIndex, int startColumnIndex, List<DataGridRow> rows, List<ColumnDefinition> columns);

        event System.EventHandler<ComponentErrorEventArgs> ErrorOccurred;
    }
}