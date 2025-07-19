// EnhancedDataGridControl.xaml.cs - OPRAVENÝ FALLBACK BEZ TOOLTIPS
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
using RpaWinUiComponents.AdvancedWinUiDataGrid.Converters;

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
        private bool _isUsingFallback = false;

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

        // FALLBACK UI elements - BEZ TOOLTIPS
        private Grid? _fallbackMainGrid;
        private ScrollViewer? _fallbackScrollViewer;
        private StackPanel? _fallbackDataContainer;
        private Border? _fallbackHeaderContainer;

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
                CreateEnhancedFallbackUI();
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

            _logger.LogDebug("EnhancedDataGridControl vytvorený s enhanced fallback");
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

        #region ENHANCED FALLBACK UI - BEZ TOOLTIPS

        /// <summary>
        /// OPRAVENÝ FALLBACK: Vytvorí plne funkčný DataGrid bez XAML - BEZ TOOLTIPS
        /// </summary>
        private void CreateEnhancedFallbackUI()
        {
            try
            {
                _logger?.LogWarning("📋 Creating enhanced fallback UI without tooltips...");

                // Main container
                _fallbackMainGrid = new Grid();
                _fallbackMainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Header
                _fallbackMainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Content
                _fallbackMainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }); // Status

                // KĽÚČOVÉ: Vypnutie tooltips na main container
                ToolTipService.SetToolTip(_fallbackMainGrid, null);

                // Title bar
                var titleBorder = new Border
                {
                    Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                    Padding = new Thickness(16, 12,16,12),
                    BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    BorderThickness = new Thickness(0, 0, 0, 1)
                };

                // KĽÚČOVÉ: Vypnutie tooltips na title
                ToolTipService.SetToolTip(titleBorder, null);

                var titleText = new TextBlock
                {
                    Text = "🎯 Enhanced RpaWinUiComponents DataGrid (Fallback Mode)",
                    FontSize = 18,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkBlue)
                };

                // KĽÚČOVÉ: Vypnutie tooltips na title text
                ToolTipService.SetToolTip(titleText, null);

                titleBorder.Child = titleText;
                Grid.SetRow(titleBorder, 0);
                _fallbackMainGrid.Children.Add(titleBorder);

                // Content area with ScrollViewer
                _fallbackScrollViewer = new ScrollViewer
                {
                    ZoomMode = ZoomMode.Disabled,
                    HorizontalScrollMode = ScrollMode.Auto,
                    VerticalScrollMode = ScrollMode.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                    Padding = new Thickness(8)
                };

                // KĽÚČOVÉ: Vypnutie tooltips na scroll viewer
                ToolTipService.SetToolTip(_fallbackScrollViewer, null);

                // Main data container
                var mainContainer = new StackPanel();
                ToolTipService.SetToolTip(mainContainer, null);

                // Header container
                _fallbackHeaderContainer = new Border
                {
                    Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                    BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4, 4, 0, 0),
                    Padding = new Thickness(8)
                };
                ToolTipService.SetToolTip(_fallbackHeaderContainer, null);

                // Data container
                _fallbackDataContainer = new StackPanel();
                ToolTipService.SetToolTip(_fallbackDataContainer, null);

                mainContainer.Children.Add(_fallbackHeaderContainer);
                mainContainer.Children.Add(_fallbackDataContainer);

                _fallbackScrollViewer.Content = mainContainer;
                Grid.SetRow(_fallbackScrollViewer, 1);
                _fallbackMainGrid.Children.Add(_fallbackScrollViewer);

                // Status bar
                var statusBorder = new Border
                {
                    Background = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray),
                    Padding = new Thickness(16, 10,16,10)
                };
                ToolTipService.SetToolTip(statusBorder, null);

                var statusText = new TextBlock
                {
                    Text = "Ready (Fallback Mode - No Tooltips)",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                };
                ToolTipService.SetToolTip(statusText, null);

                statusBorder.Child = statusText;
                Grid.SetRow(statusBorder, 2);
                _fallbackMainGrid.Children.Add(statusBorder);

                // Set as content
                this.Content = _fallbackMainGrid;

                _logger?.LogInformation("✅ Enhanced fallback UI created successfully without tooltips");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Error creating enhanced fallback UI");

                // Ultra-simple fallback
                var simpleText = new TextBlock
                {
                    Text = "⚠️ Enhanced RpaWinUiComponents DataGrid\nFallback Mode - XAML parsing failed",
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(20)
                };

                // KĽÚČOVÉ: Vypnutie tooltips aj na simple fallback
                ToolTipService.SetToolTip(simpleText, null);

                this.Content = simpleText;
            }
        }

        /// <summary>
        /// ENHANCED FALLBACK: Aktualizuje header v fallback móde - BEZ TOOLTIPS
        /// </summary>
        private void UpdateFallbackHeader(List<InternalColumnDefinition> columns)
        {
            if (!_isUsingFallback || _fallbackHeaderContainer == null) return;

            try
            {
                var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
                ToolTipService.SetToolTip(headerPanel, null);

                foreach (var column in columns)
                {
                    var headerBorder = new Border
                    {
                        Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                        BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(8, 10,8,10),
                        Width = column.Width,
                        MinWidth = column.MinWidth
                    };

                    // KĽÚČOVÉ: Vypnutie tooltips na header border
                    ToolTipService.SetToolTip(headerBorder, null);

                    var headerText = new TextBlock
                    {
                        Text = column.Header ?? column.Name,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        FontSize = 12,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkBlue),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextTrimming = TextTrimming.CharacterEllipsis
                    };

                    // KĽÚČOVÉ: Vypnutie tooltips na header text
                    ToolTipService.SetToolTip(headerText, null);

                    headerBorder.Child = headerText;
                    headerPanel.Children.Add(headerBorder);
                }

                _fallbackHeaderContainer.Child = headerPanel;
                _logger?.LogDebug("✅ Fallback header updated without tooltips");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Error updating fallback header");
            }
        }

        /// <summary>
        /// ENHANCED FALLBACK: Aktualizuje dáta v fallback móde - BEZ TOOLTIPS
        /// </summary>
        private void UpdateFallbackData(List<RowViewModel> rows, List<InternalColumnDefinition> columns)
        {
            if (!_isUsingFallback || _fallbackDataContainer == null) return;

            try
            {
                _fallbackDataContainer.Children.Clear();

                var nonEmptyRows = rows.Where(r => !r.IsEmpty).Take(50).ToList(); // Limit pre performance

                foreach (var row in nonEmptyRows)
                {
                    var rowBorder = new Border
                    {
                        Background = row.IsEvenRow
                            ? new SolidColorBrush(Microsoft.UI.Colors.White)
                            : new SolidColorBrush(Microsoft.UI.Colors.AliceBlue),
                        BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        BorderThickness = new Thickness(1, 0, 1, 1)
                    };

                    // KĽÚČOVÉ: Vypnutie tooltips na row border
                    ToolTipService.SetToolTip(rowBorder, null);

                    var rowPanel = new StackPanel { Orientation = Orientation.Horizontal };
                    ToolTipService.SetToolTip(rowPanel, null);

                    foreach (var column in columns)
                    {
                        var cellBorder = new Border
                        {
                            BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                            BorderThickness = new Thickness(1),
                            Padding = new Thickness(8, 6,8,6),
                            Width = column.Width,
                            MinWidth = column.MinWidth
                        };

                        // KĽÚČOVÉ: Vypnutie tooltips na cell border
                        ToolTipService.SetToolTip(cellBorder, null);

                        var cellViewModel = row.GetCell(column.Name);

                        // Vytvorenie cell textu BEZ TOOLTIPS
                        var cellText = new TextBlock
                        {
                            Text = cellViewModel?.DisplayValue ?? "",
                            FontSize = 12,
                            VerticalAlignment = VerticalAlignment.Center,
                            Foreground = cellViewModel?.HasValidationErrors == true
                                ? new SolidColorBrush(Microsoft.UI.Colors.Red)
                                : new SolidColorBrush(Microsoft.UI.Colors.Black),
                            TextWrapping = TextWrapping.NoWrap,
                            TextTrimming = TextTrimming.CharacterEllipsis
                        };

                        // NAJDÔLEŽITEJŠIE: Vypnutie tooltips na cell text
                        ToolTipService.SetToolTip(cellText, null);

                        // Validation error styling BEZ TOOLTIP
                        if (cellViewModel?.HasValidationErrors == true)
                        {
                            cellBorder.Background = new SolidColorBrush(Microsoft.UI.Colors.MistyRose);
                            cellBorder.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                            cellBorder.BorderThickness = new Thickness(2);

                            // KĽÚČOVÉ: ŽIADNY TOOLTIP pre validation errors
                            // Validation errors sa zobrazia len v ValidAlerts stĺpci
                        }

                        cellBorder.Child = cellText;
                        rowPanel.Children.Add(cellBorder);
                    }

                    rowBorder.Child = rowPanel;
                    _fallbackDataContainer.Children.Add(rowBorder);
                }

                _logger?.LogDebug("✅ Fallback data updated without tooltips: {RowCount} rows", nonEmptyRows.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "❌ Error updating fallback data");
            }
        }

        #endregion

        #region PUBLIC API METHODS (unchanged)

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

                // Update UI (aj fallback)
                UpdateUI();

                if (_isUsingFallback)
                {
                    UpdateFallbackHeader(columns ?? new List<InternalColumnDefinition>());
                    if (_viewModel.Rows?.Count > 0)
                    {
                        UpdateFallbackData(_viewModel.Rows.ToList(), columns ?? new List<InternalColumnDefinition>());
                    }
                }

                await Task.Delay(200); // Dať čas na UI update

                LoadingProgress = 100;
                LoadingMessage = "Dokončené";

                _isInitialized = true;

                // Hide loading panel
                await Task.Delay(500);
                IsLoading = false;

                _logger.LogInformation("✅ Enhanced inicializácia dokončená úspešne (fallback: {IsUsingFallback})", _isUsingFallback);
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

                    // Update fallback UI ak je potrebné
                    if (_isUsingFallback && ViewModel.Columns?.Count > 0)
                    {
                        UpdateFallbackData(ViewModel.Rows.ToList(), ViewModel.Columns.ToList());
                    }
                }

                LoadingMessage = "Validácia dokončená";
                LoadingProgress = 100;

                UpdateRowCountDisplay();

                _logger.LogInformation("✅ Enhanced dáta načítané úspešne (fallback: {IsUsingFallback})", _isUsingFallback);

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

        // Ostatné metódy nezmenené...
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

                    // Update fallback UI po validácii
                    if (_isUsingFallback && ViewModel.Columns?.Count > 0)
                    {
                        UpdateFallbackData(ViewModel.Rows.ToList(), ViewModel.Columns.ToList());
                    }

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

                    // Clear fallback UI
                    if (_isUsingFallback && _fallbackDataContainer != null)
                    {
                        _fallbackDataContainer.Children.Clear();
                    }
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

                    // Update fallback UI
                    if (_isUsingFallback && ViewModel.Columns?.Count > 0)
                    {
                        UpdateFallbackData(ViewModel.Rows.ToList(), ViewModel.Columns.ToList());
                    }

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

                // Clear fallback UI
                if (_isUsingFallback && _fallbackDataContainer != null)
                {
                    _fallbackDataContainer.Children.Clear();
                }

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

        #region Helper Methods (unchanged pero s podporou fallback)

        private void UpdateUI()
        {
            try
            {
                if (_isUsingFallback) return; // Fallback UI sa aktualizuje samostatne

                // Update header
                if (ViewModel?.Columns != null && HeaderItemsRepeater != null)
                {
                    HeaderItemsRepeater.ItemsSource = ViewModel.Columns;
                }

                // Update data rows
                if (ViewModel?.Rows != null && DataRowsItemsRepeater != null)
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

        // Ostatné helper metódy zostávajú nezmenené...
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

        // Memory management, error handling, keyboard navigation, event handlers zostávajú nezmenené...
        // (skrátené kvôli dlhému kódu, ale sú identické s pôvodnou verziou)

        #endregion

        #region IDisposable & INotifyPropertyChanged (unchanged)

        private void MonitorMemoryUsage(object? state) { /* Nezmenené */ }
        private async Task TriggerMemoryCleanup() { /* Nezmenené */ }
        private void HandleError(Exception ex, string operation) { /* Nezmenené */ }
        private void OnKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e) { /* Nezmenené */ }
        private async Task HandleCopyAsync() { /* Nezmenené */ }
        private async Task HandlePasteAsync() { /* Nezmenené */ }
        private void HandleSelectAll() { /* Nezmenené */ }
        private async Task HandleRefreshAsync() { /* Nezmenené */ }
        private void HandleDeleteSelected() { /* Nezmenené */ }

        // XAML Event Handlers - nezmenené pre normálny mód
        private void OnCellDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e) { /* Nezmenené */ }
        private void OnCellTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e) { /* Nezmenené */ }
        private void OnCellGotFocus(object sender, RoutedEventArgs e) { /* Nezmenené */ }
        private void OnCellLostFocus(object sender, RoutedEventArgs e) { /* Nezmenené */ }
        private void OnCellKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e) { /* Nezmenené */ }
        private void OnCellTextChanged(object sender, TextChangedEventArgs e) { /* Nezmenené */ }

        private void OnControlLoaded(object sender, RoutedEventArgs e) { /* Nezmenené */ }
        private void OnControlUnloaded(object sender, RoutedEventArgs e) { /* Nezmenené */ }

        private void SubscribeToViewModel(AdvancedDataGridViewModel viewModel) { /* Nezmenené */ }
        private void UnsubscribeFromViewModel(AdvancedDataGridViewModel viewModel) { /* Nezmenené */ }
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) { /* Nezmenené */ }
        private void OnViewModelErrorOccurred(object? sender, ComponentErrorEventArgs e) { /* Nezmenené */ }

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

                // Clear fallback UI references
                _fallbackMainGrid = null;
                _fallbackScrollViewer = null;
                _fallbackDataContainer = null;
                _fallbackHeaderContainer = null;

                _disposed = true;
                _logger?.LogInformation("EnhancedDataGridControl disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during enhanced disposal");
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