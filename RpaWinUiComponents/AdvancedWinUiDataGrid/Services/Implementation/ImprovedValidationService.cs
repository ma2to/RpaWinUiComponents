// OPRAVA 1,3,4,6: Improved ValidationService - Memory + Performance + Error Handling
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/Services/Implementation/ImprovedValidationService.cs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces;
using RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Implementation
{
    /// <summary>
    /// Improved ValidationService s performance a memory optimizations
    /// OPRAVA: Riešenie memory leaks, performance issues a proper error handling
    /// </summary>
    public class ImprovedValidationService : IValidationService, IDisposable
    {
        private readonly ILogger<ImprovedValidationService> _logger;
        private readonly ConcurrentDictionary<string, List<ValidationRule>> _validationRules = new();
        private readonly SemaphoreSlim _validationSemaphore;
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _pendingValidations = new();

        // OPRAVA 1: Memory Management - WeakReference tracking
        private readonly ConcurrentDictionary<string, WeakReference<CellViewModel>> _cellReferences = new();
        private readonly Timer _cleanupTimer;
        private bool _disposed;

        // OPRAVA 4: Performance Optimization
        private readonly ConcurrentDictionary<string, DateTime> _lastValidationTime = new();
        private readonly TimeSpan _minimumValidationInterval = TimeSpan.FromMilliseconds(50);

        // OPRAVA 6: Error Handling & Recovery
        private int _consecutiveErrors;
        private DateTime _lastErrorTime = DateTime.MinValue;
        private readonly TimeSpan _errorCooldownPeriod = TimeSpan.FromSeconds(5);

        public event EventHandler<ValidationCompletedEventArgs>? ValidationCompleted;
        public event EventHandler<ComponentErrorEventArgs>? ValidationErrorOccurred;

        public ImprovedValidationService(ILogger<ImprovedValidationService> logger, int maxConcurrentValidations = 5)
        {
            _logger = logger;
            _validationSemaphore = new SemaphoreSlim(maxConcurrentValidations, maxConcurrentValidations);

            // OPRAVA 1: Memory cleanup timer
            _cleanupTimer = new Timer(CleanupWeakReferences, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            _logger.LogDebug("ImprovedValidationService initialized with {MaxConcurrent} concurrent validations", maxConcurrentValidations);
        }

        #region Core Validation Methods (OPRAVA 3,4: Performance + Proper Validation)

        /// <summary>
        /// Async validácia bunky s performance optimizations
        /// </summary>
        public async Task<ValidationResult> ValidateCellAsync(CellViewModel cell, RowViewModel row, CancellationToken cancellationToken = default)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));
            if (row == null) throw new ArgumentNullException(nameof(row));

            var stopwatch = Stopwatch.StartNew();
            var cellKey = cell.CellKey;

            try
            {
                // OPRAVA 4: Performance - Skip validation if too frequent
                if (ShouldSkipValidation(cellKey))
                {
                    return ValidationResult.Success(cell.ColumnName, cell.RowIndex);
                }

                // OPRAVA 6: Error Handling - Circuit breaker pattern
                if (IsInErrorCooldown())
                {
                    _logger.LogWarning("Validation service in error cooldown, skipping validation for {CellKey}", cellKey);
                    return ValidationResult.Success(cell.ColumnName, cell.RowIndex);
                }

                // Track cell reference for memory management
                TrackCellReference(cell);

                // Cancel previous validation for this cell
                CancelPendingValidation(cellKey);

                // Check if row is empty
                if (row.IsEmpty)
                {
                    cell.ClearValidationErrors();
                    return ValidationResult.Success(cell.ColumnName, cell.RowIndex);
                }

                // Get validation rules for this column
                if (!_validationRules.TryGetValue(cell.ColumnName, out var rules) || rules.Count == 0)
                {
                    cell.ClearValidationErrors();
                    return ValidationResult.Success(cell.ColumnName, cell.RowIndex);
                }

                // Create cancellation token for this validation
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _pendingValidations[cellKey] = cts;

                try
                {
                    await _validationSemaphore.WaitAsync(cts.Token);

                    try
                    {
                        var applicableRules = rules
                            .Where(r => r.ShouldApply(ConvertToDataGridRow(row)))
                            .OrderByDescending(r => r.Priority)
                            .ToList();

                        var errorMessages = new List<string>();
                        var hasAsyncValidation = false;

                        foreach (var rule in applicableRules)
                        {
                            try
                            {
                                bool isValid;

                                if (rule.IsAsync)
                                {
                                    hasAsyncValidation = true;
                                    isValid = await rule.ValidateAsync(cell.Value, ConvertToDataGridRow(row), cts.Token);
                                }
                                else
                                {
                                    isValid = rule.Validate(cell.Value, ConvertToDataGridRow(row));
                                }

                                if (!isValid)
                                {
                                    errorMessages.Add(rule.ErrorMessage);
                                    _logger.LogDebug("Validation failed: {RuleName} for {CellKey}", rule.RuleName, cellKey);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                throw; // Re-throw cancellation
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Validation rule {RuleName} failed with exception for {CellKey}", rule.RuleName, cellKey);
                                OnValidationError(ex, $"Rule {rule.RuleName}");
                                errorMessages.Add($"Validation error: {rule.ErrorMessage}");
                            }
                        }

                        // OPRAVA 3: Proper validation error handling
                        cell.SetValidationErrors(nameof(CellViewModel.Value), errorMessages);

                        // Update last validation time
                        _lastValidationTime[cellKey] = DateTime.UtcNow;

                        var result = new ValidationResult(errorMessages.Count == 0)
                        {
                            ErrorMessages = errorMessages,
                            ColumnName = cell.ColumnName,
                            RowIndex = cell.RowIndex,
                            ValidationDuration = stopwatch.Elapsed,
                            WasAsync = hasAsyncValidation
                        };

                        // Reset error counter on success
                        if (result.IsValid)
                        {
                            Interlocked.Exchange(ref _consecutiveErrors, 0);
                        }

                        return result;
                    }
                    finally
                    {
                        _validationSemaphore.Release();
                    }
                }
                finally
                {
                    _pendingValidations.TryRemove(cellKey, out _);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Validation cancelled for {CellKey}", cellKey);
                return ValidationResult.Failure("Validation was cancelled", cell.ColumnName, cell.RowIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cell {CellKey}", cellKey);
                OnValidationError(ex, "ValidateCellAsync");
                return ValidationResult.Failure($"Validation error: {ex.Message}", cell.ColumnName, cell.RowIndex);
            }
        }

        /// <summary>
        /// Batch validation s performance optimizations
        /// </summary>
        public async Task<List<ValidationResult>> ValidateRowAsync(RowViewModel row, CancellationToken cancellationToken = default)
        {
            if (row == null) throw new ArgumentNullException(nameof(row));

            var results = new List<ValidationResult>();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (row.IsEmpty)
                {
                    // Clear all validation errors for empty row
                    foreach (var cell in row.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                    {
                        cell.ClearValidationErrors();
                    }
                    row.UpdateValidationStatus();
                    return results;
                }

                // OPRAVA 4: Performance - Parallel validation with controlled concurrency
                var cellsToValidate = row.Cells
                    .Where(c => !IsSpecialColumn(c.ColumnName) && _validationRules.ContainsKey(c.ColumnName))
                    .ToList();

                var validationTasks = cellsToValidate.Select(cell =>
                    ValidateCellAsync(cell, row, cancellationToken));

                var cellResults = await Task.WhenAll(validationTasks);
                results.AddRange(cellResults);

                row.UpdateValidationStatus();

                OnValidationCompleted(new ValidationCompletedEventArgs
                {
                    Row = ConvertToDataGridRow(row),
                    Results = results,
                    TotalDuration = stopwatch.Elapsed,
                    AsyncValidationCount = results.Count(r => r.WasAsync)
                });

                _logger.LogDebug("Row validation completed: {ValidCount} valid, {InvalidCount} invalid",
                    results.Count(r => r.IsValid), results.Count(r => !r.IsValid));

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating row {RowIndex}", row.RowIndex);
                OnValidationError(ex, "ValidateRowAsync");
                return results;
            }
        }

        /// <summary>
        /// Bulk validation with progress reporting a performance optimizations
        /// </summary>
        public async Task<List<ValidationResult>> ValidateAllRowsAsync(IEnumerable<RowViewModel> rows, IProgress<double>? progress = null, CancellationToken cancellationToken = default)
        {
            var allResults = new List<ValidationResult>();
            var dataRows = rows?.Where(r => !r.IsEmpty).ToList() ?? new List<RowViewModel>();

            if (dataRows.Count == 0)
            {
                _logger.LogInformation("No non-empty rows to validate");
                return allResults;
            }

            try
            {
                _logger.LogInformation("Validating {RowCount} non-empty rows", dataRows.Count);

                // OPRAVA 4: Performance - Adaptive batch size based on row count
                var batchSize = CalculateOptimalBatchSize(dataRows.Count);
                var totalRows = dataRows.Count;
                var processedRows = 0;

                for (int i = 0; i < dataRows.Count; i += batchSize)
                {
                    var batch = dataRows.Skip(i).Take(batchSize).ToList();

                    // OPRAVA 4: Performance - Parallel processing within batch
                    var batchTasks = batch.Select(row => ValidateRowAsync(row, cancellationToken));
                    var batchResults = await Task.WhenAll(batchTasks);

                    foreach (var rowResults in batchResults)
                    {
                        allResults.AddRange(rowResults);
                    }

                    processedRows += batch.Count;
                    var progressPercentage = (double)processedRows / totalRows * 100;
                    progress?.Report(progressPercentage);

                    _logger.LogDebug("Validated batch: {ProcessedRows}/{TotalRows} rows ({Progress:F1}%)",
                        processedRows, totalRows, progressPercentage);

                    cancellationToken.ThrowIfCancellationRequested();

                    // OPRAVA 4: Performance - Brief pause between batches to prevent UI freezing
                    if (i + batchSize < dataRows.Count)
                    {
                        await Task.Delay(1, cancellationToken);
                    }
                }

                var validCount = allResults.Count(r => r.IsValid);
                var invalidCount = allResults.Count(r => !r.IsValid);

                _logger.LogInformation("Validation completed: {ValidCount} valid, {InvalidCount} invalid results",
                    validCount, invalidCount);

                return allResults;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Validation of all rows was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating all rows");
                OnValidationError(ex, "ValidateAllRowsAsync");
                return allResults;
            }
        }

        #endregion

        #region Rule Management (ZACHOVANÉ: Existing API)

        public void AddValidationRule(ValidationRule rule)
        {
            try
            {
                if (rule == null) throw new ArgumentNullException(nameof(rule));
                if (string.IsNullOrWhiteSpace(rule.ColumnName))
                    throw new ArgumentException("ColumnName cannot be null or empty", nameof(rule));

                _validationRules.AddOrUpdate(
                    rule.ColumnName,
                    new List<ValidationRule> { rule },
                    (key, existingRules) =>
                    {
                        // OPRAVA 1: Memory - Remove old rule with same name to prevent duplicates
                        existingRules.RemoveAll(r => r.RuleName == rule.RuleName);
                        existingRules.Add(rule);
                        return existingRules;
                    });

                _logger.LogDebug("Added validation rule '{RuleName}' for column '{ColumnName}'",
                    rule.RuleName, rule.ColumnName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding validation rule");
                OnValidationError(ex, "AddValidationRule");
            }
        }

        public void RemoveValidationRule(string columnName, string ruleName)
        {
            try
            {
                if (_validationRules.TryGetValue(columnName, out var rules))
                {
                    var removedCount = rules.RemoveAll(r => r.RuleName == ruleName);
                    _logger.LogDebug("Removed {RemovedCount} validation rule(s) '{RuleName}' from column '{ColumnName}'",
                        removedCount, ruleName, columnName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing validation rule '{RuleName}' from column '{ColumnName}'",
                    ruleName, columnName);
                OnValidationError(ex, "RemoveValidationRule");
            }
        }

        public void ClearValidationRules(string? columnName = null)
        {
            try
            {
                if (columnName == null)
                {
                    var totalRules = _validationRules.Values.Sum(rules => rules.Count);
                    _validationRules.Clear();
                    _logger.LogInformation("Cleared all {TotalRules} validation rules", totalRules);
                }
                else if (_validationRules.TryGetValue(columnName, out var rules))
                {
                    var ruleCount = rules.Count;
                    rules.Clear();
                    _logger.LogDebug("Cleared {RuleCount} validation rules from column '{ColumnName}'",
                        ruleCount, columnName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing validation rules for column: {ColumnName}", columnName ?? "ALL");
                OnValidationError(ex, "ClearValidationRules");
            }
        }

        public List<ValidationRule> GetValidationRules(string columnName)
        {
            try
            {
                return _validationRules.TryGetValue(columnName, out var rules)
                    ? new List<ValidationRule>(rules)
                    : new List<ValidationRule>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting validation rules for column: {ColumnName}", columnName);
                OnValidationError(ex, "GetValidationRules");
                return new List<ValidationRule>();
            }
        }

        public bool HasValidationRules(string columnName)
        {
            return _validationRules.TryGetValue(columnName, out var rules) && rules.Count > 0;
        }

        public int GetTotalRuleCount()
        {
            return _validationRules.Values.Sum(rules => rules.Count);
        }

        #endregion

        #region Private Methods (OPRAVA 1,4,6: Memory + Performance + Error Handling)

        private bool ShouldSkipValidation(string cellKey)
        {
            if (!_lastValidationTime.TryGetValue(cellKey, out var lastTime))
                return false;

            var elapsed = DateTime.UtcNow - lastTime;
            return elapsed < _minimumValidationInterval;
        }

        private bool IsInErrorCooldown()
        {
            if (_consecutiveErrors < 5) return false;

            var elapsed = DateTime.UtcNow - _lastErrorTime;
            return elapsed < _errorCooldownPeriod;
        }

        private void OnValidationError(Exception ex, string operation)
        {
            Interlocked.Increment(ref _consecutiveErrors);
            _lastErrorTime = DateTime.UtcNow;

            OnValidationErrorOccurred(new ComponentErrorEventArgs(ex, operation));
        }

        private int CalculateOptimalBatchSize(int totalRows)
        {
            // OPRAVA 4: Performance - Adaptive batch size
            return totalRows switch
            {
                <= 50 => 10,
                <= 200 => 20,
                <= 1000 => 50,
                _ => 100
            };
        }

        private void TrackCellReference(CellViewModel cell)
        {
            // OPRAVA 1: Memory Management - Use WeakReference
            _cellReferences[cell.CellKey] = new WeakReference<CellViewModel>(cell);
        }

        private void CleanupWeakReferences(object? state)
        {
            try
            {
                var keysToRemove = new List<string>();

                foreach (var kvp in _cellReferences)
                {
                    if (!kvp.Value.TryGetTarget(out _))
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }

                foreach (var key in keysToRemove)
                {
                    _cellReferences.TryRemove(key, out _);
                    _lastValidationTime.TryRemove(key, out _);
                }

                if (keysToRemove.Count > 0)
                {
                    _logger.LogDebug("Cleaned up {Count} dead cell references", keysToRemove.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during cleanup of weak references");
            }
        }

        private void CancelPendingValidation(string cellKey)
        {
            if (_pendingValidations.TryRemove(cellKey, out var cts))
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch
                {
                    // Ignore cancellation errors
                }
            }
        }

        private static bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        // OPRAVA 7: Code Quality - Helper conversion methods
        private static DataGridRow ConvertToDataGridRow(RowViewModel rowViewModel)
        {
            var dataGridRow = new DataGridRow(rowViewModel.RowIndex)
            {
                IsEvenRow = rowViewModel.IsEvenRow
            };

            foreach (var cellVM in rowViewModel.Cells)
            {
                var cell = new DataGridCell(cellVM.ColumnName, cellVM.DataType, cellVM.RowIndex, cellVM.ColumnIndex)
                {
                    Value = cellVM.Value,
                    OriginalValue = cellVM.OriginalValue,
                    IsReadOnly = cellVM.IsReadOnly
                };

                if (cellVM.HasValidationErrors)
                {
                    cell.SetValidationErrors(new[] { cellVM.ValidationErrorsText });
                }

                dataGridRow.AddCell(cellVM.ColumnName, cell);
            }

            return dataGridRow;
        }

        #endregion

        #region Event Handlers

        protected virtual void OnValidationCompleted(ValidationCompletedEventArgs e)
        {
            ValidationCompleted?.Invoke(this, e);
        }

        protected virtual void OnValidationErrorOccurred(ComponentErrorEventArgs e)
        {
            ValidationErrorOccurred?.Invoke(this, e);
        }

        #endregion

        #region IDisposable (OPRAVA 1: Proper Memory Management)

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger.LogDebug("Disposing ImprovedValidationService...");

                // Cancel all pending validations
                foreach (var cts in _pendingValidations.Values)
                {
                    try
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }
                    catch { }
                }
                _pendingValidations.Clear();

                // Dispose resources
                _cleanupTimer?.Dispose();
                _validationSemaphore?.Dispose();

                // Clear collections
                _validationRules.Clear();
                _cellReferences.Clear();
                _lastValidationTime.Clear();

                // Clear events
                ValidationCompleted = null;
                ValidationErrorOccurred = null;

                _disposed = true;
                _logger.LogInformation("ImprovedValidationService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ImprovedValidationService disposal");
            }
        }

        #endregion
    }
}