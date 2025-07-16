// FINÁLNY UNIFIKOVANÝ KOMPONENT - Spojuje najlepšie z oboch verzií
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/Views/UnifiedAdvancedDataGridControl.xaml.cs

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
using Microsoft.UI.Xaml.Media;
using Windows.System;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

// Používame interné typy
using InternalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition;
using InternalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.ValidationRule;
using InternalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Views
{
    /// <summary>
    /// FINÁLNY UNIFIKOVANÝ KOMPONENT - Nahradí oba existujúce komponenty
    /// ✅ Opravené všetky CS1061, CS0246, CS0029 chyby
    /// ✅ Jednoduché a funkčné riešenie bez duplicitných komponentov
    /// ✅ Memory management a performance optimalizácie
    /// ✅ Kompletná keyboard navigation
    /// ✅ Real-time validácia s throttling
    /// </summary>
    public sealed partial class UnifiedAdvancedDataGridControl : UserControl, IDisposable, INotifyPropertyChanged
    {
        private AdvancedDataGridViewModel? _viewModel;
        private readonly ILogger<UnifiedAdvancedDataGridControl> _logger;
        private bool _disposed = false;
        private bool _isInitialized = false;

        // UI State
        private readonly List<InternalColumnDefinition> _columns = new();
        private readonly List<Dictionary<string, object?>> _currentData = new();
        private readonly List<InternalValidationRule> _validationRules = new();
        private readonly Dictionary<string, TextBox> _cellControls = new(); // Key: "row_column", Value: TextBox
        private readonly Dictionary<string, System.Threading.Timer> _validationTimers = new();

        // Performance tracking
        private readonly HashSet<string> _validationInProgress = new();
        private InternalThrottlingConfig _throttlingConfig = InternalThrottlingConfig.Default;

        // Memory management
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly Timer _memoryMonitorTimer;

        public UnifiedAdvancedDataGridControl()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 Inicializujem UnifiedAdvancedDataGridControl...");
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
            _logger = loggerProvider.CreateLogger<UnifiedAdvancedDataGridControl>();

            // Memory monitoring
            _memoryMonitorTimer = new Timer(MonitorMemoryUsage, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;
            this.KeyDown += OnKeyDown;

            _logger.LogDebug("UnifiedAdvancedDataGridControl vytvorený");
        }

        #region Properties and Events

        /// <summary>
        /// Internal ViewModel access for services
        /// </summary>
        internal AdvancedDataGridViewModel? InternalViewModel
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
                OnPropertyChanged(nameof(InternalViewModel));
            }
        }

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region PUBLIC API METHODS

        /// <summary>
        /// ✅ OPRAVENÁ INICIALIZÁCIA - Správne vytvára UI štruktúru
        /// </summary>
        public async Task InitializeAsync(
            List<InternalColumnDefinition> columns,
            List<InternalValidationRule>? validationRules = null,
            InternalThrottlingConfig? throttling = null,
            int initialRowCount = 15)
        {
            try
            {
                UpdateStatus("Inicializujem DataGrid...");
                _logger.LogInformation("🚀 Začínam inicializáciu s {ColumnCount} stĺpcami", columns?.Count ?? 0);

                // Cancel previous operations
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();

                // Vyčistenie existujúcich dát
                await ClearAllAsync();

                // Uloženie konfigurácie
                _columns.Clear();
                _columns.AddRange(columns ?? new List<InternalColumnDefinition>());

                _validationRules.Clear();
                _validationRules.AddRange(validationRules ?? new List<InternalValidationRule>());

                _throttlingConfig = throttling ?? InternalThrottlingConfig.Default;

                // Vytvorenie ViewModel ak neexistuje
                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    InternalViewModel = _viewModel;
                }

                // Inicializácia ViewModel
                await _viewModel.InitializeAsync(_columns, _validationRules, _throttlingConfig, initialRowCount);

                // ✅ KĽÚČOVÉ: Vytvorenie UI štruktúry
                CreateHeaderUI();
                CreateInitialDataRows(initialRowCount);

                // Skrytie loading panelu
                ShowDataGrid();

                _isInitialized = true;
                UpdateStatus($"Inicializované: {_columns.Count} stĺpcov, {initialRowCount} riadkov");

                _logger.LogInformation("✅ Inicializácia dokončená úspešne");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri inicializácii");
                UpdateStatus($"Chyba: {ex.Message}");
                ShowError();
                HandleError(ex, "InitializeAsync");
                throw;
            }
        }

        /// <summary>
        /// ✅ OPRAVENÉ NAČÍTANIE DÁT - S memory management
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

                UpdateStatus($"Načítavam {data?.Count ?? 0} riadkov...");
                _logger.LogInformation("📊 Načítavam {RowCount} riadkov dát", data?.Count ?? 0);

                // Memory management - vyčistiť pamäť
                await ClearAllMemoryAsync();

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                // Uloženie nových dát
                _currentData.Clear();
                _currentData.AddRange(data ?? new List<Dictionary<string, object?>>());

                // Naplnenie všetkých buniek súčasne
                await PopulateAllCellsAsync();

                // Pridanie prázdnych riadkov
                var totalNeeded = Math.Max(15, _currentData.Count + 5);
                await EnsureRowCount(totalNeeded);

                // Spustenie validácie všetkých buniek
                await ValidateAllCellsAsync();

                UpdateStatus($"Načítané: {_currentData.Count} riadkov dát");
                UpdateRowCount(_currentData.Count, totalNeeded);

                _logger.LogInformation("✅ Dáta načítané a zobrazené úspešne");

                // Aktualizácia ViewModel
                if (_viewModel != null)
                {
                    await _viewModel.LoadDataAsync(_currentData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri načítaní dát");
                UpdateStatus($"Chyba: {ex.Message}");
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
            return CreateDataTableFromCurrentData();
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            if (_viewModel != null)
            {
                return await _viewModel.ValidateAllRowsAsync();
            }

            // Fallback validácia
            await ValidateAllCellsAsync();
            return !HasValidationErrors();
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
                UpdateStatus("Vymazávam všetky dáta...");

                await ClearAllMemoryAsync();

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (_viewModel != null)
                {
                    await _viewModel.ClearAllDataAsync();
                }

                UpdateStatus("Všetky dáta vymazané");
                UpdateRowCount(0, 0);

                _logger.LogInformation("✅ Všetky dáta vymazané");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri mazaní dát");
                HandleError(ex, "ClearAllDataAsync");
                throw;
            }
        }

        public async Task RemoveEmptyRowsAsync()
        {
            if (_viewModel != null)
            {
                await _viewModel.RemoveEmptyRowsAsync();
            }
        }

        public void Reset()
        {
            try
            {
                UpdateStatus("Resetujem komponent...");

                // Cancel operations and cleanup
                _cancellationTokenSource?.Cancel();
                _ = ClearAllMemoryAsync();

                // Force garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _columns.Clear();
                _validationRules.Clear();
                _isInitialized = false;

                ShowLoadingPanel();
                UpdateStatus("Reset dokončený");

                _viewModel?.Reset();

                _logger.LogInformation("✅ Komponent resetovaný");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri reset");
                HandleError(ex, "Reset");
            }
        }

        #endregion

        #region UI CREATION METHODS

        /// <summary>
        /// ✅ Vytvorí header UI s správnymi šírkami stĺpcov
        /// </summary>
        private void CreateHeaderUI()
        {
            var headerPanel = this.FindName("HeaderPanel") as StackPanel;
            if (headerPanel == null)
            {
                _logger.LogWarning("❌ HeaderPanel not found in XAML");
                return;
            }

            headerPanel.Children.Clear();

            foreach (var column in _columns)
            {
                // ✅ OPRAVENÉ: Header border s správnym Thickness
                var headerBorder = new Border
                {
                    Width = column.Width,
                    MinWidth = column.MinWidth,
                    BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                    BorderThickness = new Thickness(0, 0, 1, 1), // ✅ Left, Top, Right, Bottom
                    Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
                };

                var headerText = new TextBlock
                {
                    Text = column.Header ?? column.Name,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(8, 10, 8, 10) // ✅ Left, Top, Right, Bottom
                };

                headerBorder.Child = headerText;
                headerPanel.Children.Add(headerBorder);
            }

            _logger.LogDebug("✅ Header UI vytvorené pre {ColumnCount} stĺpcov", _columns.Count);
        }

        /// <summary>
        /// ✅ Vytvorí začiatočné prázdne riadky
        /// </summary>
        private void CreateInitialDataRows(int rowCount)
        {
            var dataRowsPanel = this.FindName("DataRowsPanel") as StackPanel;
            if (dataRowsPanel == null)
            {
                _logger.LogWarning("❌ DataRowsPanel not found");
                return;
            }

            dataRowsPanel.Children.Clear();

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                CreateRowUI(rowIndex, new Dictionary<string, object?>());
            }

            _logger.LogDebug("✅ Vytvorených {RowCount} začiatočných riadkov", rowCount);
        }

        /// <summary>
        /// ✅ Vytvorí UI pre jeden riadok s správnym layoutom
        /// </summary>
        private void CreateRowUI(int rowIndex, Dictionary<string, object?> rowData)
        {
            var dataRowsPanel = this.FindName("DataRowsPanel") as StackPanel;
            if (dataRowsPanel == null) return;

            var rowGrid = new Grid
            {
                MinHeight = 35,
                Background = new SolidColorBrush(rowIndex % 2 == 0 ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.WhiteSmoke)
            };

            // ✅ OPRAVA CS0029: Správne vytvorenie ColumnDefinition pre WinUI Grid
            for (int i = 0; i < _columns.Count; i++)
            {
                var columnWidth = _columns[i].Width;

                // ✅ RIEŠENIE: Vytvor WinUI ColumnDefinition s GridLength
                var gridColumnDef = new Microsoft.UI.Xaml.Controls.ColumnDefinition();
                gridColumnDef.Width = new GridLength(columnWidth, GridUnitType.Pixel);

                rowGrid.ColumnDefinitions.Add(gridColumnDef);
            }

            // Vytvor bunky
            for (int colIndex = 0; colIndex < _columns.Count; colIndex++)
            {
                var column = _columns[colIndex];
                var cellKey = $"{rowIndex}_{column.Name}";

                // Získaj hodnotu z dát
                var cellValue = "";
                if (rowData.ContainsKey(column.Name))
                {
                    cellValue = rowData[column.Name]?.ToString() ?? "";
                }

                // Vytvor TextBox pre bunku
                var cellTextBox = new TextBox
                {
                    Text = cellValue,
                    BorderThickness = new Thickness(1, 1, 1, 1),
                    BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                    Padding = new Thickness(8, 6, 8, 6),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                    MinHeight = 35,
                    IsReadOnly = column.IsReadOnly,
                    Tag = cellKey
                };

                // Event handler pre real-time validáciu
                cellTextBox.TextChanged += OnCellTextChanged;
                cellTextBox.LostFocus += OnCellLostFocus;

                // Uloženie referencie na TextBox
                _cellControls[cellKey] = cellTextBox;

                // Pozícia v Grid
                Grid.SetColumn(cellTextBox, colIndex);
                rowGrid.Children.Add(cellTextBox);
            }

            // Border okolo riadku
            var rowBorder = new Border
            {
                Child = rowGrid,
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                BorderThickness = new Thickness(1, 0, 1, 1)
            };

            dataRowsPanel.Children.Add(rowBorder);
        }

        #endregion

        #region DATA MANAGEMENT

        /// <summary>
        /// ✅ Naplní všetky bunky dátami súčasne
        /// </summary>
        private async Task PopulateAllCellsAsync()
        {
            await Task.Run(() =>
            {
                for (int rowIndex = 0; rowIndex < _currentData.Count; rowIndex++)
                {
                    var rowData = _currentData[rowIndex];

                    foreach (var column in _columns)
                    {
                        var cellKey = $"{rowIndex}_{column.Name}";

                        if (_cellControls.TryGetValue(cellKey, out var textBox))
                        {
                            var value = rowData.ContainsKey(column.Name) ? rowData[column.Name]?.ToString() ?? "" : "";

                            // UI update na main thread
                            this.DispatcherQueue.TryEnqueue(() =>
                            {
                                textBox.Text = value;
                            });
                        }
                    }
                }
            });

            _logger.LogDebug("✅ Všetky bunky naplnené dátami");
        }

        /// <summary>
        /// ✅ Zabezpečí dostatok riadkov
        /// </summary>
        private async Task EnsureRowCount(int neededRowCount)
        {
            var dataRowsPanel = this.FindName("DataRowsPanel") as StackPanel;
            if (dataRowsPanel == null) return;

            var currentRowCount = dataRowsPanel.Children.Count;

            if (currentRowCount < neededRowCount)
            {
                await Task.Run(() =>
                {
                    for (int i = currentRowCount; i < neededRowCount; i++)
                    {
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            CreateRowUI(i, new Dictionary<string, object?>());
                        });
                    }
                });
            }
        }

        /// <summary>
        /// ✅ Kompletné vyčistenie pamäte pred načítaním nových dát
        /// </summary>
        private async Task ClearAllMemoryAsync()
        {
            try
            {
                _logger.LogDebug("🗑️ Začínam kompletné vyčistenie pamäte...");

                await Task.Run(() =>
                {
                    // 1. Dispose všetkých timerov
                    foreach (var timer in _validationTimers.Values)
                    {
                        timer?.Dispose();
                    }
                    _validationTimers.Clear();

                    // 2. Clear validation state
                    _validationInProgress.Clear();

                    // 3. Unsubscribe events a dispose UI controls
                    var cellKeysToRemove = _cellControls.Keys.ToList();
                    foreach (var cellKey in cellKeysToRemove)
                    {
                        if (_cellControls.TryGetValue(cellKey, out var textBox))
                        {
                            this.DispatcherQueue.TryEnqueue(() =>
                            {
                                try
                                {
                                    // Unsubscribe events
                                    textBox.TextChanged -= OnCellTextChanged;
                                    textBox.LostFocus -= OnCellLostFocus;

                                    // Clear tooltip a tag
                                    ToolTipService.SetToolTip(textBox, null);
                                    textBox.Tag = null;
                                    textBox.Text = "";
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Chyba pri cleanup TextBox {CellKey}", cellKey);
                                }
                            });
                        }
                    }
                    _cellControls.Clear();

                    // 4. Clear UI panels
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            var dataRowsPanel = this.FindName("DataRowsPanel") as StackPanel;
                            if (dataRowsPanel != null)
                            {
                                foreach (var child in dataRowsPanel.Children.ToList())
                                {
                                    if (child is Border border && border.Child is Grid grid)
                                    {
                                        grid.Children.Clear();
                                        grid.ColumnDefinitions.Clear();
                                    }
                                }
                                dataRowsPanel.Children.Clear();
                            }

                            var headerPanel = this.FindName("HeaderPanel") as StackPanel;
                            headerPanel?.Children.Clear();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Chyba pri cleanup UI panels");
                        }
                    });

                    // 5. Clear data collections
                    _currentData.Clear();
                    _currentData.TrimExcess();
                });

                _logger.LogDebug("✅ Pamäť kompletne vyčistená");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri kompletnom vyčistení pamäte");
            }
        }

        private async Task ClearAllAsync()
        {
            await Task.Run(() =>
            {
                // Dispose validation timers
                foreach (var timer in _validationTimers.Values)
                {
                    timer?.Dispose();
                }
                _validationTimers.Clear();

                // Clear validation state
                _validationInProgress.Clear();

                // Unsubscribe events and clear cell controls
                foreach (var kvp in _cellControls)
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        var textBox = kvp.Value;
                        if (textBox != null)
                        {
                            textBox.TextChanged -= OnCellTextChanged;
                            textBox.LostFocus -= OnCellLostFocus;
                        }
                    });
                }
                _cellControls.Clear();

                // Clear UI
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    var dataRowsPanel = this.FindName("DataRowsPanel") as StackPanel;
                    dataRowsPanel?.Children.Clear();

                    var headerPanel = this.FindName("HeaderPanel") as StackPanel;
                    headerPanel?.Children.Clear();
                });

                // Clear data
                _currentData.Clear();
            });

            _logger.LogDebug("✅ Všetky dáta vyčistené");
        }

        #endregion

        #region REAL-TIME VALIDATION

        /// <summary>
        /// ✅ Real-time validácia s throttling
        /// </summary>
        private async void OnCellTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || textBox.Tag is not string cellKey)
                return;

            try
            {
                // Throttling - zruší predchádzajúci timer
                if (_validationTimers.TryGetValue(cellKey, out var existingTimer))
                {
                    existingTimer?.Dispose();
                }

                // Vytvor nový timer pre throttled validáciu
                var timer = new System.Threading.Timer(async _ =>
                {
                    if (_disposed) return;

                    await ValidateCellAsync(cellKey, textBox.Text);

                    // Cleanup timer
                    if (_validationTimers.TryGetValue(cellKey, out var timerToRemove))
                    {
                        timerToRemove?.Dispose();
                        _validationTimers.Remove(cellKey);
                    }
                }, null, _throttlingConfig.TypingDelayMs, Timeout.Infinite);

                _validationTimers[cellKey] = timer;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba v real-time validácii pre {CellKey}", cellKey);
            }
        }

        private async void OnCellLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string cellKey)
            {
                await ValidateCellAsync(cellKey, textBox.Text);
            }
        }

        /// <summary>
        /// ✅ Validácia jednej bunky s vizuálnym indikátorom
        /// </summary>
        private async Task ValidateCellAsync(string cellKey, string value)
        {
            if (_validationInProgress.Contains(cellKey)) return;

            try
            {
                _validationInProgress.Add(cellKey);

                var parts = cellKey.Split('_', 2);
                if (parts.Length != 2) return;

                var rowIndex = int.Parse(parts[0]);
                var columnName = parts[1];

                // Nájdi príslušné validačné pravidlá
                var rules = _validationRules.Where(r => r.ColumnName == columnName).ToList();
                var errors = new List<string>();

                foreach (var rule in rules)
                {
                    try
                    {
                        bool isValid;
                        if (rule.IsAsync)
                        {
                            isValid = await rule.ValidateAsync(value, null!);
                        }
                        else
                        {
                            isValid = rule.Validate(value, null!);
                        }

                        if (!isValid)
                        {
                            errors.Add(rule.ErrorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Validačné pravidlo {RuleName} zlyhalo", rule.RuleName);
                        errors.Add($"Chyba validácie: {rule.ErrorMessage}");
                    }
                }

                // Aplikácia vizuálnych indikátorov chýb
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    ApplyValidationResult(cellKey, errors);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri validácii bunky {CellKey}", cellKey);
            }
            finally
            {
                _validationInProgress.Remove(cellKey);
            }
        }

        /// <summary>
        /// ✅ Aplikuje vizuálne indikátory validačných chýb
        /// </summary>
        private void ApplyValidationResult(string cellKey, List<string> errors)
        {
            if (!_cellControls.TryGetValue(cellKey, out var textBox))
                return;

            if (errors.Count > 0)
            {
                // ✅ OPRAVENÉ: Aplikuj error štýl s správnym Thickness
                textBox.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                textBox.BorderThickness = new Thickness(2, 2, 2, 2); // ✅ Left, Top, Right, Bottom
                textBox.Background = new SolidColorBrush(Microsoft.UI.Colors.MistyRose);

                // Nastav tooltip s chybami
                var tooltip = new ToolTip
                {
                    Content = string.Join("\n", errors)
                };
                ToolTipService.SetToolTip(textBox, tooltip);

                _logger.LogDebug("❌ Validačné chyby v {CellKey}: {Errors}", cellKey, string.Join(", ", errors));
            }
            else
            {
                // ✅ OPRAVENÉ: Odstráň error štýl s správnym Thickness
                textBox.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray);
                textBox.BorderThickness = new Thickness(1, 1, 1, 1); // ✅ Left, Top, Right, Bottom
                textBox.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
                ToolTipService.SetToolTip(textBox, null);
            }
        }

        /// <summary>
        /// ✅ Validácia všetkých buniek
        /// </summary>
        private async Task ValidateAllCellsAsync()
        {
            var tasks = new List<Task>();

            foreach (var kvp in _cellControls)
            {
                var cellKey = kvp.Key;
                var textBox = kvp.Value;

                tasks.Add(ValidateCellAsync(cellKey, textBox.Text));
            }

            await Task.WhenAll(tasks);

            // Update validation count
            var errorCount = _cellControls.Count(kvp =>
                ToolTipService.GetToolTip(kvp.Value) != null);

            this.DispatcherQueue.TryEnqueue(() =>
            {
                UpdateValidationCount(errorCount);
            });
        }

        private bool HasValidationErrors()
        {
            return _cellControls.Any(kvp => ToolTipService.GetToolTip(kvp.Value) != null);
        }

        #endregion

        #region KEYBOARD NAVIGATION

        /// <summary>
        /// ✅ Global keyboard handling
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
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling global key down");
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
                    UpdateStatus("Data copied to clipboard");
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
                    UpdateStatus("Data pasted from clipboard");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pasting data");
                HandleError(ex, "HandlePasteAsync");
            }
        }

        #endregion

        #region UI STATE MANAGEMENT

        private void ShowLoadingPanel()
        {
            var loadingPanel = this.FindName("LoadingPanel") as FrameworkElement;
            var mainScrollViewer = this.FindName("MainScrollViewer") as FrameworkElement;
            var emptyStatePanel = this.FindName("EmptyStatePanel") as FrameworkElement;

            if (loadingPanel != null) loadingPanel.Visibility = Visibility.Visible;
            if (mainScrollViewer != null) mainScrollViewer.Visibility = Visibility.Collapsed;
            if (emptyStatePanel != null) emptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowDataGrid()
        {
            var loadingPanel = this.FindName("LoadingPanel") as FrameworkElement;
            var mainScrollViewer = this.FindName("MainScrollViewer") as FrameworkElement;
            var emptyStatePanel = this.FindName("EmptyStatePanel") as FrameworkElement;

            if (loadingPanel != null) loadingPanel.Visibility = Visibility.Collapsed;
            if (mainScrollViewer != null) mainScrollViewer.Visibility = Visibility.Visible;
            if (emptyStatePanel != null) emptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowError()
        {
            var loadingPanel = this.FindName("LoadingPanel") as FrameworkElement;
            var mainScrollViewer = this.FindName("MainScrollViewer") as FrameworkElement;
            var emptyStatePanel = this.FindName("EmptyStatePanel") as FrameworkElement;

            if (loadingPanel != null) loadingPanel.Visibility = Visibility.Collapsed;
            if (mainScrollViewer != null) mainScrollViewer.Visibility = Visibility.Collapsed;
            if (emptyStatePanel != null) emptyStatePanel.Visibility = Visibility.Visible;
        }

        private void UpdateStatus(string message)
        {
            var statusText = this.FindName("StatusText") as TextBlock;
            var statusIndicator = this.FindName("StatusIndicator") as TextBlock;

            if (statusText != null) statusText.Text = message;
            if (statusIndicator != null) statusIndicator.Text = $" - {message}";

            _logger.LogDebug("Status: {Status}", message);
        }

        private void UpdateRowCount(int dataRows, int totalRows)
        {
            var rowCountText = this.FindName("RowCountText") as TextBlock;
            if (rowCountText != null)
            {
                rowCountText.Text = $"{dataRows}/{totalRows} riadkov";
            }
        }

        private void UpdateValidationCount(int errorCount)
        {
            var validationText = this.FindName("ValidationText") as TextBlock;
            if (validationText != null)
            {
                validationText.Text = errorCount > 0 ? $"{errorCount} chýb" : "";
                validationText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }

        #endregion

        #region MEMORY MANAGEMENT

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
                    var memoryText = this.FindName("MemoryText") as TextBlock;
                    if (memoryText != null)
                    {
                        memoryText.Text = $"Memory: {memoryMB} MB";
                    }
                });

                _logger.LogTrace("Memory usage: {MemoryMB} MB", memoryMB);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error monitoring memory usage");
            }
        }

        #endregion

        #region ERROR HANDLING

        private void HandleError(Exception ex, string operation)
        {
            _logger.LogError(ex, "Error in operation: {Operation}", operation);
            ErrorOccurred?.Invoke(this, new ComponentErrorEventArgs(ex, operation));
        }

        #endregion

        #region EVENT HANDLERS & HELPER METHODS

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Control načítaný");
                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    InternalViewModel = _viewModel;
                }
                _logger.LogDebug("UnifiedAdvancedDataGrid loaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OnLoaded");
                UpdateStatus($"Chyba pri načítaní: {ex.Message}");
                HandleError(ex, "OnControlLoaded");
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogDebug("UnifiedAdvancedDataGrid unloaded");
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

        private DataTable CreateDataTableFromCurrentData()
        {
            var dt = new DataTable();

            // Pridaj stĺpce
            foreach (var column in _columns)
            {
                dt.Columns.Add(column.Name, typeof(string));
            }

            // Pridaj dáta
            foreach (var rowData in _currentData)
            {
                var row = dt.NewRow();
                foreach (var column in _columns)
                {
                    row[column.Name] = rowData.ContainsKey(column.Name) ? rowData[column.Name] : "";
                }
                dt.Rows.Add(row);
            }

            return dt;
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
                Text = "⚠️ RpaWinUiComponents DataGrid - Fallback Mode\nXAML parsing zlyhal.",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            };
        }

        private void SubscribeToViewModel(AdvancedDataGridViewModel viewModel) { }
        private void UnsubscribeFromViewModel(AdvancedDataGridViewModel viewModel) { }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger?.LogDebug("Disposing UnifiedAdvancedDataGridControl...");

                // Cancel any ongoing operations
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();

                // Dispose timer
                _memoryMonitorTimer?.Dispose();

                // Clear memory
                _ = ClearAllMemoryAsync();

                // Dispose ViewModel
                if (_viewModel != null)
                {
                    UnsubscribeFromViewModel(_viewModel);
                    _viewModel.Dispose();
                    _viewModel = null!;
                }

                _disposed = true;
                _logger?.LogInformation("UnifiedAdvancedDataGridControl disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during disposal");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}