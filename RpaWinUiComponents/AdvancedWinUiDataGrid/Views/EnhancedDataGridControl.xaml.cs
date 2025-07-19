// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/Views/EnhancedDataGridControl.xaml.cs
// KOMPLETNÝ OPRAVENÝ - bez tooltips, s ItemsRepeater, opravené chyby

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Configuration;

// Používame interné typy
using InternalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition;
using InternalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.ValidationRule;
using InternalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Views
{
    public sealed partial class EnhancedDataGridControl : UserControl, IDisposable, INotifyPropertyChanged
    {
        #region Fields & Dependencies

        private AdvancedDataGridViewModel? _viewModel;
        private readonly ILogger<EnhancedDataGridControl> _logger;
        private bool _disposed = false;
        private bool _isInitialized = false;

        // OPRAVA: Loading states ako fields namiesto properties
        private bool _isLoading = false;
        private double _loadingProgress = 0;
        private string _loadingMessage = "Pripravuje sa...";

        // Memory management
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Timer _memoryMonitorTimer;

        // Cell tracking pre memory management
        private readonly Dictionary<string, WeakReference> _cellReferences = new();
        private readonly object _cellTrackingLock = new();

        #endregion

        #region Constructor

        public EnhancedDataGridControl()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 Inicializujem EnhancedDataGridControl...");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("✅ InitializeComponent() úspešné");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ XAML Error: {ex.Message}");
                CreateFallbackUI();
            }

            // Initialize logger
            var loggerProvider = GetLoggerProvider();
            _logger = loggerProvider.CreateLogger<EnhancedDataGridControl>();

            // Memory monitoring
            _memoryMonitorTimer = new Timer(MonitorMemoryUsage, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;
            this.KeyDown += OnKeyDown;

            _logger.LogDebug("EnhancedDataGridControl vytvorený s MVVM pattern");
        }

        #endregion

        #region Properties

        // OPRAVA: Jednoduché properties bez binding chýb
        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public double LoadingProgress
        {
            get => _loadingProgress;
            private set => SetProperty(ref _loadingProgress, value);
        }

        public string LoadingMessage
        {
            get => _loadingMessage;
            private set => SetProperty(ref _loadingMessage, value);
        }

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Internal ViewModel access pre MVVM binding
        /// </summary>
        internal AdvancedDataGridViewModel? ViewModel
        {
            get => _viewModel;
            private set
            {
                if (_viewModel != null)
                {
                    UnsubscribeFromViewModel(_viewModel);
                }

                _viewModel = value;

                if (_viewModel != null)
                {
                    SubscribeToViewModel(_viewModel);
                }

                this.DataContext = _viewModel;
                OnPropertyChanged(nameof(ViewModel));
            }
        }

        #endregion

        #region PUBLIC API METHODS

        public async Task InitializeAsync(
            List<InternalColumnDefinition> columns,
            List<InternalValidationRule>? validationRules = null,
            InternalThrottlingConfig? throttling = null,
            int initialRowCount = 15)
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Inicializujem DataGrid...";
                LoadingProgress = 10;

                _logger.LogInformation("🚀 Začínam enhanced inicializáciu s {ColumnCount} stĺpcami", columns?.Count ?? 0);

                // Cancel previous operations
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                LoadingProgress = 30;
                LoadingMessage = "Vytváram ViewModel...";

                // Vytvorenie ViewModel ak neexistuje
                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    ViewModel = _viewModel;
                }

                LoadingProgress = 50;
                LoadingMessage = "Inicializujem služby...";

                // Inicializácia ViewModel
                await _viewModel.InitializeAsync(columns, validationRules, throttling, initialRowCount);

                LoadingProgress = 80;
                LoadingMessage = "Finalizujem UI...";

                // UI sa vytvorí automaticky cez data binding
                UpdateUI();
                await Task.Delay(200); // Dať čas na UI update

                LoadingProgress = 100;
                LoadingMessage = "Dokončené";

                _isInitialized = true;

                // Hide loading panel
                await Task.Delay(500);
                IsLoading = false;

                _logger.LogInformation("✅ Enhanced inicializácia dokončená úspešne s MVVM pattern");
            }
            catch (Exception ex)
            {
                IsLoading = false;
                LoadingMessage = $"Chyba: {ex.Message}";
                _logger.LogError(ex, "❌ Chyba pri enhanced inicializácii");
                HandleError(ex, "InitializeAsync");
                throw;
            }
        }

        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            try
            {
                if (!_isInitialized)
                {
                    _logger.LogWarning("Komponent nie je inicializovaný, spúšťam auto-inicializáciu");
                    await AutoInitializeFromData(data);
                }

                IsLoading = true;
                LoadingMessage = $"Načítavam {data?.Count ?? 0} riadkov...";
                LoadingProgress = 0;

                _logger.LogInformation("📊 Enhanced načítavam {RowCount} riadkov dát", data?.Count ?? 0);

                // Memory management
                await TriggerMemoryCleanup();

                if (ViewModel != null)
                {
                    await ViewModel.LoadDataAsync(data ?? new List<Dictionary<string, object?>>());
                }

                LoadingMessage = "Validácia dokončená";
                LoadingProgress = 100;

                UpdateRowCountDisplay();

                _logger.LogInformation("✅ Enhanced dáta načítané úspešne");

                await Task.Delay(1000);
                LoadingMessage = "Pripravené";
                IsLoading = false;

            }
            catch (Exception ex)
            {
                IsLoading = false;
                LoadingMessage = $"Chyba pri načítavaní: {ex.Message}";
                _logger.LogError(ex, "Error loading data from dictionary list");
                HandleError(ex, "LoadDataAsync");
                throw;
            }
        }

        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("Component must be initialized first!");

                var dictList = ConvertDataTableToDictionaries(dataTable);
                await LoadDataAsync(dictList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from DataTable");
                HandleError(ex, "LoadDataAsync");
                throw;
            }
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            try
            {
                _logger.LogDebug("Starting validation of all rows");
                IsLoading = true;
                LoadingProgress = 0;
                LoadingMessage = "Validujú sa riadky...";

                if (ViewModel != null)
                {
                    var result = await ViewModel.ValidateAllRowsAsync();
                    LoadingMessage = result ? "Všetky riadky sú validné" : "Nájdené validačné chyby";

                    _logger.LogInformation("Validation completed: all valid = {AllValid}", result);

                    await Task.Delay(2000);
                    LoadingMessage = "Pripravené";
                    IsLoading = false;

                    return result;
                }

                IsLoading = false;
                return false;
            }
            catch (Exception ex)
            {
                IsLoading = false;
                LoadingMessage = "Chyba pri validácii";
                _logger.LogError(ex, "Error validating all rows");
                HandleError(ex, "ValidateAllRowsAsync");
                return false;
            }
        }

        public async Task<DataTable> ExportToDataTableAsync()
        {
            if (ViewModel != null)
            {
                return await ViewModel.ExportDataAsync();
            }
            return new DataTable();
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Vymazávam všetky dáta...";

                if (ViewModel != null)
                {
                    await ViewModel.ClearAllDataAsync();
                }

                LoadingMessage = "Všetky dáta vymazané";
                await Task.Delay(500);
                IsLoading = false;

                _logger.LogInformation("✅ Enhanced clear all data dokončené");
            }
            catch (Exception ex)
            {
                IsLoading = false;
                _logger.LogError(ex, "❌ Chyba pri enhanced mazaní dát");
                HandleError(ex, "ClearAllDataAsync");
                throw;
            }
        }

        public async Task RemoveEmptyRowsAsync()
        {
            if (ViewModel != null)
            {
                IsLoading = true;
                LoadingMessage = "Odstraňujem prázdne riadky...";

                try
                {
                    await ViewModel.RemoveEmptyRowsAsync();
                    LoadingMessage = "Prázdne riadky odstránené";
                    await Task.Delay(500);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        public void Reset()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Resetujem komponent...";

                // Cancel operations and cleanup
                _cancellationTokenSource?.Cancel();

                _isInitialized = false;

                ViewModel?.Reset();

                LoadingMessage = "Reset dokončený";
                IsLoading = false;

                _logger.LogInformation("✅ Enhanced reset dokončený");
            }
            catch (Exception ex)
            {
                IsLoading = false;
                _logger.LogError(ex, "❌ Chyba pri enhanced reset");
                HandleError(ex, "Reset");
            }
        }

        #endregion

        #region UI Updates

        private void UpdateUI()
        {
            try
            {
                // Update header
                if (ViewModel?.Columns != null)
                {
                    HeaderItemsRepeater.ItemsSource = ViewModel.Columns;
                }

                // Update data rows
                if (ViewModel?.Rows != null)
                {
                    DataRowsItemsRepeater.ItemsSource = ViewModel.Rows;
                }

                // Update status
                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating UI");
                HandleError(ex, "UpdateUI");
            }
        }

        private void UpdateStatusDisplay()
        {
            try
            {
                if (ValidationStatusText != null)
                {
                    ValidationStatusText.Text = ViewModel?.ValidationStatus ?? "Ready";
                }

                UpdateRowCountDisplay();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status display");
            }
        }

        private void UpdateRowCountDisplay()
        {
            try
            {
                if (RowCountText != null && ViewModel?.Rows != null)
                {
                    var totalRows = ViewModel.Rows.Count;
                    var dataRows = ViewModel.Rows.Count(r => !r.IsEmpty);
                    RowCountText.Text = $"{dataRows}/{totalRows} riadkov";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating row count display");
            }
        }

        #endregion

        #region Memory Management

        private void MonitorMemoryUsage(object? state)
        {
            try
            {
                var memoryBefore = GC.GetTotalMemory(false);
                GC.Collect(0, GCCollectionMode.Optimized);
                var memoryAfter = GC.GetTotalMemory(false);

                var memoryMB = memoryAfter / 1024 / 1024;

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    _logger.LogTrace("Enhanced memory usage: {MemoryMB} MB", memoryMB);
                });

                // Force cleanup if memory usage is high
                if (memoryMB > 500) // 500MB threshold
                {
                    this.DispatcherQueue.TryEnqueue(async () =>
                    {
                        _logger.LogInformation("High memory usage detected ({MemoryMB} MB), triggering cleanup", memoryMB);
                        await TriggerMemoryCleanup();
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error monitoring enhanced memory usage");
            }
        }

        private async Task TriggerMemoryCleanup()
        {
            try
            {
                await Task.Run(() =>
                {
                    // Cleanup WeakReferences
                    lock (_cellTrackingLock)
                    {
                        var keysToRemove = new List<string>();
                        foreach (var kvp in _cellReferences)
                        {
                            if (!kvp.Value.IsAlive)
                            {
                                keysToRemove.Add(kvp.Key);
                            }
                        }

                        foreach (var key in keysToRemove)
                        {
                            _cellReferences.Remove(key);
                        }

                        _logger.LogDebug("Cleaned up {Count} dead cell references", keysToRemove.Count);
                    }

                    // Force aggressive garbage collection
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                });

                _logger.LogInformation("Enhanced memory cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during enhanced memory cleanup");
            }
        }

        #endregion

        #region Error Handling

        private void HandleError(Exception ex, string operation)
        {
            _logger.LogError(ex, "Enhanced error in operation: {Operation}", operation);

            // Try to recover gracefully
            try
            {
                IsLoading = false;
                LoadingMessage = $"Chyba: {ex.Message}";

                // Reset state on critical errors
                if (ex is OutOfMemoryException || ex is StackOverflowException)
                {
                    _logger.LogCritical("Critical error detected, triggering emergency reset");
                    EmergencyReset();
                }
            }
            catch (Exception recoveryEx)
            {
                _logger.LogCritical(recoveryEx, "Error during error recovery");
            }

            ErrorOccurred?.Invoke(this, new ComponentErrorEventArgs(ex, operation));
        }

        private void EmergencyReset()
        {
            try
            {
                _logger.LogWarning("Executing emergency reset");

                // Cancel all operations
                _cancellationTokenSource?.Cancel();

                // Reset ViewModel
                if (ViewModel != null)
                {
                    UnsubscribeFromViewModel(ViewModel);
                    ViewModel.Dispose();
                    ViewModel = null;
                }

                // Reset state
                _isInitialized = false;
                IsLoading = false;
                LoadingMessage = "Emergency reset completed";

                // Force memory cleanup
                _ = TriggerMemoryCleanup();

                _logger.LogInformation("Emergency reset completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Emergency reset failed");
            }
        }

        #endregion

        #region Keyboard Navigation

        private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            try
            {
                // Copy/Paste shortcuts
                if (Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control)
                    .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                {
                    switch (e.Key)
                    {
                        case VirtualKey.C:
                            _ = HandleCopyAsync();
                            e.Handled = true;
                            break;
                        case VirtualKey.V:
                            _ = HandlePasteAsync();
                            e.Handled = true;
                            break;
                        case VirtualKey.A:
                            HandleSelectAll();
                            e.Handled = true;
                            break;
                    }
                }

                // Other navigation keys
                switch (e.Key)
                {
                    case VirtualKey.F5:
                        _ = HandleRefreshAsync();
                        e.Handled = true;
                        break;
                    case VirtualKey.Delete:
                        HandleDeleteSelected();
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling enhanced key down");
                HandleError(ex, "OnKeyDown");
            }
        }

        private async Task HandleCopyAsync()
        {
            try
            {
                if (ViewModel != null)
                {
                    await ViewModel.CopySelectedCellsAsync();
                    LoadingMessage = "Data copied to clipboard";
                    await Task.Delay(1000);
                    LoadingMessage = "Pripravené";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying data");
                HandleError(ex, "HandleCopyAsync");
            }
        }

        private async Task HandlePasteAsync()
        {
            try
            {
                if (ViewModel != null)
                {
                    await ViewModel.PasteFromClipboardAsync();
                    LoadingMessage = "Data pasted from clipboard";
                    await Task.Delay(1000);
                    LoadingMessage = "Pripravené";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pasting data");
                HandleError(ex, "HandlePasteAsync");
            }
        }

        private void HandleSelectAll()
        {
            try
            {
                // Implementation for select all functionality
                _logger.LogDebug("Select all triggered");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error selecting all");
                HandleError(ex, "HandleSelectAll");
            }
        }

        private async Task HandleRefreshAsync()
        {
            try
            {
                if (ViewModel != null)
                {
                    IsLoading = true;
                    LoadingMessage = "Refreshing data...";
                    await ViewModel.ValidateAllRowsAsync();
                    LoadingMessage = "Data refreshed";
                    await Task.Delay(1000);
                    IsLoading = false;
                }
            }
            catch (Exception ex)
            {
                IsLoading = false;
                _logger.LogError(ex, "Error refreshing data");
                HandleError(ex, "HandleRefreshAsync");
            }
        }

        private void HandleDeleteSelected()
        {
            try
            {
                // Implementation for delete selected functionality
                _logger.LogDebug("Delete selected triggered");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting selected");
                HandleError(ex, "HandleDeleteSelected");
            }
        }

        #endregion

        #region XAML Event Handlers - BEZ TOOLTIPS

        private void OnCellDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            try
            {
                if (sender is TextBlock textBlock && textBlock.DataContext is CellViewModel cellViewModel)
                {
                    if (!cellViewModel.IsReadOnly)
                    {
                        // Začni editáciu bunky
                        cellViewModel.StartEditing();
                        _logger.LogDebug("Cell editing started via double tap: {CellKey}", cellViewModel.CellKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cell double tap");
                HandleError(ex, "OnCellDoubleTapped");
            }
        }

        private void OnCellTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                if (sender is TextBlock textBlock && textBlock.DataContext is CellViewModel cellViewModel)
                {
                    // Nastav focus na bunku
                    cellViewModel.HasFocus = true;
                    _logger.LogTrace("Cell focused via tap: {CellKey}", cellViewModel.CellKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cell tap");
                HandleError(ex, "OnCellTapped");
            }
        }

        private void OnCellGotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && textBox.DataContext is CellViewModel cellViewModel)
                {
                    cellViewModel.HasFocus = true;
                    _logger.LogTrace("Cell got focus: {CellKey}", cellViewModel.CellKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cell got focus");
                HandleError(ex, "OnCellGotFocus");
            }
        }

        private void OnCellLostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && textBox.DataContext is CellViewModel cellViewModel)
                {
                    cellViewModel.HasFocus = false;

                    // Ak bola bunka v edit móde, ukončíme editáciu
                    if (cellViewModel.IsEditing)
                    {
                        cellViewModel.CommitChanges();
                        _logger.LogDebug("Cell editing committed on focus lost: {CellKey}", cellViewModel.CellKey);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cell lost focus");
                HandleError(ex, "OnCellLostFocus");
            }
        }

        private void OnCellKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && textBox.DataContext is CellViewModel cellViewModel)
                {
                    switch (e.Key)
                    {
                        case VirtualKey.Enter:
                            if (!Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift)
                                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
                            {
                                // Enter bez Shift - ukončí editáciu a prejde na ďalší riadok
                                cellViewModel.CommitChanges();
                                // TODO: Implementovať navigáciu na ďalší riadok
                                e.Handled = true;
                            }
                            break;
                        case VirtualKey.Escape:
                            // Escape - zruší editáciu
                            cellViewModel.CancelEditing();
                            e.Handled = true;
                            break;
                        case VirtualKey.Tab:
                            // Tab - ukončí editáciu a prejde na ďalšiu bunku
                            cellViewModel.CommitChanges();
                            // TODO: Implementovať navigáciu na ďalšiu bunku
                            break;
                    }

                    _logger.LogTrace("Cell key down: {Key} in {CellKey}", e.Key, cellViewModel.CellKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cell key down");
                HandleError(ex, "OnCellKeyDown");
            }
        }

        private void OnCellTextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && textBox.DataContext is CellViewModel cellViewModel)
                {
                    // Aktualizuj hodnotu v ViewModel
                    if (cellViewModel.Value?.ToString() != textBox.Text)
                    {
                        cellViewModel.Value = textBox.Text;
                        _logger.LogTrace("Cell text changed: {CellKey} = '{Text}'", cellViewModel.CellKey, textBox.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cell text changed");
                HandleError(ex, "OnCellTextChanged");
            }
        }

        #endregion

        #region Event Handlers & Helper Methods

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadingMessage = "Control načítaný";
                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    ViewModel = _viewModel;
                }
                _logger.LogDebug("EnhancedDataGridControl loaded with MVVM pattern");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OnLoaded");
                LoadingMessage = $"Chyba pri načítaní: {ex.Message}";
                HandleError(ex, "OnControlLoaded");
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogDebug("EnhancedDataGridControl unloaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OnUnloaded");
            }
        }

        private async Task AutoInitializeFromData(List<Dictionary<string, object?>>? data)
        {
            var columns = new List<InternalColumnDefinition>();

            if (data?.Count > 0)
            {
                foreach (var key in data[0].Keys)
                {
                    columns.Add(new InternalColumnDefinition(key, typeof(string))
                    {
                        Header = FormatColumnHeader(key),
                        MinWidth = 80,
                        Width = 120
                    });
                }
            }
            else
            {
                columns.Add(new InternalColumnDefinition("Stĺpec1", typeof(string)) { Header = "Stĺpec 1", Width = 150 });
                columns.Add(new InternalColumnDefinition("Stĺpec2", typeof(string)) { Header = "Stĺpec 2", Width = 150 });
            }

            // Vytvor základné validačné pravidlá
            var rules = new List<InternalValidationRule>();
            foreach (var col in columns)
            {
                if (col.Name.ToLower().Contains("email"))
                {
                    rules.Add(InternalValidationRule.Email(col.Name));
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

        private AdvancedDataGridViewModel CreateViewModel()
        {
            try
            {
                return DependencyInjectionConfig.GetService<AdvancedDataGridViewModel>()
                       ?? DependencyInjectionConfig.CreateViewModelWithoutDI();
            }
            catch
            {
                return DependencyInjectionConfig.CreateViewModelWithoutDI();
            }
        }

        private IDataGridLoggerProvider GetLoggerProvider()
        {
            try
            {
                return DependencyInjectionConfig.GetService<IDataGridLoggerProvider>()
                       ?? NullDataGridLoggerProvider.Instance;
            }
            catch
            {
                return NullDataGridLoggerProvider.Instance;
            }
        }

        private void CreateFallbackUI()
        {
            this.Content = new TextBlock
            {
                Text = "⚠️ Enhanced RpaWinUiComponents DataGrid - Fallback Mode\nXAML parsing zlyhal.",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            };
        }

        private void SubscribeToViewModel(AdvancedDataGridViewModel viewModel)
        {
            try
            {
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
                viewModel.ErrorOccurred += OnViewModelErrorOccurred;
                _logger.LogDebug("Subscribed to ViewModel events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to ViewModel events");
            }
        }

        private void UnsubscribeFromViewModel(AdvancedDataGridViewModel viewModel)
        {
            try
            {
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                viewModel.ErrorOccurred -= OnViewModelErrorOccurred;
                _logger.LogDebug("Unsubscribed from ViewModel events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from ViewModel events");
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                // Handle specific ViewModel property changes if needed
                if (e.PropertyName == nameof(AdvancedDataGridViewModel.ValidationStatus))
                {
                    // Update UI based on validation status
                    if (sender is AdvancedDataGridViewModel vm)
                    {
                        LoadingMessage = vm.ValidationStatus;
                        UpdateStatusDisplay();
                    }
                }
                else if (e.PropertyName == nameof(AdvancedDataGridViewModel.Rows))
                {
                    // Update row count when rows change
                    UpdateRowCountDisplay();
                }
                else if (e.PropertyName == nameof(AdvancedDataGridViewModel.Columns))
                {
                    // Update header when columns change
                    if (sender is AdvancedDataGridViewModel vm && HeaderItemsRepeater != null)
                    {
                        HeaderItemsRepeater.ItemsSource = vm.Columns;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ViewModel property change");
            }
        }

        private void OnViewModelErrorOccurred(object? sender, ComponentErrorEventArgs e)
        {
            HandleError(e.Exception, $"ViewModel.{e.Operation}");
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger?.LogDebug("Disposing EnhancedDataGridControl...");

                // Cancel any ongoing operations
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();

                // Dispose timer
                _memoryMonitorTimer?.Dispose();

                // Clear cell references
                lock (_cellTrackingLock)
                {
                    _cellReferences.Clear();
                }

                // Dispose ViewModel
                if (_viewModel != null)
                {
                    UnsubscribeFromViewModel(_viewModel);
                    _viewModel.Dispose();
                    _viewModel = null;
                }

                _disposed = true;
                _logger?.LogInformation("EnhancedDataGridControl disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during enhanced disposal");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}