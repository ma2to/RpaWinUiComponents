//ViewModels/AdvancedDataGridViewModel.cs - ✅ OPRAVENÝ: internal class
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Collections;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Commands;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces;
using RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels
{
    /// <summary>
    /// ✅ OPRAVA CS0051: INTERNAL ViewModel - nie je súčasťou public API
    /// </summary>
    internal class AdvancedDataGridViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Fields & Dependencies

        private readonly IDataService _dataService;
        private readonly IValidationService _validationService;
        private readonly IClipboardService _clipboardService;
        private readonly IColumnService _columnService;
        private readonly IExportService _exportService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<AdvancedDataGridViewModel> _logger;

        #endregion

        #region State Management

        private ObservableRangeCollection<RowViewModel> _rows = new();
        private ObservableCollection<ColumnDefinition> _columns = new();
        private readonly ObservableCollection<CellViewModel> _visibleCells = new();

        private bool _isValidating = false;
        private double _validationProgress = 0;
        private string _validationStatus = "Pripravené";
        private bool _isInitialized = false;
        private ThrottlingConfig _throttlingConfig = ThrottlingConfig.Default;
        private bool _isKeyboardShortcutsVisible = false;
        private int _initialRowCount = 100;
        private bool _disposed = false;
        private bool _isLoadingData = false;

        #endregion

        // ✅ OPRAVA CS0051: INTERNAL constructor s internal parameters
        public AdvancedDataGridViewModel(
            IDataService dataService,
            IValidationService validationService,
            IClipboardService clipboardService,
            IColumnService columnService,
            IExportService exportService,
            INavigationService navigationService,
            ILogger<AdvancedDataGridViewModel> logger)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _clipboardService = clipboardService ?? throw new ArgumentNullException(nameof(clipboardService));
            _columnService = columnService ?? throw new ArgumentNullException(nameof(columnService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _logger = logger;

            InitializeCommands();
            SubscribeToEvents();

            _logger.LogDebug("AdvancedDataGridViewModel created successfully");
        }

        #region Properties - ✅ OPRAVA CS0053: INTERNAL properties

        public ObservableRangeCollection<RowViewModel> Rows
        {
            get
            {
                ThrowIfDisposed();
                return _rows;
            }
            set => SetProperty(ref _rows, value);
        }

        public ObservableCollection<ColumnDefinition> Columns
        {
            get
            {
                ThrowIfDisposed();
                return _columns;
            }
            set => SetProperty(ref _columns, value);
        }

        public ObservableCollection<CellViewModel> VisibleCells
        {
            get
            {
                ThrowIfDisposed();
                return _visibleCells;
            }
        }

        public bool IsValidating
        {
            get => _isValidating;
            set => SetProperty(ref _isValidating, value);
        }

        public double ValidationProgress
        {
            get => _validationProgress;
            set => SetProperty(ref _validationProgress, value);
        }

        public string ValidationStatus
        {
            get => _validationStatus;
            set => SetProperty(ref _validationStatus, value);
        }

        public bool IsInitialized
        {
            get
            {
                if (_disposed) return false;
                return _isInitialized;
            }
            private set => SetProperty(ref _isInitialized, value);
        }

        public ThrottlingConfig ThrottlingConfig
        {
            get
            {
                ThrowIfDisposed();
                return _throttlingConfig;
            }
            private set => SetProperty(ref _throttlingConfig, value);
        }

        public bool IsKeyboardShortcutsVisible
        {
            get => _isKeyboardShortcutsVisible;
            set => SetProperty(ref _isKeyboardShortcutsVisible, value);
        }

        public INavigationService NavigationService
        {
            get
            {
                ThrowIfDisposed();
                return _navigationService;
            }
        }

        public int InitialRowCount
        {
            get
            {
                ThrowIfDisposed();
                return _initialRowCount;
            }
        }

        public bool IsLoadingData
        {
            get => _isLoadingData;
            private set => SetProperty(ref _isLoadingData, value);
        }

        #endregion

        #region Commands

        public ICommand ValidateAllCommand { get; private set; } = null!;
        public ICommand ClearAllDataCommand { get; private set; } = null!;
        public ICommand RemoveEmptyRowsCommand { get; private set; } = null!;
        public ICommand CopyCommand { get; private set; } = null!;
        public ICommand PasteCommand { get; private set; } = null!;
        public ICommand DeleteRowCommand { get; private set; } = null!;
        public ICommand ExportToDataTableCommand { get; private set; } = null!;
        public ICommand ToggleKeyboardShortcutsCommand { get; private set; } = null!;

        #endregion

        #region Public Methods

        public async Task InitializeAsync(
            List<ColumnDefinition> columnDefinitions,
            List<ValidationRule>? validationRules = null,
            ThrottlingConfig? throttling = null,
            int initialRowCount = 100)
        {
            ThrowIfDisposed();

            try
            {
                if (IsInitialized)
                {
                    _logger.LogWarning("Component already initialized. Call Reset() first if needed.");
                    return;
                }

                IsLoadingData = true;

                _initialRowCount = Math.Max(1, Math.Min(initialRowCount, 10000));
                ThrottlingConfig = throttling ?? ThrottlingConfig.Default;

                if (!ThrottlingConfig.IsValidConfig(out var configError))
                {
                    throw new ArgumentException($"Invalid throttling config: {configError}");
                }

                _logger.LogInformation("Initializing AdvancedDataGrid with {ColumnCount} columns, {RuleCount} validation rules, {InitialRowCount} rows",
                    columnDefinitions?.Count ?? 0, validationRules?.Count ?? 0, _initialRowCount);

                // Process and validate columns
                var processedColumns = _columnService.ProcessColumnDefinitions(columnDefinitions ?? new List<ColumnDefinition>());
                _columnService.ValidateColumnDefinitions(processedColumns);

                // Reorder special columns to the end
                var reorderedColumns = _columnService.ReorderSpecialColumns(processedColumns);

                // Initialize data service
                await _dataService.InitializeAsync(reorderedColumns, _initialRowCount);

                // Update UI collections
                Columns.Clear();
                foreach (var column in reorderedColumns)
                {
                    Columns.Add(column);
                }

                // Add validation rules
                if (validationRules != null)
                {
                    foreach (var rule in validationRules)
                    {
                        _validationService.AddValidationRule(rule);
                    }
                    _logger.LogDebug("Added {RuleCount} validation rules", validationRules.Count);
                }

                // Create initial rows
                await CreateInitialRowViewModelsAsync();

                // Initialize navigation service
                var dataRows = Rows.Select(r => ConvertToDataGridRow(r)).ToList();
                _navigationService.Initialize(dataRows, reorderedColumns);

                IsLoadingData = false;
                IsInitialized = true;

                _logger.LogInformation("AdvancedDataGrid initialization completed: {ActualRowCount} rows created",
                    Rows.Count);

            }
            catch (Exception ex)
            {
                IsLoadingData = false;
                IsInitialized = false;
                _logger.LogError(ex, "Error during initialization");
                HandleGlobalError(ex, "InitializeAsync");
                throw;
            }
        }

        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            ThrowIfDisposed();

            try
            {
                if (!IsInitialized)
                {
                    _logger.LogWarning("Component nie je inicializovaný, spúšťam auto-inicializáciu");
                    await AutoInitializeFromDataAsync(data);
                }

                IsLoadingData = true;
                UpdateValidationStatus("Načítavam dáta...");

                _logger.LogInformation("📊 Načítavam {RowCount} riadkov dát", data?.Count ?? 0);

                var newRowViewModels = new List<RowViewModel>();
                var rowIndex = 0;
                var totalRows = data?.Count ?? 0;

                if (data != null)
                {
                    foreach (var dataRow in data)
                    {
                        var rowViewModel = CreateRowViewModelForLoading(rowIndex);

                        _logger.LogTrace("Loading row {RowIndex}/{TotalRows}", rowIndex + 1, totalRows);

                        foreach (var column in Columns.Where(c => !IsSpecialColumn(c.Name)))
                        {
                            if (dataRow.ContainsKey(column.Name))
                            {
                                rowViewModel.SetValueSilently(column.Name, dataRow[column.Name]);
                            }
                        }

                        await ValidateRowViewModelAfterLoadingAsync(rowViewModel);

                        newRowViewModels.Add(rowViewModel);
                        rowIndex++;

                        var progress = (double)rowIndex / totalRows * 90;
                        UpdateValidationProgress(progress);
                    }
                }

                // Add empty rows for future data
                var minEmptyRows = Math.Min(10, _initialRowCount / 5);
                var finalRowCount = Math.Max(_initialRowCount, totalRows + minEmptyRows);

                while (newRowViewModels.Count < finalRowCount)
                {
                    newRowViewModels.Add(CreateEmptyRowViewModel(newRowViewModels.Count));
                }

                Rows.Clear();
                Rows.AddRange(newRowViewModels);

                UpdateValidationStatus("Validácia dokončená");
                UpdateValidationProgress(100);

                var validRows = newRowViewModels.Count(r => !r.IsEmpty && !r.HasValidationErrors);
                var invalidRows = newRowViewModels.Count(r => !r.IsEmpty && r.HasValidationErrors);
                var emptyRows = newRowViewModels.Count - totalRows;

                _logger.LogInformation("Data loaded with auto-expansion: {TotalRows} total rows ({DataRows} data, {EmptyRows} empty), {ValidRows} valid, {InvalidRows} invalid",
                    newRowViewModels.Count, totalRows, emptyRows, validRows, invalidRows);

                await Task.Delay(2000);
                IsValidating = false;
                UpdateValidationStatus("Pripravené");
                IsLoadingData = false;

            }
            catch (Exception ex)
            {
                IsLoadingData = false;
                IsValidating = false;
                UpdateValidationStatus("Chyba pri načítavaní");
                _logger.LogError(ex, "Error loading data from dictionary list");
                HandleGlobalError(ex, "LoadDataAsync");
                throw;
            }
        }

        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                if (!IsInitialized)
                    throw new InvalidOperationException("Component must be initialized first!");

                var dictList = ConvertDataTableToDictionaries(dataTable);
                await LoadDataAsync(dictList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from DataTable");
                HandleGlobalError(ex, "LoadDataAsync");
                throw;
            }
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Starting validation of all rows");
                IsValidating = true;
                UpdateValidationProgress(0);
                UpdateValidationStatus("Validujú sa riadky...");

                var progress = new Progress<double>(p => UpdateValidationProgress(p));
                var dataRows = Rows.Where(r => !r.IsEmpty).Select(r => ConvertToDataGridRow(r)).ToList();
                var results = await _validationService.ValidateAllRowsAsync(dataRows, progress);

                var allValid = results.All(r => r.IsValid);
                UpdateValidationStatus(allValid ? "Všetky riadky sú validné" : "Nájdené validačné chyby");

                _logger.LogInformation("Validation completed: all valid = {AllValid}", allValid);

                await Task.Delay(2000);
                UpdateValidationStatus("Pripravené");
                IsValidating = false;

                return allValid;
            }
            catch (Exception ex)
            {
                IsValidating = false;
                UpdateValidationStatus("Chyba pri validácii");
                _logger.LogError(ex, "Error validating all rows");
                HandleGlobalError(ex, "ValidateAllRowsAsync");
                return false;
            }
        }

        public async Task<DataTable> ExportDataAsync(bool includeValidAlerts = false)
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Exporting data to DataTable, includeValidAlerts: {IncludeValidAlerts}", includeValidAlerts);
                var dataRows = Rows.Select(r => ConvertToDataGridRow(r)).ToList();
                var result = await _exportService.ExportToDataTableAsync(dataRows, Columns.ToList(), includeValidAlerts);
                _logger.LogInformation("Exported {RowCount} rows to DataTable", result.Rows.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                HandleGlobalError(ex, "ExportDataAsync");
                return new DataTable();
            }
        }

        public async Task ClearAllDataAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (!IsInitialized) return;

                _logger.LogDebug("Clearing all data");

                await Task.Run(() =>
                {
                    foreach (var rowViewModel in Rows)
                    {
                        foreach (var cellViewModel in rowViewModel.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                        {
                            cellViewModel.Value = null;
                            cellViewModel.ClearValidationErrors();
                        }
                    }
                });

                _logger.LogInformation("All data cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
                HandleGlobalError(ex, "ClearAllDataAsync");
            }
        }

        public async Task RemoveEmptyRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Removing empty rows");

                var result = await Task.Run(() =>
                {
                    var dataRowViewModels = Rows.Where(r => !r.IsEmpty).ToList();

                    var minEmptyRows = Math.Min(10, _initialRowCount / 5);
                    var emptyRowsNeeded = Math.Max(minEmptyRows, _initialRowCount - dataRowViewModels.Count);

                    var newEmptyRowViewModels = new List<RowViewModel>();
                    for (int i = 0; i < emptyRowsNeeded; i++)
                    {
                        newEmptyRowViewModels.Add(CreateEmptyRowViewModel(dataRowViewModels.Count + i));
                    }

                    return new { DataRows = dataRowViewModels, EmptyRows = newEmptyRowViewModels };
                });

                Rows.Clear();
                Rows.AddRange(result.DataRows);
                Rows.AddRange(result.EmptyRows);

                _logger.LogInformation("Empty rows removed, {DataRowCount} data rows kept, {EmptyRowCount} empty rows added",
                    result.DataRows.Count, result.EmptyRows.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                HandleGlobalError(ex, "RemoveEmptyRowsAsync");
            }
        }

        public void Reset()
        {
            if (_disposed) return;

            try
            {
                _logger.LogInformation("Resetting ViewModel");

                ClearCollections();

                _validationService.ClearValidationRules();
                IsInitialized = false;

                IsValidating = false;
                UpdateValidationProgress(0);
                UpdateValidationStatus("Pripravené");

                _initialRowCount = 100;
                IsKeyboardShortcutsVisible = false;

                _logger.LogInformation("ViewModel reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ViewModel reset");
                HandleGlobalError(ex, "Reset");
            }
        }

        #endregion

        #region Copy/Paste Operations

        public async Task CopySelectedCellsAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("🔄 Kopírujem vybrané bunky...");

                var selectedCells = GetSelectedCells();
                if (selectedCells.Count == 0)
                {
                    _logger.LogDebug("⚠️ Žiadne bunky nie sú vybrané");
                    return;
                }

                await _clipboardService.CopySelectedCellsAsync(selectedCells);

                _logger.LogInformation("✅ Skopírovaných {CellCount} buniek do schránky", selectedCells.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri kopírovaní buniek");
                HandleGlobalError(ex, "CopySelectedCellsAsync");
                throw;
            }
        }

        public async Task PasteFromClipboardAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (!IsInitialized)
                {
                    _logger.LogWarning("⚠️ Komponent nie je inicializovaný");
                    return;
                }

                _logger.LogDebug("🔄 Vkladám dáta zo schránky...");

                var currentCell = _navigationService.CurrentCell;
                if (currentCell == null)
                {
                    _logger.LogDebug("⚠️ Žiadna bunka nie je vybraná pre paste operáciu");
                    return;
                }

                var startRowIndex = currentCell.RowIndex;
                var startColumnIndex = currentCell.ColumnIndex;

                var dataRows = Rows.Select(r => ConvertToDataGridRow(r)).ToList();
                var success = await _clipboardService.PasteToPositionAsync(
                    startRowIndex,
                    startColumnIndex,
                    dataRows,
                    Columns.ToList()
                );

                if (success)
                {
                    if (ThrottlingConfig.IsEnabled && ThrottlingConfig.PasteDelayMs > 0)
                    {
                        await Task.Delay(ThrottlingConfig.PasteDelayMs);
                    }

                    _logger.LogInformation("✅ Úspešne vložené dáta zo schránky na pozíciu [{Row},{Col}]",
                        startRowIndex, startColumnIndex);
                }
                else
                {
                    _logger.LogDebug("⚠️ Paste operácia nebola úspešná (možno prázdna schránka)");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri vkladaní zo schránky");
                HandleGlobalError(ex, "PasteFromClipboardAsync");
                throw;
            }
        }

        private List<DataGridCell> GetSelectedCells()
        {
            var selectedCells = new List<DataGridCell>();

            try
            {
                foreach (var rowViewModel in Rows)
                {
                    foreach (var cellViewModel in rowViewModel.Cells)
                    {
                        if (cellViewModel.IsSelected || cellViewModel.HasFocus)
                        {
                            var dataGridRow = ConvertToDataGridRow(rowViewModel);
                            var dataGridCell = ConvertToDataGridCell(cellViewModel, dataGridRow);
                            selectedCells.Add(dataGridCell);
                        }
                    }
                }

                if (selectedCells.Count == 0)
                {
                    var currentCell = _navigationService.CurrentCell;
                    if (currentCell != null)
                    {
                        selectedCells.Add(currentCell);
                    }
                }

                _logger.LogTrace("Nájdených {Count} vybraných buniek", selectedCells.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chyba pri získavaní vybraných buniek");
                HandleGlobalError(ex, "GetSelectedCells");
            }

            return selectedCells;
        }

        #endregion

        #region Helper Methods

        private async Task CreateInitialRowViewModelsAsync()
        {
            var rowCount = _initialRowCount;

            var rowViewModels = await Task.Run(() =>
            {
                var rowList = new List<RowViewModel>();

                for (int i = 0; i < rowCount; i++)
                {
                    var rowViewModel = CreateEmptyRowViewModel(i);
                    rowList.Add(rowViewModel);
                }

                return rowList;
            });

            Rows.AddRange(rowViewModels);

            _logger.LogDebug("Created {RowCount} initial empty row ViewModels safely", rowCount);
        }

        private RowViewModel CreateEmptyRowViewModel(int rowIndex)
        {
            var rowViewModel = new RowViewModel(rowIndex);

            foreach (var column in Columns)
            {
                var cellViewModel = new CellViewModel(column.Name, column.DataType, rowIndex, Columns.IndexOf(column))
                {
                    IsReadOnly = column.IsReadOnly
                };

                rowViewModel.AddCell(cellViewModel);
            }

            return rowViewModel;
        }

        private RowViewModel CreateRowViewModelForLoading(int rowIndex)
        {
            var rowViewModel = new RowViewModel(rowIndex);

            foreach (var column in Columns)
            {
                var cellViewModel = new CellViewModel(column.Name, column.DataType, rowIndex, Columns.IndexOf(column))
                {
                    IsReadOnly = column.IsReadOnly
                };

                rowViewModel.AddCell(cellViewModel);
            }

            return rowViewModel;
        }

        private async Task ValidateRowViewModelAfterLoadingAsync(RowViewModel rowViewModel)
        {
            try
            {
                rowViewModel.UpdateRowStatus();

                if (!rowViewModel.IsEmpty)
                {
                    var dataGridRow = ConvertToDataGridRow(rowViewModel);

                    foreach (var cellViewModel in rowViewModel.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                    {
                        var dataGridCell = ConvertToDataGridCell(cellViewModel, dataGridRow);
                        await _validationService.ValidateCellAsync(dataGridCell, dataGridRow);
                        UpdateCellViewModelFromDataGridCell(cellViewModel, dataGridCell);
                    }

                    rowViewModel.UpdateValidationStatus();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating row after loading");
                HandleGlobalError(ex, "ValidateRowViewModelAfterLoadingAsync");
            }
        }

        private async Task AutoInitializeFromDataAsync(List<Dictionary<string, object?>>? data)
        {
            var columns = new List<ColumnDefinition>();

            if (data?.Count > 0)
            {
                foreach (var key in data[0].Keys)
                {
                    columns.Add(new ColumnDefinition(key, typeof(string))
                    {
                        Header = FormatColumnHeader(key),
                        MinWidth = 80,
                        Width = 120
                    });
                }
            }
            else
            {
                columns.Add(new ColumnDefinition("Stĺpec1", typeof(string)) { Header = "Stĺpec 1", Width = 150 });
                columns.Add(new ColumnDefinition("Stĺpec2", typeof(string)) { Header = "Stĺpec 2", Width = 150 });
            }

            var rules = new List<ValidationRule>();
            foreach (var col in columns)
            {
                if (col.Name.ToLower().Contains("email"))
                {
                    rules.Add(ValidationRule.Email(col.Name));
                }
            }

            await InitializeAsync(columns, rules);
        }

        private string FormatColumnHeader(string columnName)
        {
            var lowerName = columnName.ToLower();

            if (lowerName.Contains("id")) return $"🔢 {columnName}";
            if (lowerName.Contains("meno") || lowerName.Contains("name")) return $"👤 {columnName}";
            if (lowerName.Contains("email")) return $"📧 {columnName}";
            if (lowerName.Contains("vek") || lowerName.Contains("age")) return $"🎂 {columnName}";
            if (lowerName.Contains("plat") || lowerName.Contains("salary")) return $"💰 {columnName}";
            if (lowerName.Contains("datum") || lowerName.Contains("date")) return $"📅 {columnName}";

            return columnName;
        }

        private List<Dictionary<string, object?>> ConvertDataTableToDictionaries(DataTable dataTable)
        {
            var result = new List<Dictionary<string, object?>>();

            foreach (DataRow row in dataTable.Rows)
            {
                var dict = new Dictionary<string, object?>();
                foreach (DataColumn column in dataTable.Columns)
                {
                    dict[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                }
                result.Add(dict);
            }

            return result;
        }

        private DataGridRow ConvertToDataGridRow(RowViewModel rowViewModel)
        {
            var dataGridRow = new DataGridRow(rowViewModel.RowIndex);

            foreach (var cellViewModel in rowViewModel.Cells)
            {
                var dataGridCell = new DataGridCell(cellViewModel.ColumnName, cellViewModel.DataType,
                    cellViewModel.RowIndex, cellViewModel.ColumnIndex)
                {
                    Value = cellViewModel.Value,
                    OriginalValue = cellViewModel.OriginalValue,
                    IsReadOnly = cellViewModel.IsReadOnly,
                    IsEditing = cellViewModel.IsEditing,
                    HasFocus = cellViewModel.HasFocus,
                    IsSelected = cellViewModel.IsSelected
                };

                // Copy validation errors
                if (cellViewModel.HasValidationErrors)
                {
                    var errors = new List<string>();
                    foreach (var error in cellViewModel.GetErrors(nameof(CellViewModel.Value)))
                    {
                        if (error is string errorString)
                            errors.Add(errorString);
                    }
                    dataGridCell.SetValidationErrors(errors);
                }

                dataGridRow.AddCell(cellViewModel.ColumnName, dataGridCell);
            }

            return dataGridRow;
        }

        private DataGridCell ConvertToDataGridCell(CellViewModel cellViewModel, DataGridRow parentRow)
        {
            var dataGridCell = new DataGridCell(cellViewModel.ColumnName, cellViewModel.DataType,
                cellViewModel.RowIndex, cellViewModel.ColumnIndex)
            {
                Value = cellViewModel.Value,
                OriginalValue = cellViewModel.OriginalValue,
                IsReadOnly = cellViewModel.IsReadOnly,
                IsEditing = cellViewModel.IsEditing,
                HasFocus = cellViewModel.HasFocus,
                IsSelected = cellViewModel.IsSelected
            };

            // Copy validation errors
            if (cellViewModel.HasValidationErrors)
            {
                var errors = new List<string>();
                foreach (var error in cellViewModel.GetErrors(nameof(CellViewModel.Value)))
                {
                    if (error is string errorString)
                        errors.Add(errorString);
                }
                dataGridCell.SetValidationErrors(errors);
            }

            return dataGridCell;
        }

        private void UpdateCellViewModelFromDataGridCell(CellViewModel cellViewModel, DataGridCell dataGridCell)
        {
            // Update validation errors
            if (dataGridCell.HasValidationErrors)
            {
                cellViewModel.SetValidationErrors(nameof(CellViewModel.Value), dataGridCell.ValidationErrors);
            }
            else
            {
                cellViewModel.ClearValidationErrors();
            }

            // Update other properties if needed
            cellViewModel.HasFocus = dataGridCell.HasFocus;
            cellViewModel.IsSelected = dataGridCell.IsSelected;
        }

        private void UpdateValidationStatus(string status)
        {
            ValidationStatus = status;
            _logger.LogTrace("Validation status updated: {Status}", status);
        }

        private void UpdateValidationProgress(double progress)
        {
            ValidationProgress = Math.Max(0, Math.Min(100, progress));
        }

        private void DeleteRowViewModel(RowViewModel? rowViewModel)
        {
            if (_disposed || rowViewModel == null) return;

            try
            {
                if (Rows.Contains(rowViewModel))
                {
                    foreach (var cellViewModel in rowViewModel.Cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                    {
                        cellViewModel.Value = null;
                        cellViewModel.ClearValidationErrors();
                    }

                    _logger.LogDebug("Row deleted: {RowIndex}", rowViewModel.RowIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting row");
                HandleGlobalError(ex, "DeleteRowViewModel");
            }
        }

        private void ToggleKeyboardShortcuts()
        {
            if (_disposed) return;

            try
            {
                IsKeyboardShortcutsVisible = !IsKeyboardShortcutsVisible;
                _logger.LogDebug("Keyboard shortcuts visibility toggled to: {IsVisible}", IsKeyboardShortcutsVisible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling keyboard shortcuts visibility");
                HandleGlobalError(ex, "ToggleKeyboardShortcuts");
            }
        }

        private static bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        private void HandleGlobalError(Exception ex, string operation)
        {
            _logger.LogError(ex, "Global error in operation: {Operation}", operation);
            OnErrorOccurred(new ComponentErrorEventArgs(ex, operation));
        }

        #endregion

        #region Commands Initialization

        private void InitializeCommands()
        {
            ValidateAllCommand = new AsyncRelayCommand(ValidateAllRowsAsync, null, HandleCommandError);
            ClearAllDataCommand = new AsyncRelayCommand(ClearAllDataAsync, null, HandleCommandError);
            RemoveEmptyRowsCommand = new AsyncRelayCommand(RemoveEmptyRowsAsync, null, HandleCommandError);
            CopyCommand = new AsyncRelayCommand(CopySelectedCellsAsync, null, HandleCommandError);
            PasteCommand = new AsyncRelayCommand(PasteFromClipboardAsync, null, HandleCommandError);
            DeleteRowCommand = new RelayCommand<RowViewModel>(DeleteRowViewModel);
            ExportToDataTableCommand = new AsyncRelayCommand(async () => await ExportDataAsync(), null, HandleCommandError);
            ToggleKeyboardShortcutsCommand = new RelayCommand(ToggleKeyboardShortcuts);
        }

        private void HandleCommandError(Exception ex)
        {
            _logger.LogError(ex, "Command execution error");
            HandleGlobalError(ex, "Command");
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            try
            {
                _dataService.DataChanged += OnDataChanged;
                _dataService.ErrorOccurred += OnDataServiceErrorOccurred;
                _validationService.ValidationCompleted += OnValidationCompleted;
                _validationService.ValidationErrorOccurred += OnValidationServiceErrorOccurred;
                _navigationService.ErrorOccurred += OnNavigationServiceErrorOccurred;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to service events");
                HandleGlobalError(ex, "SubscribeToEvents");
            }
        }

        private void OnDataChanged(object? sender, DataChangeEventArgs e)
        {
            if (_disposed) return;
            _logger.LogTrace("Data changed: {ChangeType}", e.ChangeType);
        }

        private void OnValidationCompleted(object? sender, ValidationCompletedEventArgs e)
        {
            if (_disposed) return;
            _logger.LogTrace("Validation completed for row. Is valid: {IsValid}", e.IsValid);
        }

        private void OnDataServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            _logger.LogError(e.Exception, "DataService error: {Operation}", e.Operation);
            HandleGlobalError(e.Exception, "DataService");
        }

        private void OnValidationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            _logger.LogError(e.Exception, "ValidationService error: {Operation}", e.Operation);
            HandleGlobalError(e.Exception, "ValidationService");
        }

        private void OnNavigationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            _logger.LogError(e.Exception, "NavigationService error: {Operation}", e.Operation);
            HandleGlobalError(e.Exception, "NavigationService");
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger?.LogDebug("Disposing AdvancedDataGridViewModel...");

                // Unsubscribe from all events
                UnsubscribeFromEvents();

                // Clear collections
                ClearCollections();

                // Clear commands
                ClearCommands();

                _isInitialized = false;

                _logger?.LogInformation("AdvancedDataGridViewModel disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during AdvancedDataGridViewModel disposal");
            }

            _disposed = true;
        }

        private void UnsubscribeFromEvents()
        {
            try
            {
                if (_dataService != null)
                {
                    _dataService.DataChanged -= OnDataChanged;
                    _dataService.ErrorOccurred -= OnDataServiceErrorOccurred;
                }

                if (_validationService != null)
                {
                    _validationService.ValidationCompleted -= OnValidationCompleted;
                    _validationService.ValidationErrorOccurred -= OnValidationServiceErrorOccurred;
                }

                if (_navigationService != null)
                {
                    _navigationService.ErrorOccurred -= OnNavigationServiceErrorOccurred;
                }

                _logger?.LogDebug("All service events unsubscribed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error unsubscribing from service events");
            }
        }

        private void ClearCollections()
        {
            try
            {
                // Dispose all row view models
                foreach (var rowViewModel in Rows)
                {
                    rowViewModel.Dispose();
                }

                Rows?.Clear();
                Columns?.Clear();
                _visibleCells?.Clear();

                _logger?.LogDebug("Collections cleared successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing collections");
            }
        }

        private void ClearCommands()
        {
            try
            {
                ValidateAllCommand = null!;
                ClearAllDataCommand = null!;
                RemoveEmptyRowsCommand = null!;
                CopyCommand = null!;
                PasteCommand = null!;
                DeleteRowCommand = null!;
                ExportToDataTableCommand = null!;
                ToggleKeyboardShortcutsCommand = null!;

                _logger?.LogDebug("Commands cleared successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing commands");
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdvancedDataGridViewModel));
        }

        #endregion

        #region Events & Property Changed

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (_disposed) return false;

            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (_disposed) return;

            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error firing PropertyChanged for {PropertyName}", propertyName);
            }
        }

        #endregion
    }
}