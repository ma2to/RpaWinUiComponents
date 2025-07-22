// SÚBOR: Services/Interfaces/IValidationService.cs - OPRAVENÉ event types
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces
{
    /// <summary>
    /// INTERNAL interface - nie je súčasťou public API
    /// ✅ OPRAVA CS0738, CS0050, CS0051: Všetko internal, správne event types
    /// </summary>
    internal interface IValidationService
    {
        // ✅ OPRAVA CS0050, CS0051: internal types v internal methods
        Task<ValidationResult> ValidateCellAsync(DataGridCell cell, DataGridRow row, CancellationToken cancellationToken = default);
        Task<IList<ValidationResult>> ValidateRowAsync(DataGridRow row, CancellationToken cancellationToken = default);
        Task<IList<ValidationResult>> ValidateAllRowsAsync(IEnumerable<DataGridRow> rows, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

        void AddValidationRule(ValidationRule rule);
        void RemoveValidationRule(string columnName, string ruleName);
        void ClearValidationRules(string? columnName = null);

        List<ValidationRule> GetValidationRules(string columnName);
        bool HasValidationRules(string columnName);
        int GetTotalRuleCount();

        // ✅ OPRAVA CS0738: Správne event types - ComponentErrorEventArgs
        event EventHandler<ValidationCompletedEventArgs> ValidationCompleted;
        event EventHandler<ComponentErrorEventArgs> ValidationErrorOccurred;
    }
}