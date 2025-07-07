//ViewModels/AdvancedDataGridViewModel.cs - KOMPLETNÁ OPRAVA ZACYKLENIA VALIDÁCIE
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

// LOKÁLNE ALIASY pre zamedzenie CS0104 chýb
using ColumnDef = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
using ValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
using ThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels
{
    /// <summary>
    /// ViewModel pre AdvancedWinUiDataGrid komponent - KOMPLETNÁ OPRAVA ZACYKLENIA
    /// </summary>
    public class AdvancedDataGridViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IDataService _dataService;
        private readonly IValidationService _validationService;
        private readonly IClipboardService _clipboardService;
        private readonly IColumnService _columnService;
        private readonly IExportService _exportService;
        private readonly INavigationService _navigationService;
        private readonly ILogger<AdvancedDataGridViewModel> _logger;

        private ObservableRangeCollection<DataGridRow> _rows = new();
        private ObservableCollection<ColumnDef> _columns = new();
        private bool _isValidating = false;
        private double _validationProgress = 0;
        private string _validationStatus = "Pripravené";
        private bool _isInitialized = false;
        private ThrottlingConfig _throttlingConfig = ThrottlingConfig.Default;
        private bool _isKeyboardShortcutsVisible = false;

        private int _initialRowCount = 100;
        private bool _disposed = false;

        // KOMPLETNÁ OPRAVA ZACYKLENIA: Vylepšené sledovanie eventov a validácií
        private readonly object _eventSubscriptionLock = new object(); // Lock pre event subscription
        private readonly HashSet<string> _subscribedCellKeys = new(); // Sleduje ktoré bunky už majú event handler
        private readonly Dictionary<string, WeakReference> _cellReferences = new(); // Weak references na bunky

        // Validačné ochrany
        private readonly Dictionary<string, CancellationTokenSource> _pendingValidations = new();
        private readonly HashSet<string> _currentlyValidating = new(); // Ktoré bunky sa práve validujú
        private readonly object _validationStateLock = new object(); // Thread-safe prístup k validačnému stavu
        private SemaphoreSlim? _validationSemaphore;

        // PropertyChanged ochrany
        private readonly HashSet<string> _propertyChangeInProgress = new();
        private readonly object _propertyChangeLock = new object();

        // UI Throttling
        private readonly Dictionary<string, DateTime> _lastValidationTime = new();
        private readonly TimeSpan _minValidationInterval = TimeSpan.FromMilliseconds(100);

        // Loading state tracking
        private bool _isLoadingData = false;
        private readonly object _loadingStateLock = new object();

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

            _logger.LogDebug("AdvancedDataGridViewModel created");
        }

        #region Properties

        public ObservableRangeCollection<DataGridRow> Rows
        {
            get
            {
                ThrowIfDisposed();
                return _rows;
            }
            set => SetProperty(ref _rows, value);
        }

        public ObservableCollection<ColumnDef> Columns
        {
            get
            {
                ThrowIfDisposed();
                return _columns;
            }
            set => SetProperty(ref _columns, value);
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

        /// <summary>
        /// Inicializuje ViewModel s konfiguráciou stĺpcov a validáciami
        /// </summary>
        public async Task InitializeAsync(
            List<ColumnDef> columnDefinitions,
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

                // OPRAVA ZACYKLENIA: Nastavenie loading state
                lock (_loadingStateLock)
                {
                    _isLoadingData = true;
                }

                _initialRowCount = Math.Max(1, Math.Min(initialRowCount, 10000));
                ThrottlingConfig = throttling ?? ThrottlingConfig.Default;

                if (!ThrottlingConfig.IsValidConfig(out var configError))
                {
                    throw new ArgumentException($"Invalid throttling config: {configError}");
                }

                // Update semaphore with new max concurrent validations
                _validationSemaphore?.Dispose();
                _validationSemaphore = new SemaphoreSlim(ThrottlingConfig.MaxConcurrentValidations, ThrottlingConfig.MaxConcurrentValidations);

                _logger.LogInformation("Initializing AdvancedDataGrid with {ColumnCount} columns, {RuleCount} validation rules, {InitialRowCount} rows",
                    columnDefinitions?.Count ?? 0, validationRules?.Count ?? 0, _initialRowCount);

                // OPRAVA ZACYKLENIA: Clear všetky tracking dáta pred inicializáciou
                await ClearAllTrackingDataSafely();

                // Process and validate columns
                var processedColumns = _columnService.ProcessColumnDefinitions(columnDefinitions ?? new List<ColumnDef>());
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

                // Create initial rows WITH PROTECTION
                await CreateInitialRowsSafely();

                // Initialize navigation service
                _navigationService.Initialize(Rows.ToList(), reorderedColumns);

                // OPRAVA ZACYKLENIA: Loading state ukončený
                lock (_loadingStateLock)
                {
                    _isLoadingData = false;
                }

                IsInitialized = true;
                _logger.LogInformation("AdvancedDataGrid initialization completed: {ActualRowCount} rows created",
                    Rows.Count);

            }
            catch (Exception ex)
            {
                // OPRAVA: Reset loading state aj pri chybe
                lock (_loadingStateLock)
                {
                    _isLoadingData = false;
                }

                IsInitialized = false;
                _logger.LogError(ex, "Error during initialization");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
                throw;
            }
        }

        /// <summary>
        /// Načíta dáta z DataTable s automatickou validáciou
        /// </summary>
        public async Task LoadDataAsync(DataTable dataTable)
        {
            ThrowIfDisposed();

            try
            {
                if (!IsInitialized)
                    throw new InvalidOperationException("Component must be initialized first!");

                _logger.LogInformation("Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);

                // OPRAVA ZACYKLENIA: Nastavenie loading state
                lock (_loadingStateLock)
                {
                    _isLoadingData = true;
                }

                IsValidating = true;
                ValidationStatus = "Načítavajú sa dáta...";
                ValidationProgress = 0;

                // KĽÚČOVÁ OPRAVA: Vyčistiť všetko pred načítaním nových dát
                await ClearAllTrackingDataSafely();

                var newRows = new List<DataGridRow>();
                var rowIndex = 0;
                var totalRows = dataTable?.Rows.Count ?? 0;

                if (dataTable != null)
                {
                    foreach (DataRow dataRow in dataTable.Rows)
                    {
                        var gridRow = CreateRowForLoadingWithSafeValidation(rowIndex);

                        _logger.LogTrace("Loading row {RowIndex}/{TotalRows}", rowIndex + 1, totalRows);

                        foreach (var column in Columns.Where(c => !IsSpecialColumn(c.Name)))
                        {
                            if (dataTable.Columns.Contains(column.Name))
                            {
                                var value = dataRow[column.Name];
                                var cell = gridRow.GetCell(column.Name);
                                if (cell != null)
                                {
                                    // OPRAVA ZACYKLENIA: Set value bez spustenia validácie počas načítavania
                                    cell.SetValueWithoutValidation(value == DBNull.Value ? null : value);
                                }
                            }
                        }

                        // OPRAVA ZACYKLENIA: Validácia až po kompletnom nastavení riadku
                        await ValidateRowAfterLoadingSafely(gridRow);

                        newRows.Add(gridRow);
                        rowIndex++;
                        ValidationProgress = (double)rowIndex / totalRows * 90;
                    }
                }

                var minEmptyRows = Math.Min(10, _initialRowCount / 5);
                var finalRowCount = Math.Max(_initialRowCount, totalRows + minEmptyRows);

                while (newRows.Count < finalRowCount)
                {
                    newRows.Add(CreateEmptyRowWithSafeValidation(newRows.Count));
                }

                // OPRAVA ZACYKLENIA: Reset rows collection bezpečne
                Rows.Clear();
                Rows.AddRange(newRows);

                ValidationStatus = "Validácia dokončená";
                ValidationProgress = 100;

                var validRows = newRows.Count(r => !r.IsEmpty && !r.HasValidationErrors);
                var invalidRows = newRows.Count(r => !r.IsEmpty && r.HasValidationErrors);
                var emptyRows = newRows.Count - totalRows;

                _logger.LogInformation("Data loaded with auto-expansion: {TotalRows} total rows ({DataRows} data, {EmptyRows} empty), {ValidRows} valid, {InvalidRows} invalid",
                    newRows.Count, totalRows, emptyRows, validRows, invalidRows);

                await Task.Delay(2000);
                IsValidating = false;
                ValidationStatus = "Pripravené";

                // OPRAVA ZACYKLENIA: Loading state ukončený
                lock (_loadingStateLock)
                {
                    _isLoadingData = false;
                }
            }
            catch (Exception ex)
            {
                // OPRAVA: Reset loading state aj pri chybe
                lock (_loadingStateLock)
                {
                    _isLoadingData = false;
                }

                IsValidating = false;
                ValidationStatus = "Chyba pri načítavaní";
                _logger.LogError(ex, "Error loading data from DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// Načíta dáta zo zoznamu dictionary objektov
        /// </summary>
        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            ThrowIfDisposed();

            try
            {
                if (!IsInitialized)
                    throw new InvalidOperationException("Component must be initialized first!");

                var dataTable = ConvertToDataTable(data);
                await LoadDataAsync(dataTable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from dictionary list");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// Validuje všetky riadky a vráti true ak sú všetky validné
        /// </summary>
        public async Task<bool> ValidateAllRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Starting validation of all rows");
                IsValidating = true;
                ValidationProgress = 0;
                ValidationStatus = "Validujú sa riadky...";

                var progress = new Progress<double>(p => ValidationProgress = p);
                var dataRows = Rows.Where(r => !r.IsEmpty).ToList();
                var results = await _validationService.ValidateAllRowsAsync(dataRows, progress);

                var allValid = results.All(r => r.IsValid);
                ValidationStatus = allValid ? "Všetky riadky sú validné" : "Nájdené validačné chyby";

                _logger.LogInformation("Validation completed: all valid = {AllValid}", allValid);

                await Task.Delay(2000);
                ValidationStatus = "Pripravené";
                IsValidating = false;

                return allValid;
            }
            catch (Exception ex)
            {
                IsValidating = false;
                ValidationStatus = "Chyba pri validácii";
                _logger.LogError(ex, "Error validating all rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        // ... (zvyšok metód zostáva rovnaký ako v pôvodnom súbore, len sa pridajú nové pomocné metódy)

        #endregion

        #region KOMPLETNÁ OPRAVA ZACYKLENIA - Nové bezpečné metódy

        /// <summary>
        /// OPRAVA ZACYKLENIA: Bezpečné vytvorenie initial rows s ochranou proti duplicitným event handlers
        /// </summary>
        private async Task CreateInitialRowsSafely()
        {
            var rowCount = _initialRowCount;

            var rows = await Task.Run(() =>
            {
                var rowList = new List<DataGridRow>();

                for (int i = 0; i < rowCount; i++)
                {
                    var row = CreateEmptyRowWithSafeValidation(i);
                    rowList.Add(row);
                }

                return rowList;
            });

            // OPRAVA ZACYKLENIA: Vyčistiť tracking pred pridaním nových riadkov
            await ClearAllTrackingDataSafely();
            Rows.AddRange(rows);

            _logger.LogDebug("Created {RowCount} initial empty rows safely", rowCount);
        }

        /// <summary>
        /// OPRAVA ZACYKLENIA: Bezpečné vytvorenie riadku s pokročilou kontrolou duplicitných eventov
        /// </summary>
        private DataGridRow CreateEmptyRowWithSafeValidation(int rowIndex)
        {
            var row = new DataGridRow(rowIndex);

            foreach (var column in Columns)
            {
                var cell = new DataGridCell(column.Name, column.DataType, rowIndex, Columns.IndexOf(column))
                {
                    IsReadOnly = column.IsReadOnly
                };

                row.AddCell(column.Name, cell);

                // KĽÚČOVÁ OPRAVA: Subscribe to validation iba ak nie je loading a nie je špeciálny stĺpec
                if (!IsSpecialColumn(column.Name) && !IsLoadingData())
                {
                    SubscribeToCellValidationSafely(row, cell);
                }
            }

            return row;
        }

        /// <summary>
        /// OPRAVA ZACYKLENIA: Bezpečné prihlásenie na cell validation events s ochranou proti duplicitám
        /// </summary>
        private void SubscribeToCellValidationSafely(DataGridRow row, DataGridCell cell)
        {
            var cellKey = GenerateCellKey(row.RowIndex, cell.ColumnName);

            lock (_eventSubscriptionLock)
            {
                // Kontrola či už má event handler
                if (_subscribedCellKeys.Contains(cellKey))
                {
                    _logger.LogTrace("Cell already has event subscription, skipping: {CellKey}", cellKey);
                    return;
                }

                try
                {
                    // Pridaj do tracking
                    _subscribedCellKeys.Add(cellKey);
                    _cellReferences[cellKey] = new WeakReference(cell);

                    // Subscribe to PropertyChanged event
                    cell.PropertyChanged += async (s, e) =>
                    {
                        if (e.PropertyName == nameof(DataGridCell.Value) && !_disposed && !IsLoadingData())
                        {
                            await OnCellValueChangedSafely(row, cell);
                        }
                    };

                    _logger.LogTrace("Successfully subscribed to cell validation: {CellKey}", cellKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error subscribing to cell validation: {CellKey}", cellKey);
                    // Odstráň z tracking pri chybe
                    _subscribedCellKeys.Remove(cellKey);
                    _cellReferences.Remove(cellKey);
                }
            }
        }

        /// <summary>
        /// KOMPLETNÁ OPRAVA ZACYKLENIA: Vylepšené bezpečné spracovanie zmeny hodnoty bunky
        /// </summary>
        private async Task OnCellValueChangedSafely(DataGridRow row, DataGridCell cell)
        {
            if (_disposed || IsLoadingData()) return;

            var cellKey = GenerateCellKey(row.RowIndex, cell.ColumnName);

            try
            {
                // KĽÚČOVÁ OCHRANA: Kontrola či už prebieha validácia tejto bunky
                lock (_validationStateLock)
                {
                    if (_currentlyValidating.Contains(cellKey))
                    {
                        _logger.LogTrace("Validation already in progress for cell: {CellKey}", cellKey);
                        return;
                    }

                    // Throttling check - minimálny interval medzi validáciami
                    if (_lastValidationTime.TryGetValue(cellKey, out var lastTime))
                    {
                        var elapsed = DateTime.UtcNow - lastTime;
                        if (elapsed < _minValidationInterval)
                        {
                            _logger.LogTrace("Throttling validation for cell: {CellKey}, elapsed: {Elapsed}ms", cellKey, elapsed.TotalMilliseconds);
                            return;
                        }
                    }

                    _currentlyValidating.Add(cellKey);
                    _lastValidationTime[cellKey] = DateTime.UtcNow;
                }

                try
                {
                    // If throttling is disabled, validate immediately
                    if (!ThrottlingConfig.IsEnabled)
                    {
                        await ValidateCellImmediately(row, cell);
                        return;
                    }

                    // Cancel previous validation for this cell
                    lock (_validationStateLock)
                    {
                        if (_pendingValidations.TryGetValue(cellKey, out var existingCts))
                        {
                            existingCts.Cancel();
                            existingCts.Dispose();
                            _pendingValidations.Remove(cellKey);
                        }
                    }

                    // If row is empty, clear validation immediately
                    if (row.IsEmpty)
                    {
                        cell.ClearValidationErrors();
                        row.UpdateValidationStatus();
                        return;
                    }

                    // Create new cancellation token for this validation
                    var cts = new CancellationTokenSource();
                    lock (_validationStateLock)
                    {
                        _pendingValidations[cellKey] = cts;
                    }

                    try
                    {
                        // Apply throttling delay
                        await Task.Delay(ThrottlingConfig.TypingDelayMs, cts.Token);

                        // Check if still valid (not cancelled and not disposed)
                        if (cts.Token.IsCancellationRequested || _disposed || IsLoadingData())
                            return;

                        // Perform throttled validation
                        await ValidateCellThrottled(row, cell, cellKey, cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Validation was cancelled - this is normal
                        _logger.LogTrace("Validation cancelled for cell: {CellKey}", cellKey);
                    }
                    finally
                    {
                        // Clean up
                        lock (_validationStateLock)
                        {
                            _pendingValidations.Remove(cellKey);
                        }
                        cts.Dispose();
                    }
                }
                finally
                {
                    // KĽÚČOVÉ: Vždy odstrániť zo zoznamu validujúcich sa buniek
                    lock (_validationStateLock)
                    {
                        _currentlyValidating.Remove(cellKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in safe cell validation for {CellKey}", cellKey);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnCellValueChangedSafely"));
            }
        }

        private async Task ValidateCellImmediately(DataGridRow row, DataGridCell cell)
        {
            try
            {
                if (row.IsEmpty)
                {
                    cell.ClearValidationErrors();
                    row.UpdateValidationStatus();
                    return;
                }

                await _validationService.ValidateCellAsync(cell, row);
                row.UpdateValidationStatus();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in immediate cell validation");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateCellImmediately"));
            }
        }

        private async Task ValidateCellThrottled(DataGridRow row, DataGridCell cell, string cellKey, CancellationToken cancellationToken)
        {
            try
            {
                // Use semaphore to limit concurrent validations
                await _validationSemaphore!.WaitAsync(cancellationToken);

                try
                {
                    // Double-check if still valid
                    if (cancellationToken.IsCancellationRequested || _disposed || IsLoadingData())
                        return;

                    _logger.LogTrace("Executing throttled validation for cell: {CellKey}", cellKey);

                    // Perform actual validation
                    await _validationService.ValidateCellAsync(cell, row, cancellationToken);
                    row.UpdateValidationStatus();

                    _logger.LogTrace("Throttled validation completed for cell: {CellKey}", cellKey);
                }
                finally
                {
                    _validationSemaphore.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when validation is cancelled
                _logger.LogTrace("Throttled validation cancelled for cell: {CellKey}", cellKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in throttled validation for cell: {CellKey}", cellKey);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateCellThrottled"));
            }
        }

        /// <summary>
        /// OPRAVA ZACYKLENIA: Bezpečné vytvorenie riadku pre loading
        /// </summary>
        private DataGridRow CreateRowForLoadingWithSafeValidation(int rowIndex)
        {
            var row = new DataGridRow(rowIndex);

            foreach (var column in Columns)
            {
                var cell = new DataGridCell(column.Name, column.DataType, rowIndex, Columns.IndexOf(column))
                {
                    IsReadOnly = column.IsReadOnly
                };

                row.AddCell(column.Name, cell);
                // OPRAVA: Event handlers sa pridajú až po načítaní všetkých dát
            }

            return row;
        }

        /// <summary>
        /// OPRAVA ZACYKLENIA: Bezpečná validácia riadku po načítaní
        /// </summary>
        private async Task ValidateRowAfterLoadingSafely(DataGridRow row)
        {
            try
            {
                row.UpdateEmptyStatus();

                if (!row.IsEmpty)
                {
                    foreach (var cell in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                    {
                        await _validationService.ValidateCellAsync(cell, row);
                    }

                    row.UpdateValidationStatus();
                }

                // KĽÚČOVÁ OPRAVA: Subscribe to validation events AŽ PO NAČÍTANÍ dát
                foreach (var cell in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                {
                    SubscribeToCellValidationSafely(row, cell);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating row after loading");
            }
        }

        /// <summary>
        /// OPRAVA ZACYKLENIA: Bezpečné vyčistenie všetkých tracking dát
        /// </summary>
        private async Task ClearAllTrackingDataSafely()
        {
            await Task.Run(() =>
            {
                lock (_eventSubscriptionLock)
                {
                    _subscribedCellKeys.Clear();
                    _cellReferences.Clear();
                }

                lock (_validationStateLock)
                {
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
                    _currentlyValidating.Clear();
                    _lastValidationTime.Clear();
                }

                lock (_propertyChangeLock)
                {
                    _propertyChangeInProgress.Clear();
                }

                _logger.LogDebug("All tracking data cleared safely");
            });
        }

        /// <summary>
        /// OPRAVA ZACYKLENIA: Kontrola či prebieha loading
        /// </summary>
        private bool IsLoadingData()
        {
            lock (_loadingStateLock)
            {
                return _isLoadingData;
            }
        }

        /// <summary>
        /// Generuje unikátny kľúč pre bunku
        /// </summary>
        private static string GenerateCellKey(int rowIndex, string columnName)
        {
            return $"{rowIndex}_{columnName}";
        }

        private static bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        #endregion

        #region Zvyšok implementácie (Commands, Events, atď.)

        private void InitializeCommands()
        {
            ValidateAllCommand = new AsyncRelayCommand(ValidateAllRowsAsync);
            ClearAllDataCommand = new AsyncRelayCommand(ClearAllDataAsync);
            RemoveEmptyRowsCommand = new AsyncRelayCommand(RemoveEmptyRowsAsync);
            CopyCommand = new AsyncRelayCommand(CopySelectedCellsInternalAsync);
            PasteCommand = new AsyncRelayCommand(PasteFromClipboardInternalAsync);
            DeleteRowCommand = new RelayCommand<DataGridRow>(DeleteRowInternal);
            ExportToDataTableCommand = new AsyncRelayCommand(async () => await ExportDataAsync());
            ToggleKeyboardShortcutsCommand = new RelayCommand(ToggleKeyboardShortcuts);
        }

        private void SubscribeToEvents()
        {
            _dataService.DataChanged += OnDataChanged;
            _dataService.ErrorOccurred += OnDataServiceErrorOccurred;
            _validationService.ValidationCompleted += OnValidationCompleted;
            _validationService.ValidationErrorOccurred += OnValidationServiceErrorOccurred;
            _navigationService.ErrorOccurred += OnNavigationServiceErrorOccurred;
        }

        // ... (zvyšok metód ostáva rovnaký)

        public async Task<DataTable> ExportDataAsync(bool includeValidAlerts = false)
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogDebug("Exporting data to DataTable, includeValidAlerts: {IncludeValidAlerts}", includeValidAlerts);
                var result = await _exportService.ExportToDataTableAsync(Rows.ToList(), Columns.ToList(), includeValidAlerts);
                _logger.LogInformation("Exported {RowCount} rows to DataTable", result.Rows.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportDataAsync"));
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
                    foreach (var row in Rows)
                    {
                        foreach (var cell in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                        {
                            cell.Value = null;
                            cell.ClearValidationErrors();
                        }
                    }
                });

                _logger.LogInformation("All data cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
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
                    var dataRows = Rows.Where(r => !r.IsEmpty).ToList();

                    var minEmptyRows = Math.Min(10, _initialRowCount / 5);
                    var emptyRowsNeeded = Math.Max(minEmptyRows, _initialRowCount - dataRows.Count);

                    var newEmptyRows = new List<DataGridRow>();
                    for (int i = 0; i < emptyRowsNeeded; i++)
                    {
                        newEmptyRows.Add(CreateEmptyRowWithSafeValidation(dataRows.Count + i));
                    }

                    return new { DataRows = dataRows, EmptyRows = newEmptyRows };
                });

                // Clear tracking pred resetom riadkov
                await ClearAllTrackingDataSafely();

                Rows.Clear();
                Rows.AddRange(result.DataRows);
                Rows.AddRange(result.EmptyRows);

                _logger.LogInformation("Empty rows removed, {DataRowCount} data rows kept, {EmptyRowCount} empty rows added",
                    result.DataRows.Count, result.EmptyRows.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        private async Task CopySelectedCellsInternalAsync()
        {
            ThrowIfDisposed();

            try
            {
                var selectedCells = GetSelectedCells();
                await _clipboardService.CopySelectedCellsAsync(selectedCells);
                _logger.LogDebug("Copied selected cells to clipboard");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying selected cells");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "CopySelectedCellsInternalAsync"));
            }
        }

        private async Task PasteFromClipboardInternalAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (!IsInitialized) return;

                var currentCell = _navigationService.CurrentCell;
                if (currentCell == null) return;

                var startRowIndex = currentCell.RowIndex;
                var startColumnIndex = currentCell.ColumnIndex;

                var success = await _clipboardService.PasteToPositionAsync(startRowIndex, startColumnIndex, Rows.ToList(), Columns.ToList());

                if (success)
                {
                    if (ThrottlingConfig.IsEnabled && ThrottlingConfig.PasteDelayMs > 0)
                    {
                        await Task.Delay(ThrottlingConfig.PasteDelayMs);
                    }

                    _logger.LogDebug("Pasted data from clipboard at position [{Row},{Col}]", startRowIndex, startColumnIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pasting from clipboard");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "PasteFromClipboardInternalAsync"));
            }
        }

        private void DeleteRowInternal(DataGridRow? row)
        {
            if (_disposed || row == null) return;

            try
            {
                if (Rows.Contains(row))
                {
                    foreach (var cell in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                    {
                        cell.Value = null;
                        cell.ClearValidationErrors();
                    }

                    _logger.LogDebug("Row deleted: {RowIndex}", row.RowIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting row");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "DeleteRowInternal"));
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
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ToggleKeyboardShortcuts"));
            }
        }

        private List<DataGridCell> GetSelectedCells()
        {
            var selectedCells = new List<DataGridCell>();

            foreach (var row in Rows)
            {
                foreach (var cell in row.Cells.Values)
                {
                    if (cell.IsSelected)
                    {
                        selectedCells.Add(cell);
                    }
                }
            }

            return selectedCells;
        }

        private DataTable ConvertToDataTable(List<Dictionary<string, object?>> data)
        {
            var dataTable = new DataTable();

            if (data?.Count > 0)
            {
                foreach (var key in data[0].Keys)
                {
                    dataTable.Columns.Add(key, typeof(object));
                }

                foreach (var row in data)
                {
                    var dataRow = dataTable.NewRow();
                    foreach (var kvp in row)
                    {
                        dataRow[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
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
                ValidationProgress = 0;
                ValidationStatus = "Pripravené";

                _initialRowCount = 100;
                IsKeyboardShortcutsVisible = false;

                _logger.LogInformation("ViewModel reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during ViewModel reset");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Reset"));
            }
        }

        #endregion

        #region Event Handlers

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
            OnErrorOccurred(e);
        }

        private void OnValidationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            _logger.LogError(e.Exception, "ValidationService error: {Operation}", e.Operation);
            OnErrorOccurred(e);
        }

        private void OnNavigationServiceErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            if (_disposed) return;
            _logger.LogError(e.Exception, "NavigationService error: {Operation}", e.Operation);
            OnErrorOccurred(e);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    _logger?.LogDebug("Disposing AdvancedDataGridViewModel...");

                    // Unsubscribe from all events
                    UnsubscribeFromEvents();

                    // Clear collections and tracking data
                    ClearCollections();

                    // Dispose semaphore
                    _validationSemaphore?.Dispose();

                    // Clear commands
                    ClearCommands();

                    _isInitialized = false;

                    _logger?.LogInformation("AdvancedDataGridViewModel disposed successfully");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during AdvancedDataGridViewModel disposal");
                }
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
                // Clear tracking data safely
                _ = ClearAllTrackingDataSafely();

                // Clear rows and unsubscribe from cell events
                if (Rows?.Count > 0)
                {
                    foreach (var row in Rows)
                    {
                        foreach (var cell in row.Cells.Values)
                        {
                            // Note: PropertyChanged events will be GC'd when cells are disposed
                        }
                    }
                }

                Rows?.Clear();
                Columns?.Clear();

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

            // OPRAVA ZACYKLENIA: Prevencia zacyklenia PropertyChanged eventov
            lock (_propertyChangeLock)
            {
                if (_propertyChangeInProgress.Contains(propertyName))
                {
                    _logger.LogTrace("Property change already in progress: {PropertyName}", propertyName);
                    return false;
                }
                _propertyChangeInProgress.Add(propertyName);
            }

            try
            {
                if (EqualityComparer<T>.Default.Equals(backingStore, value))
                    return false;

                backingStore = value;
                OnPropertyChanged(propertyName);
                return true;
            }
            finally
            {
                lock (_propertyChangeLock)
                {
                    _propertyChangeInProgress.Remove(propertyName);
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            if (_disposed) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}