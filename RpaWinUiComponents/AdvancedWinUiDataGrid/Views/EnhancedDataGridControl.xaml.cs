// OPRAVENÝ EnhancedDataGridControl.xaml.cs - PUBLIC Event Args
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
using Microsoft.UI.Xaml.Media;
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
    /// ✅ OPRAVENÝ - Používa iba PUBLIC event args
    /// </summary>
    public sealed partial class EnhancedDataGridControl : UserControl, IDisposable, INotifyPropertyChanged
    {
        #region Fields & Dependencies

        private AdvancedDataGridViewModel? _viewModel;
        private readonly ILogger<EnhancedDataGridControl> _logger;
        private bool _disposed = false;
        private bool _isInitialized = false;
        private bool _isUsingFallback = false;

        // Loading states
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
                this.InitializeComponent();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ XAML Error: {ex.Message}");
                CreateSimpleFallbackUI();
                _isUsingFallback = true;
            }

            // Initialize logger
            var loggerProvider = GetLoggerProvider();
            _logger = loggerProvider.CreateLogger<EnhancedDataGridControl>();

            // Memory monitoring
            _memoryMonitorTimer = new Timer(MonitorMemoryUsage, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;
            this.KeyDown += OnKeyDown;

            _logger.LogDebug("✅ EnhancedDataGridControl vytvorený úspešně");
        }

        #endregion

        #region Properties

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

        /// <summary>
        /// ✅ OPRAVENÉ: Používa PUBLIC ComponentErrorEventArgs
        /// </summary>
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        public event PropertyChangedEventHandler? PropertyChanged;

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

                _logger.LogInformation("🚀 Začínam inicializáciu s {ColumnCount} stĺpcami", columns?.Count ?? 0);

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

                // Update UI
                UpdateUI();

                await Task.Delay(200); // Dať čas na UI update

                LoadingProgress = 100;
                LoadingMessage = "Dokončené";

                _isInitialized = true;

                // Hide loading panel
                await Task.Delay(500);
                IsLoading = false;

                _logger.LogInformation("✅ Inicializácia dokončená úspešne");
            }
            catch (Exception ex)
            {
                IsLoading = false;
                LoadingMessage = $"Chyba: {ex.Message}";
                _logger.LogError(ex, "❌ Chyba pri inicializácii");

                // ✅ OPRAVA: Používa PUBLIC ComponentErrorEventArgs
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

                _logger.LogInformation("📊 Načítavam {RowCount} riadkov dát", data?.Count ?? 0);

                // Memory management
                await TriggerMemoryCleanup();

                if (ViewModel != null)
                {
                    await ViewModel.LoadDataAsync(data ?? new List<Dictionary<string, object?>>());

                    // Update UI after loading
                    UpdateUI();
                }

                LoadingMessage = "Validácia dokončená";
                LoadingProgress = 100;

                UpdateRowCountDisplay();

                _logger.LogInformation("✅ Dáta načítané úspešne");

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

                    // Update UI po validácii
                    UpdateUI();

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

        public void Reset()
        {
            try
            {
                _logger.LogInformation("Resetting EnhancedDataGridControl");

                if (ViewModel != null)
                {
                    ViewModel.Reset();
                }

                _isInitialized = false;
                IsLoading = false;
                LoadingProgress = 0;
                LoadingMessage = "Pripravené";

                // Update UI
                UpdateUI();

                _logger.LogInformation("EnhancedDataGridControl reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reset");
                HandleError(ex, "Reset");
            }
        }

        public async Task<DataTable> ExportToDataTableAsync()
        {
            try
            {
                _logger.LogDebug("Exporting data to DataTable");

                if (ViewModel != null)
                {
                    var result = await ViewModel.ExportDataAsync();
                    _logger.LogInformation("Successfully exported {RowCount} rows to DataTable", result.Rows.Count);
                    return result;
                }

                _logger.LogWarning("ViewModel is null, returning empty DataTable");
                return new DataTable();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to DataTable");
                HandleError(ex, "ExportToDataTableAsync");
                return new DataTable();
            }
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
                _logger.LogDebug("Clearing all data");

                if (ViewModel != null)
                {
                    await ViewModel.ClearAllDataAsync();
                    UpdateUI();
                    _logger.LogInformation("All data cleared successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
                HandleError(ex, "ClearAllDataAsync");
                throw;
            }
        }

        public async Task RemoveEmptyRowsAsync()
        {
            try
            {
                _logger.LogDebug("Removing empty rows");

                if (ViewModel != null)
                {
                    await ViewModel.RemoveEmptyRowsAsync();
                    UpdateUI();
                    _logger.LogInformation("Empty rows removed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                HandleError(ex, "RemoveEmptyRowsAsync");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateUI()
        {
            try
            {
                if (_isUsingFallback)
                {
                    return; // Fallback UI doesn't need updates
                }

                // Update header - HeaderItemsRepeater existuje v XAML
                if (ViewModel?.Columns != null && HeaderItemsRepeater != null)
                {
                    HeaderItemsRepeater.ItemsSource = ViewModel.Columns;
                    _logger.LogDebug("✅ Header updated with {ColumnCount} columns", ViewModel.Columns.Count);
                }

                // Update data rows - DataRowsItemsRepeater existuje v XAML  
                if (ViewModel?.Rows != null && DataRowsItemsRepeater != null)
                {
                    DataRowsItemsRepeater.ItemsSource = ViewModel.Rows;
                    _logger.LogDebug("✅ Data rows updated with {RowCount} rows", ViewModel.Rows.Count);
                }

                // Update visibility states
                UpdateVisibilityStates();

                // Update status
                UpdateStatusDisplay();

                _logger.LogDebug("✅ UI updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating UI");
                HandleError(ex, "UpdateUI");
            }
        }

        private void UpdateVisibilityStates()
        {
            try
            {
                if (ViewModel?.Rows == null) return;

                var hasData = ViewModel.Rows.Any(r => !r.IsEmpty);

                // EmptyStatePanel existuje v XAML
                if (EmptyStatePanel != null)
                {
                    EmptyStatePanel.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
                }

                // MainScrollViewer existuje v XAML
                if (MainScrollViewer != null)
                {
                    MainScrollViewer.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
                }

                _logger.LogDebug("✅ Visibility states updated - HasData: {HasData}", hasData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating visibility states");
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

        private AdvancedDataGridViewModel CreateViewModel()
        {
            try
            {
                return DependencyInjectionConfig.CreateViewModelWithoutDI();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ViewModel");
                throw;
            }
        }

        private IDataGridLoggerProvider GetLoggerProvider()
        {
            return NullDataGridLoggerProvider.Instance;
        }

        private void MonitorMemoryUsage(object? state)
        {
            try
            {
                var memoryUsage = GC.GetTotalMemory(false);
                if (memoryUsage > 100_000_000) // 100MB
                {
                    _logger.LogWarning("High memory usage detected: {MemoryMB} MB", memoryUsage / 1024 / 1024);
                    _ = TriggerMemoryCleanup();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring memory usage");
            }
        }

        private async Task TriggerMemoryCleanup()
        {
            try
            {
                await Task.Run(() =>
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                });

                _logger.LogDebug("Memory cleanup completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during memory cleanup");
            }
        }

        /// <summary>
        /// ✅ OPRAVA: Používa PUBLIC ComponentErrorEventArgs
        /// </summary>
        private void HandleError(Exception ex, string operation)
        {
            _logger.LogError(ex, "Error in operation: {Operation}", operation);
            ErrorOccurred?.Invoke(this, new ComponentErrorEventArgs(ex, operation));
        }

        #endregion

        #region SIMPLIFIED FALLBACK UI

        private void CreateSimpleFallbackUI()
        {
            try
            {
                _logger?.LogWarning("📋 Creating simplified fallback UI...");

                var simpleText = new TextBlock
                {
                    Text = "⚠️ RpaWinUiComponents DataGrid\nSimplified Mode\nXAML loading failed",
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(20),
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed)
                };

                this.Content = simpleText;
                _logger?.LogInformation("✅ Fallback UI created");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Error creating fallback UI");
            }
        }

        #endregion

        #region Event Handlers

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogDebug("🔧 EnhancedDataGridControl loaded");

                if (ViewModel != null)
                {
                    UpdateUI();
                }

                _logger.LogDebug("✅ EnhancedDataGridControl loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during control loaded");
                CreateSimpleFallbackUI();
                _isUsingFallback = true;
                HandleError(ex, "OnControlLoaded");
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }

        private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // Keyboard handling implementation
        }

        // XAML Event Handlers (simplified)
        private void OnCellDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) { }
        private void OnCellTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) { }
        private void OnCellGotFocus(object sender, RoutedEventArgs e) { }
        private void OnCellLostFocus(object sender, RoutedEventArgs e) { }
        private void OnCellKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e) { }
        private void OnCellTextChanged(object sender, TextChangedEventArgs e) { }

        /// <summary>
        /// ✅ OPRAVA: Subscribe to ViewModel events, konvertuje internal na public
        /// </summary>
        private void SubscribeToViewModel(AdvancedDataGridViewModel viewModel)
        {
            if (viewModel != null)
            {
                viewModel.PropertyChanged += OnViewModelPropertyChanged;

                // ✅ OPRAVA: Subscribe to internal events and convert to public
                viewModel.ErrorOccurred += (sender, internalArgs) =>
                {
                    // Konvertuj internal ComponentErrorEventArgs na public
                    var publicArgs = new ComponentErrorEventArgs(internalArgs.Exception, internalArgs.Operation, internalArgs.AdditionalInfo);
                    ErrorOccurred?.Invoke(this, publicArgs);
                };
            }
        }

        private void UnsubscribeFromViewModel(AdvancedDataGridViewModel viewModel)
        {
            if (viewModel != null)
            {
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                // ErrorOccurred events will be automatically unsubscribed by lambda capture
            }
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdvancedDataGridViewModel.Rows) ||
                e.PropertyName == nameof(AdvancedDataGridViewModel.Columns))
            {
                UpdateUI();
            }
        }

        #endregion

        #region IDisposable & INotifyPropertyChanged

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
                _logger?.LogError(ex, "Error during disposal");
            }
        }

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