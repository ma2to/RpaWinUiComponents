// KOMPLETNE PREROBENÉ AdvancedDataGridControl.xaml.cs - Správna implementácia UI a validácie
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
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

// Internal typy
using InternalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition;
using InternalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.ValidationRule;
using InternalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Views
{
    public sealed partial class AdvancedDataGridControl : UserControl, IDisposable, INotifyPropertyChanged
    {
        private AdvancedDataGridViewModel? _viewModel;
        private readonly ILogger<AdvancedDataGridControl> _logger;
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

        public AdvancedDataGridControl()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 Inicializujem AdvancedDataGridControl...");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("✅ InitializeComponent() úspešne zavolaný");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ XAML Error: {ex.Message}");
                CreateFallbackUI();
            }

            // Inicializácia logger
            var loggerProvider = GetLoggerProvider();
            _logger = loggerProvider.CreateLogger<AdvancedDataGridControl>();

            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;

            _logger.LogDebug("AdvancedDataGridControl vytvorený");
        }

        #region Properties and Events

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
                throw;
            }
        }

        /// <summary>
        /// ✅ OPRAVENÉ NAČÍTANIE DÁT - Skutočne zobrazuje všetky dáta súčasne
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

                // Vyčistenie existujúcich dát (ale zachovanie UI štruktúry)
                ClearCurrentData();

                // Uloženie nových dát
                _currentData.Clear();
                _currentData.AddRange(data ?? new List<Dictionary<string, object?>>());

                // ✅ KĽÚČOVÉ: Naplnenie všetkých buniek súčasne
                await PopulateAllCellsAsync();

                // Pridanie prázdnych riadkov
                var totalNeeded = Math.Max(15, _currentData.Count + 5);
                await EnsureRowCount(totalNeeded);

                // ✅ KĽÚČOVÉ: Spustenie validácie všetkých buniek
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

                // ✅ OPRAVENÉ: Správne vyčistenie bez memory leaks
                await ClearAllAsync();

                if (_viewModel != null)
                {
                    await _viewModel.ClearAllDataAsync();
                }

                UpdateStatus("Všetky dáta vymazané");
                UpdateRowCount(0, 0);

                _logger.LogInformation("✅ Všetky dáta vymazané bez memory leaks");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri mazaní dát");
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

                // ✅ OPRAVENÉ: Kompletný reset bez memory leaks
                _ = ClearAllAsync();

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
                var headerBorder = new Border
                {
                    Width = column.Width,
                    MinWidth = column.MinWidth,
                    BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
                };

                var headerText = new TextBlock
                {
                    Text = column.Header ?? column.Name,
                    Style = this.Resources["HeaderTextStyle"] as Style
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

            // Vytvor Grid pre riadok
            var rowGrid = new Grid
            {
                MinHeight = 35,
                Background = new SolidColorBrush(rowIndex % 2 == 0 ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.WhiteSmoke)
            };

            // Definície stĺpcov
            for (int i = 0; i < _columns.Count; i++)
            {
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_columns[i].Width) });
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
                    Style = this.Resources["DataCellTextBoxStyle"] as Style,
                    IsReadOnly = column.IsReadOnly,
                    Tag = cellKey,
                    BorderThickness = new Thickness(0, 0, 1, 1)
                };

                // ✅ KĽÚČOVÉ: Event handler pre real-time validáciu
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
        /// ✅ OPRAVENÉ: Správne vyčistenie dát bez memory leaks
        /// </summary>
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

            _logger.LogDebug("✅ Všetky dáta vyčistené bez memory leaks");
        }

        private void ClearCurrentData()
        {
            foreach (var kvp in _cellControls)
            {
                kvp.Value.Text = "";
                RemoveValidationError(kvp.Key);
            }
        }

        #endregion

        #region REAL-TIME VALIDATION

        /// <summary>
        /// ✅ OPRAVENÁ real-time validácia s throttling
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
                            isValid = await rule.ValidateAsync(value, null);
                        }
                        else
                        {
                            isValid = rule.Validate(value, null);
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

                // ✅ KĽÚČOVÉ: Aplikácia vizuálnych indikátorov chýb
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
                // Aplikuj error štýl
                textBox.Style = this.Resources["ErrorCellTextBoxStyle"] as Style;

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
                // Odstráň error štýl
                textBox.Style = this.Resources["DataCellTextBoxStyle"] as Style;
                ToolTipService.SetToolTip(textBox, null);
            }
        }

        private void RemoveValidationError(string cellKey)
        {
            if (_cellControls.TryGetValue(cellKey, out var textBox))
            {
                textBox.Style = this.Resources["DataCellTextBoxStyle"] as Style;
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

        #region UI STATE MANAGEMENT

        private void ShowLoadingPanel()
        {
            var loadingPanel = this.FindName("LoadingPanel") as FrameworkElement;
            var dataGridContainer = this.FindName("DataGridContainer") as FrameworkElement;
            var emptyStatePanel = this.FindName("EmptyStatePanel") as FrameworkElement;

            if (loadingPanel != null) loadingPanel.Visibility = Visibility.Visible;
            if (dataGridContainer != null) dataGridContainer.Visibility = Visibility.Collapsed;
            if (emptyStatePanel != null) emptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowDataGrid()
        {
            var loadingPanel = this.FindName("LoadingPanel") as FrameworkElement;
            var dataGridContainer = this.FindName("DataGridContainer") as FrameworkElement;
            var emptyStatePanel = this.FindName("EmptyStatePanel") as FrameworkElement;

            if (loadingPanel != null) loadingPanel.Visibility = Visibility.Collapsed;
            if (dataGridContainer != null) dataGridContainer.Visibility = Visibility.Visible;
            if (emptyStatePanel != null) emptyStatePanel.Visibility = Visibility.Collapsed;
        }

        private void ShowError()
        {
            var loadingPanel = this.FindName("LoadingPanel") as FrameworkElement;
            var dataGridContainer = this.FindName("DataGridContainer") as FrameworkElement;
            var emptyStatePanel = this.FindName("EmptyStatePanel") as FrameworkElement;

            if (loadingPanel != null) loadingPanel.Visibility = Visibility.Collapsed;
            if (dataGridContainer != null) dataGridContainer.Visibility = Visibility.Collapsed;
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
                _logger.LogDebug("AdvancedDataGrid loaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OnLoaded");
                UpdateStatus($"Chyba pri načítaní: {ex.Message}");
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogDebug("AdvancedDataGrid unloaded");
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
                _logger?.LogDebug("Disposing AdvancedDataGridControl...");

                // ✅ OPRAVENÉ: Správne cleanup všetkých zdrojov
                _ = ClearAllAsync();

                if (_viewModel != null)
                {
                    UnsubscribeFromViewModel(_viewModel);
                    _viewModel.Dispose();
                    _viewModel = null;
                }

                _disposed = true;
                _logger?.LogInformation("AdvancedDataGridControl disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during disposal");
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}