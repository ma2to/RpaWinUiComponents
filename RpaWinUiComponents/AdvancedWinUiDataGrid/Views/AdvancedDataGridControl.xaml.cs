// OPRAVENÉ AdvancedDataGridControl.xaml.cs - S funkčným UI renderingom
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

        // UI tracking
        private readonly List<InternalColumnDefinition> _columns = new();
        private readonly List<DataGridRow> _dataRows = new();

        public AdvancedDataGridControl()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔧 Inicializujem AdvancedDataGridControl...");
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("✅ InitializeComponent() úspešne zavolaný");

                // Aktualizuj status text
                UpdateStatusText("XAML úspešne načítaný");
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

        #region PUBLIC API METHODS - OPRAVENÉ!

        /// <summary>
        /// ✅ OPRAVENÁ INICIALIZÁCIA - Skutočne vytvára UI
        /// </summary>
        public async Task InitializeAsync(
            List<InternalColumnDefinition> columns,
            List<InternalValidationRule>? validationRules = null,
            InternalThrottlingConfig? throttling = null,
            int initialRowCount = 15)
        {
            try
            {
                UpdateStatusText("Inicializujem DataGrid...");
                _logger.LogInformation("🚀 Začínam inicializáciu s {ColumnCount} stĺpcami", columns?.Count ?? 0);

                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    InternalViewModel = _viewModel;
                }

                // Inicializácia ViewModel
                await _viewModel.InitializeAsync(columns ?? new List<InternalColumnDefinition>(),
                                               validationRules,
                                               throttling,
                                               initialRowCount);

                // Uloženie stĺpcov
                _columns.Clear();
                _columns.AddRange(columns ?? new List<InternalColumnDefinition>());

                // ✅ KĽÚČOVÉ: Vytvorenie UI pre stĺpce
                CreateHeaderUI();

                // ✅ KĽÚČOVÉ: Vytvorenie prázdnych riadkov
                CreateInitialRows(initialRowCount);

                _isInitialized = true;
                UpdateStatusText($"Inicializované s {_columns.Count} stĺpcami, {initialRowCount} riadkov");

                _logger.LogInformation("✅ Inicializácia dokončená úspešne");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri inicializácii");
                UpdateStatusText($"Chyba: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ✅ OPRAVENÉ NAČÍTANIE DÁT - Skutočne zobrazuje dáta
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

                UpdateStatusText($"Načítavam {data?.Count ?? 0} riadkov...");
                _logger.LogInformation("📊 Načítavam {RowCount} riadkov dát", data?.Count ?? 0);

                // Vyčistenie existujúcich dát
                ClearDataRows();

                if (data != null && data.Count > 0)
                {
                    // ✅ KĽÚČOVÉ: Vytvorenie UI pre každý riadok dát
                    for (int i = 0; i < data.Count; i++)
                    {
                        CreateDataRowUI(data[i], i);
                    }
                }

                // Pridanie prázdnych riadkov
                var emptyRowsNeeded = Math.Max(5, 15 - (data?.Count ?? 0));
                for (int i = data?.Count ?? 0; i < (data?.Count ?? 0) + emptyRowsNeeded; i++)
                {
                    CreateEmptyRowUI(i);
                }

                UpdateStatusText($"Načítané {data?.Count ?? 0} riadkov dát + {emptyRowsNeeded} prázdnych");
                _logger.LogInformation("✅ Dáta načítané úspešne");

                // Aktualizácia ViewModel ak existuje
                if (_viewModel != null)
                {
                    await _viewModel.LoadDataAsync(data ?? new List<Dictionary<string, object?>>());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chyba pri načítaní dát");
                UpdateStatusText($"Chyba: {ex.Message}");
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
                return await _viewModel.ValidateAllRowsAsync();
            }
            return true;
        }

        public async Task ClearAllDataAsync()
        {
            ClearDataRows();
            UpdateStatusText("Všetky dáta vymazané");

            if (_viewModel != null)
            {
                await _viewModel.ClearAllDataAsync();
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
            ClearDataRows();
            ClearHeaders();
            _columns.Clear();
            _isInitialized = false;
            UpdateStatusText("Reset dokončený");

            _viewModel?.Reset();
        }

        #endregion

        #region UI CREATION METHODS - NOVÉ!

        /// <summary>
        /// ✅ Vytvorí header UI pre stĺpce
        /// </summary>
        private void CreateHeaderUI()
        {
            var headerContainer = this.FindName("HeaderContainer") as StackPanel;
            if (headerContainer == null)
            {
                _logger.LogWarning("❌ HeaderContainer not found in XAML");
                return;
            }

            headerContainer.Children.Clear();

            foreach (var column in _columns)
            {
                var headerBorder = new Border
                {
                    Style = this.Resources["CellBorderStyle"] as Style,
                    MinWidth = column.MinWidth,
                    Width = column.Width,
                    Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
                };

                var headerText = new TextBlock
                {
                    Text = column.Header ?? column.Name,
                    Style = this.Resources["HeaderTextStyle"] as Style,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                headerBorder.Child = headerText;
                headerContainer.Children.Add(headerBorder);
            }

            _logger.LogDebug("✅ Header UI vytvorené pre {ColumnCount} stĺpcov", _columns.Count);
        }

        /// <summary>
        /// ✅ Vytvorí UI pre riadok s dátami
        /// </summary>
        private void CreateDataRowUI(Dictionary<string, object?> rowData, int rowIndex)
        {
            var rowsRepeater = this.FindName("RowsRepeater") as ItemsRepeater;
            if (rowsRepeater == null)
            {
                // Fallback - pridaj priamo do container
                CreateRowUIFallback(rowData, rowIndex, false);
                return;
            }

            // TODO: Implementácia cez ItemsRepeater
            CreateRowUIFallback(rowData, rowIndex, false);
        }

        /// <summary>
        /// ✅ Vytvorí UI pre prázdny riadok
        /// </summary>
        private void CreateEmptyRowUI(int rowIndex)
        {
            CreateRowUIFallback(new Dictionary<string, object?>(), rowIndex, true);
        }

        /// <summary>
        /// ✅ Fallback metóda pre vytvorenie riadku - pridáva priamo do kontajnera
        /// </summary>
        private void CreateRowUIFallback(Dictionary<string, object?> rowData, int rowIndex, bool isEmpty)
        {
            var dataGridContainer = this.FindName("DataGridContainer") as StackPanel;
            if (dataGridContainer == null)
            {
                _logger.LogWarning("❌ DataGridContainer not found!");
                return;
            }

            // Vytvor border pre riadok
            var rowBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Background = new SolidColorBrush(rowIndex % 2 == 0 ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.WhiteSmoke)
            };

            // Vytvor StackPanel pre bunky
            var rowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                MinHeight = 35
            };

            // Vytvor bunky pre každý stĺpec
            foreach (var column in _columns)
            {
                var cellBorder = new Border
                {
                    Style = this.Resources["CellBorderStyle"] as Style,
                    MinWidth = column.MinWidth,
                    Width = column.Width
                };

                var cellValue = "";
                if (!isEmpty && rowData.ContainsKey(column.Name))
                {
                    cellValue = rowData[column.Name]?.ToString() ?? "";
                }

                var cellTextBox = new TextBox
                {
                    Text = cellValue,
                    Style = this.Resources["CellTextStyle"] as Style,
                    IsReadOnly = column.IsReadOnly,
                    Tag = $"{rowIndex}_{column.Name}" // Pre identifikáciu
                };

                // Event handler pre zmeny
                cellTextBox.TextChanged += OnCellTextChanged;

                cellBorder.Child = cellTextBox;
                rowPanel.Children.Add(cellBorder);
            }

            rowBorder.Child = rowPanel;
            dataGridContainer.Children.Add(rowBorder);
        }

        /// <summary>
        /// ✅ Vytvorí začiatočné prázdne riadky
        /// </summary>
        private void CreateInitialRows(int rowCount)
        {
            for (int i = 0; i < rowCount; i++)
            {
                CreateEmptyRowUI(i);
            }
            _logger.LogDebug("✅ Vytvorených {RowCount} začiatočných riadkov", rowCount);
        }

        #endregion

        #region EVENT HANDLERS

        private void OnCellTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.Tag is string tag)
            {
                _logger.LogTrace("📝 Cell changed: {Tag} = {Value}", tag, textBox.Text);
                // TODO: Notifikácia ViewModel o zmene
            }
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatusText("Control načítaný");
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
                UpdateStatusText($"Chyba pri načítaní: {ex.Message}");
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

        #endregion

        #region HELPER METHODS

        private void ClearDataRows()
        {
            var dataGridContainer = this.FindName("DataGridContainer") as StackPanel;
            if (dataGridContainer != null)
            {
                // Odstráň všetky riadky okrem header
                var childrenToRemove = dataGridContainer.Children
                    .Skip(1) // Preskočiť header
                    .ToList();

                foreach (var child in childrenToRemove)
                {
                    dataGridContainer.Children.Remove(child);
                }
            }
            _dataRows.Clear();
        }

        private void ClearHeaders()
        {
            var headerContainer = this.FindName("HeaderContainer") as StackPanel;
            headerContainer?.Children.Clear();
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
                // Default stĺpce
                columns.Add(new InternalColumnDefinition("Stĺpec1", typeof(string)) { Header = "Stĺpec 1", Width = 150 });
                columns.Add(new InternalColumnDefinition("Stĺpec2", typeof(string)) { Header = "Stĺpec 2", Width = 150 });
            }

            await InitializeAsync(columns);
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
                Text = "⚠️ RpaWinUiComponents DataGrid - Fallback Mode\nXAML parsing zlyhal.",
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20)
            };
        }

        private void UpdateStatusText(string text)
        {
            try
            {
                var statusText = this.FindName("DebugStatusText") as TextBlock;
                if (statusText != null)
                {
                    statusText.Text = text;
                }
                _logger.LogDebug("Status: {Status}", text);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Chyba pri aktualizácii status textu");
            }
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