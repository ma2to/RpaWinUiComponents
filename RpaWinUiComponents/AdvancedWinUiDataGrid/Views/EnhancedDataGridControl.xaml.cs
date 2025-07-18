// ZLEPŠENIE 2,4,5: Enhanced Control s MVVM binding namiesto Dictionary<string,TextBox>
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/Views/EnhancedDataGridControl.xaml.cs

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
using Microsoft.UI.Xaml.Data;
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
    /// <summary>
    /// ENHANCED DataGrid Control s implementovanými zlepšeniami:
    /// ✅ ZLEPŠENIE 2: MVVM binding namiesto Dictionary<string,TextBox>
    /// ✅ ZLEPŠENIE 4: UI virtualizácia s ItemsRepeater
    /// ✅ ZLEPŠENIE 5: Loading states a progress indicators
    /// ✅ ZLEPŠENIE 1: Proper memory management
    /// ✅ ZLEPŠENIE 6: Enhanced error handling
    /// </summary>
    public sealed partial class EnhancedDataGridControl : UserControl, IDisposable, INotifyPropertyChanged
    {
        private AdvancedDataGridViewModel? _viewModel;
        private readonly ILogger<EnhancedDataGridControl> _logger;
        private bool _disposed = false;
        private bool _isInitialized = false;

        // ZLEPŠENIE 5: Loading states
        private bool _isLoading = false;
        private double _loadingProgress = 0;
        private string _loadingMessage = "Pripravuje sa...";

        // ZLEPŠENIE 1: Memory management
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Timer _memoryMonitorTimer;

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

            // ZLEPŠENIE 1: Memory monitoring
            _memoryMonitorTimer = new Timer(MonitorMemoryUsage, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;
            this.KeyDown += OnKeyDown;

            _logger.LogDebug("EnhancedDataGridControl vytvorený s MVVM pattern");
        }

        #region Properties (ZLEPŠENIE 5: Loading States)

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
        /// ZLEPŠENIE 2: Internal ViewModel access pre MVVM binding
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

        /// <summary>
        /// ✅ ENHANCED INICIALIZÁCIA s MVVM pattern
        /// </summary>
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

                // ZLEPŠENIE 2: Vytvorenie ViewModel ak neexistuje
                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    ViewModel = _viewModel;
                }

                LoadingProgress = 50;
                LoadingMessage = "Inicializujem služby...";

                // ZLEPŠENIE 2: Inicializácia ViewModel s MVVM pattern
                await _viewModel.InitializeAsync(columns, validationRules, throttling, initialRowCount);

                LoadingProgress = 80;
                LoadingMessage = "Finalizujem UI...";

                // ZLEPŠENIE 4: UI sa vytvorí automaticky cez data binding
                await Task.Delay(200); // Dať čas na UI update

                LoadingProgress = 100;
                LoadingMessage = "Dokončené";

                _isInitialized = true;

                // ZLEPŠENIE 5: Hide loading panel
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

        /// <summary>
        /// ✅ ENHANCED NAČÍTANIE DÁT s MVVM pattern
        /// </summary>
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

                // ZLEPŠENIE 1: Memory management
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                LoadingProgress = 20;

                // ZLEPŠENIE 2: ViewModel handles all data loading
                if (_viewModel != null)
                {
                    // Subscribe to progress updates
                    var progressHandler = new Progress<double>(progress =>
                    {
                        LoadingProgress = 20 + (progress * 0.7); // 20-90%
                    });

                    await _viewModel.LoadDataAsync(data);
                }

                LoadingProgress = 100;
                LoadingMessage = $"Načítané: {data?.Count ?? 0} riadkov";

                _logger.LogInformation("✅ Enhanced data loading dokončené úspešne");

                // Hide loading after delay
                await Task.Delay(1000);
                IsLoading = false;
            }
            catch (Exception ex)
            {
                IsLoading = false;
                LoadingMessage = $"Chyba: {ex.Message}";
                _logger.LogError(ex, "❌ Chyba pri enhanced načítaní dát");
                HandleError(ex, "LoadDataAsync");
                throw;
            }
        }

        public async Task LoadDataAsync(DataTable dataTable)
        {
            var dictList = ConvertDataTableToDictionaries(dataTable);
            await LoadDataAsync(dictList);
        }

        public async Task<DataTable> ExportToDataTableAsync()
        {
            if (_viewModel != null)
            {
                return await _viewModel.ExportDataAsync();
            }
            return new DataTable();
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            if (_viewModel != null)
            {
                IsLoading = true;
                LoadingMessage = "Validujem všetky riadky...";

                try
                {
                    var result = await _viewModel.ValidateAllRowsAsync();
                    LoadingMessage = result ? "Všetky riadky sú validné" : "Nájdené chyby";
                    await Task.Delay(1000);
                    return result;
                }
                finally
                {
                    IsLoading = false;
                }
            }
            return false;
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
                IsLoading = true;
                LoadingMessage = "Vymazávam všetky dáta...";

                if (_viewModel != null)
                {
                    await _viewModel.ClearAllDataAsync();
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
            if (_viewModel != null)
            {
                IsLoading = true;
                LoadingMessage = "Odstraňujem prázdne riadky...";

                try
                {
                    await _viewModel.RemoveEmptyRowsAsync();
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

                _viewModel?.Reset();

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

        #region ZLEPŠENIE 1: Enhanced Memory Management

        /// <summary>
        /// ZLEPŠENIE 1: Memory monitoring s optimalizáciou
        /// </summary>
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
                    // Update memory info in UI if needed
                    _logger.LogTrace("Enhanced memory usage: {MemoryMB} MB", memoryMB);
                });

                // ZLEPŠENIE 1: Force cleanup if memory usage is high
                if (memoryMB > 500) // 500MB threshold
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        _logger.LogInformation("High memory usage detected ({MemoryMB} MB), triggering cleanup", memoryMB);
                        TriggerMemoryCleanup();
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error monitoring enhanced memory usage");
            }
        }

        /// <summary>
        /// ZLEPŠENIE 1: Trigger memory cleanup
        /// </summary>
        private void TriggerMemoryCleanup()
        {
            try
            {
                // Cleanup ViewModel memory
                _viewModel?.Reset();

                // Force aggressive garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _logger.LogInformation("Enhanced memory cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during enhanced memory cleanup");
            }
        }

        #endregion

        #region ZLEPŠENIE 6: Enhanced Error Handling

        /// <summary>
        /// ZLEPŠENIE 6: Global error handling s recovery
        /// </summary>
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

        /// <summary>
        /// ZLEPŠENIE 6: Emergency reset pre kritické chyby
        /// </summary>
        private void EmergencyReset()
        {
            try
            {
                _logger.LogWarning("Executing emergency reset");

                // Cancel all operations
                _cancellationTokenSource?.Cancel();

                // Reset ViewModel
                if (_viewModel != null)
                {
                    UnsubscribeFromViewModel(_viewModel);
                    _viewModel.Dispose();
                    _viewModel = null;
                    ViewModel = null;
                }

                // Reset state
                _isInitialized = false;
                IsLoading = false;
                LoadingMessage = "Emergency reset completed";

                // Force memory cleanup
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _logger.LogInformation("Emergency reset completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Emergency reset failed");
            }
        }

        #endregion

        #region ZLEPŠENIE 4: Keyboard Navigation

        /// <summary>
        /// ZLEPŠENIE 4: Enhanced keyboard handling
        /// </summary>
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
                if (_viewModel != null)
                {
                    await _viewModel.CopySelectedCellsAsync();
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
                if (_viewModel != null)
                {
                    await _viewModel.PasteFromClipboardAsync();
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
                if (_viewModel != null)
                {
                    IsLoading = true;
                    LoadingMessage = "Refreshing data...";
                    await _viewModel.ValidateAllRowsAsync();
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
            if (columnName.ToLower().Contains("meno") || columnName.ToLower().Contains("name")) return $"👤 {columnName}";
            if (columnName.ToLower().Contains("email")) return $"📧 {columnName}";
            if (columnName.ToLower().Contains("vek") || columnName.ToLower().Contains("age")) return $"🎂 {columnName}";
            if (columnName.ToLower().Contains("plat") || columnName.ToLower().Contains("salary")) return $"💰 {columnName}";
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

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
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