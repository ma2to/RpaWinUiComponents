//Views/AdvancedDataGridControl.xaml.cs - KOMPLETNÁ OPRAVA CS1061 InitializeComponent
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Configuration;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Commands;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Helpers;

// KĽÚČOVÁ OPRAVA CS1061: V internal views používame iba INTERNAL typy
using InternalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition;
using InternalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.ValidationRule;
using InternalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Views
{
    /// <summary>
    /// KOMPLETNÁ OPRAVA CS1061 InitializeComponent - UserControl s XAML
    /// </summary>
    public sealed partial class AdvancedDataGridControl : UserControl, IDisposable
    {
        private AdvancedDataGridViewModel? _viewModel;
        private readonly ILogger<AdvancedDataGridControl> _logger;
        private bool _disposed = false;
        private bool _isKeyboardShortcutsVisible = false;
        private bool _isInitialized = false;

        // UI tracking
        private readonly Dictionary<DataGridRow, StackPanel> _rowElements = new();
        private readonly List<Border> _headerElements = new();

        public AdvancedDataGridControl()
        {
            // ✅ KĽÚČOVÁ OPRAVA CS1061: InitializeComponent je dostupný cez partial class
            // XAML compiler generuje túto metódu automaticky
            try
            {
                this.InitializeComponent();
                System.Diagnostics.Debug.WriteLine("✅ InitializeComponent() úspešne zavolaný");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ InitializeComponent() error: {ex.Message}");
                // Fallback pre prípad problému s XAML
                this.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
            }

            var loggerProvider = GetLoggerProvider();
            _logger = loggerProvider.CreateLogger<AdvancedDataGridControl>();

            this.Loaded += OnControlLoaded;
            this.Unloaded += OnControlUnloaded;

            _logger.LogDebug("AdvancedDataGridControl created");
        }

        #region Properties and Events

        /// <summary>
        /// Public property ViewModel - POTREBNÉ PRE XAML BINDING
        /// </summary>
        public AdvancedDataGridViewModel? ViewModel
        {
            get => _viewModel;
            set
            {
                if (_viewModel != null)
                {
                    UnsubscribeFromViewModel(_viewModel);
                }

                _viewModel = value;

                if (_viewModel != null)
                {
                    SubscribeToViewModel(_viewModel);
                    _isKeyboardShortcutsVisible = _viewModel.IsKeyboardShortcutsVisible;
                    UpdateKeyboardShortcutsVisibility();
                }

                this.DataContext = _viewModel;
                OnPropertyChanged(nameof(ViewModel));
            }
        }

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region UI Event Handlers

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            if (_disposed) return;

            try
            {
                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    ViewModel = _viewModel;
                }

                SetupEventHandlers();
                UpdateKeyboardShortcutsVisibility();

                _logger.LogDebug("AdvancedDataGrid loaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OnLoaded");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnLoaded"));
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            if (_disposed) return;

            try
            {
                UnsubscribeAllEvents();
                _logger.LogDebug("AdvancedDataGrid unloaded");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during OnUnloaded");
            }
        }

        #endregion

        #region KOMPLETNÁ OPRAVA UI GENEROVANIA

        /// <summary>
        /// KĽÚČOVÁ OPRAVA: Manuálne vytvorenie UI elementov
        /// </summary>
        private async Task UpdateUIManuallyAsync()
        {
            if (_disposed || _viewModel == null || _viewModel.Columns == null || _viewModel.Rows == null)
                return;

            try
            {
                await Task.Run(() =>
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            _logger.LogDebug("🔄 Manuálne aktualizovanie UI...");

                            CreateSimpleDataGridUI();

                            _logger.LogDebug("✅ UI úspešne aktualizované");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error updating UI manually");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in UpdateUIManuallyAsync");
            }
        }

        /// <summary>
        /// KĽÚČOVÁ OPRAVA: Vytvorenie jednoduchého DataGrid UI
        /// </summary>
        private void CreateSimpleDataGridUI()
        {
            try
            {
                var container = this.FindName("DataGridContainer") as StackPanel;
                if (container == null)
                {
                    // FALLBACK: Vytvor container ak XAML zlyhal
                    container = new StackPanel();
                    this.Content = container;
                    _logger.LogWarning("❌ DataGridContainer not found in XAML, created fallback");
                }

                // Vyčistiť existujúci obsah
                container.Children.Clear();

                _logger.LogDebug($"📊 Vytváram UI pre {_viewModel!.Columns.Count} stĺpcov a {_viewModel.Rows.Count} riadkov");

                // Vytvorenie headerov
                var headersPanel = CreateHeadersPanel();
                container.Children.Add(headersPanel);

                // Vytvorenie dátových riadkov (iba prvých 20 pre výkon)
                var visibleRows = Math.Min(20, _viewModel.Rows.Count);
                for (int i = 0; i < visibleRows; i++)
                {
                    var rowPanel = CreateRowPanel(_viewModel.Rows[i], i);
                    container.Children.Add(rowPanel);
                }

                _logger.LogDebug($"✅ UI vytvorené: {_viewModel.Columns.Count} stĺpcov, {visibleRows} viditeľných riadkov");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating simple DataGrid UI");
            }
        }

        /// <summary>
        /// Vytvorenie panel s headermi
        /// </summary>
        private StackPanel CreateHeadersPanel()
        {
            var headersPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                Height = 40
            };

            foreach (var column in _viewModel!.Columns)
            {
                var headerBorder = new Border
                {
                    Width = column.Width,
                    MinWidth = column.MinWidth,
                    BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    BorderThickness = new Thickness(0, 0, 1, 1),
                    Padding = new Thickness(8, 6, 8, 6),
                    Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray)
                };

                var headerText = new TextBlock
                {
                    Text = column.Header ?? column.Name,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black)
                };

                headerBorder.Child = headerText;
                headersPanel.Children.Add(headerBorder);

                _logger.LogTrace($"📄 Header vytvorený: {column.Name} ({column.Width}px)");
            }

            return headersPanel;
        }

        /// <summary>
        /// Vytvorenie panelu pre jeden riadok
        /// </summary>
        private StackPanel CreateRowPanel(DataGridRow row, int rowIndex)
        {
            var rowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Background = rowIndex % 2 == 0
                    ? new SolidColorBrush(Microsoft.UI.Colors.White)
                    : new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.3 },
                MinHeight = 32
            };

            foreach (var column in _viewModel!.Columns)
            {
                var cell = row.GetCell(column.Name);
                var cellBorder = CreateCellBorder(cell, column, rowIndex);
                rowPanel.Children.Add(cellBorder);
            }

            _rowElements[row] = rowPanel;
            return rowPanel;
        }

        /// <summary>
        /// Vytvorenie border pre bunku
        /// </summary>
        private Border CreateCellBorder(DataGridCell? cell, InternalColumnDefinition column, int rowIndex)
        {
            var cellBorder = new Border
            {
                Width = column.Width,
                MinWidth = column.MinWidth,
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(0, 0, 1, 1),
                Padding = new Thickness(8, 4, 8, 4),
                Background = cell?.HasValidationError == true
                    ? new SolidColorBrush(Microsoft.UI.Colors.MistyRose)
                    : new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            FrameworkElement cellContent;

            if (column.Name == "DeleteAction")
            {
                cellContent = CreateDeleteButton(cell, rowIndex);
            }
            else if (column.Name == "ValidAlerts")
            {
                cellContent = CreateValidationText(cell);
            }
            else
            {
                cellContent = CreateEditableTextBox(cell, column);
            }

            cellBorder.Child = cellContent;
            return cellBorder;
        }

        /// <summary>
        /// Vytvorenie delete button
        /// </summary>
        private Button CreateDeleteButton(DataGridCell? cell, int rowIndex)
        {
            var deleteButton = new Button
            {
                Content = "🗑️",
                Width = 30,
                Height = 24,
                FontSize = 10,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                BorderThickness = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            deleteButton.Click += (s, e) =>
            {
                try
                {
                    if (rowIndex < _viewModel!.Rows.Count)
                    {
                        var row = _viewModel.Rows[rowIndex];
                        foreach (var c in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                        {
                            c.Value = null;
                            c.ClearValidationErrors();
                        }

                        _ = UpdateUIManuallyAsync();
                        _logger.LogDebug($"Riadok {rowIndex} vymazaný");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing row");
                }
            };

            return deleteButton;
        }

        /// <summary>
        /// Vytvorenie textu pre validačné chyby
        /// </summary>
        private TextBlock CreateValidationText(DataGridCell? cell)
        {
            return new TextBlock
            {
                Text = cell?.ValidationErrorsText ?? "",
                FontSize = 10,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red),
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        /// <summary>
        /// Vytvorenie editovateľného TextBox
        /// </summary>
        private TextBox CreateEditableTextBox(DataGridCell? cell, InternalColumnDefinition column)
        {
            var textBox = new TextBox
            {
                Text = cell?.Value?.ToString() ?? "",
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                VerticalAlignment = VerticalAlignment.Center,
                IsReadOnly = cell?.IsReadOnly == true || column.IsReadOnly,
                FontSize = 11,
                Padding = new Thickness(2)
            };

            if (cell != null)
            {
                textBox.TextChanged += (s, e) =>
                {
                    if (s is TextBox tb && !tb.IsReadOnly)
                    {
                        cell.Value = tb.Text;
                        _logger.LogTrace($"Cell hodnota zmenená: {cell.ColumnName} = {tb.Text}");
                    }
                };
            }

            return textBox;
        }

        private static bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        #endregion

        #region Public API Methods - KOMPLETNÁ OPRAVA CS1061: INTERNAL TYPY + INTERNAL API + CUSTOM ROW COUNT

        /// <summary>
        /// KOMPLETNÁ OPRAVA CS1061: Internal view používa INTERNAL API s internal typmi + CUSTOM ROW COUNT
        /// ŽIADNE KONVERZIE, priama kompatibilita
        /// </summary>
        public async Task InitializeAsync(
            List<InternalColumnDefinition> columns, // OPRAVA CS1061: internal typ
            List<InternalValidationRule>? validationRules = null, // OPRAVA CS1061: internal typ
            InternalThrottlingConfig? throttling = null, // OPRAVA CS1061: internal typ
            int initialRowCount = 15)  // OPRAVA: Default je 15 namiesto 100
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogInformation("🚀 Initializing AdvancedDataGrid with {ColumnCount} columns, {InitialRowCount} rows",
                    columns?.Count ?? 0, initialRowCount);

                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                    ViewModel = _viewModel;
                }

                // KĽÚČOVÁ OPRAVA CS1061: Volanie INTERNAL API metódy ViewModel s internal typmi + custom row count
                // Žiadne konverzie, priama kompatibilita
                await _viewModel.InitializeAsync(columns, validationRules ?? new List<InternalValidationRule>(), throttling, initialRowCount);

                _isInitialized = true;

                // UI update
                await UpdateUIManuallyAsync();

                _logger.LogInformation("✅ AdvancedDataGrid initialized successfully with {RowCount} rows", initialRowCount);

                // Debug output
                _logger.LogDebug("📊 After initialization - Columns: {ColumnCount}, Rows: {RowCount}",
                    _viewModel.Columns?.Count ?? 0, _viewModel.Rows?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error initializing AdvancedDataGrid");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
                throw;
            }
        }

        public async Task LoadDataAsync(DataTable dataTable)
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null)
                    throw new InvalidOperationException("Component must be initialized first!");

                if (!_viewModel.IsInitialized)
                    throw new InvalidOperationException("Component not properly initialized!");

                _logger.LogInformation("📊 Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);
                await _viewModel.LoadDataAsync(dataTable);

                // UI update
                await UpdateUIManuallyAsync();

                _logger.LogInformation("✅ Data loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading data from DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null)
                    throw new InvalidOperationException("Component must be initialized first!");

                var dataTable = ConvertToDataTable(data);
                await LoadDataAsync(dataTable);
                _logger.LogInformation("✅ Data loaded from dictionary list successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error loading data from dictionary list");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        public async Task<DataTable> ExportToDataTableAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null)
                    return new DataTable();

                var result = await _viewModel.ExportDataAsync();
                _logger.LogInformation("Data exported to DataTable with {RowCount} rows", result.Rows.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDataTableAsync"));
                return new DataTable();
            }
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null)
                    return false;

                var result = await _viewModel.ValidateAllRowsAsync();
                _logger.LogInformation("Validation completed, all valid: {AllValid}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating all rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        public async Task ClearAllDataAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel?.ClearAllDataCommand != null && _viewModel.ClearAllDataCommand.CanExecute(null))
                {
                    if (_viewModel.ClearAllDataCommand is AsyncRelayCommand asyncCommand)
                    {
                        await asyncCommand.ExecuteAsync();
                    }
                    else
                    {
                        _viewModel.ClearAllDataCommand.Execute(null);
                        await Task.CompletedTask;
                    }

                    await UpdateUIManuallyAsync();
                    _logger.LogInformation("All data cleared");
                }
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
                if (_viewModel?.RemoveEmptyRowsCommand != null && _viewModel.RemoveEmptyRowsCommand.CanExecute(null))
                {
                    if (_viewModel.RemoveEmptyRowsCommand is AsyncRelayCommand asyncCommand)
                    {
                        await asyncCommand.ExecuteAsync();
                    }
                    else
                    {
                        _viewModel.RemoveEmptyRowsCommand.Execute(null);
                        await Task.CompletedTask;
                    }

                    await UpdateUIManuallyAsync();
                    _logger.LogInformation("Empty rows removed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        #endregion

        #region ViewModel Event Handling

        private void SubscribeToViewModel(AdvancedDataGridViewModel viewModel)
        {
            try
            {
                viewModel.ErrorOccurred += OnViewModelError;
                viewModel.PropertyChanged += OnViewModelPropertyChanged;

                if (viewModel.Columns is INotifyCollectionChanged columnsCollection)
                {
                    columnsCollection.CollectionChanged += OnColumnsChanged;
                }

                if (viewModel.Rows is INotifyCollectionChanged rowsCollection)
                {
                    rowsCollection.CollectionChanged += OnRowsChanged;
                }

                _logger.LogDebug("✅ Subscribed to ViewModel events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error subscribing to ViewModel");
            }
        }

        private void UnsubscribeFromViewModel(AdvancedDataGridViewModel viewModel)
        {
            try
            {
                viewModel.ErrorOccurred -= OnViewModelError;
                viewModel.PropertyChanged -= OnViewModelPropertyChanged;

                if (viewModel.Columns is INotifyCollectionChanged columnsCollection)
                {
                    columnsCollection.CollectionChanged -= OnColumnsChanged;
                }

                if (viewModel.Rows is INotifyCollectionChanged rowsCollection)
                {
                    rowsCollection.CollectionChanged -= OnRowsChanged;
                }

                _logger.LogDebug("✅ Unsubscribed from ViewModel events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error unsubscribing from ViewModel");
            }
        }

        private void OnColumnsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_disposed && _isInitialized)
            {
                _logger.LogDebug("🔄 Columns changed, updating UI");
                _ = UpdateUIManuallyAsync();
            }
        }

        private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_disposed && _isInitialized)
            {
                _logger.LogDebug("🔄 Rows changed, updating UI");
                _ = UpdateUIManuallyAsync();
            }
        }

        private void OnViewModelError(object? sender, ComponentErrorEventArgs e)
        {
            OnErrorOccurred(e);
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdvancedDataGridViewModel.IsKeyboardShortcutsVisible))
            {
                _isKeyboardShortcutsVisible = _viewModel?.IsKeyboardShortcutsVisible ?? false;
                UpdateKeyboardShortcutsVisibility();
            }
        }

        #endregion

        #region Helper Methods

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

        private void SetupEventHandlers()
        {
            try
            {
                this.KeyDown += OnMainControlKeyDown;
                _logger.LogDebug("Event handlers set up");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up event handlers");
            }
        }

        private void UpdateKeyboardShortcutsVisibility()
        {
            // Implementácia pre keyboard shortcuts visibility
            try
            {
                _logger.LogTrace("Keyboard shortcuts visibility: {IsVisible}", _isKeyboardShortcutsVisible);
                // Tu by mohla byť implementácia pre zobrazenie/skrytie keyboard shortcuts
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating keyboard shortcuts visibility");
            }
        }

        private void UnsubscribeAllEvents()
        {
            try
            {
                this.KeyDown -= OnMainControlKeyDown;
                this.Loaded -= OnControlLoaded;
                this.Unloaded -= OnControlUnloaded;

                _logger.LogDebug("All events unsubscribed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing events");
            }
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

        private void OnMainControlKeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                // Základné keyboard handling
                switch (e.Key)
                {
                    case VirtualKey.F1:
                        _isKeyboardShortcutsVisible = !_isKeyboardShortcutsVisible;
                        UpdateKeyboardShortcutsVisibility();
                        e.Handled = true;
                        break;

                    case VirtualKey.Escape:
                        // ESC handling
                        if (_viewModel != null)
                        {
                            _logger.LogDebug("ESC key pressed");
                        }
                        break;

                    case VirtualKey.F5:
                        // Refresh handling
                        if (_viewModel != null)
                        {
                            _logger.LogDebug("F5 refresh key pressed");
                            _ = UpdateUIManuallyAsync();
                        }
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling keyboard input");
            }
        }

        public void OnToggleKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isKeyboardShortcutsVisible = !_isKeyboardShortcutsVisible;
                UpdateKeyboardShortcutsVisibility();
                _logger.LogDebug("Keyboard shortcuts toggled: {IsVisible}", _isKeyboardShortcutsVisible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling keyboard shortcuts");
            }
        }

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public void Reset()
        {
            try
            {
                _logger.LogDebug("Resetting AdvancedDataGridControl");

                _isInitialized = false;
                _isKeyboardShortcutsVisible = false;

                if (_viewModel != null)
                {
                    _viewModel.Reset();
                }

                _rowElements.Clear();
                _headerElements.Clear();

                // Clear UI container
                var container = this.FindName("DataGridContainer") as StackPanel;
                container?.Children.Clear();

                _logger.LogInformation("AdvancedDataGridControl reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during reset");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Reset"));
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger?.LogDebug("Disposing AdvancedDataGridControl...");

                UnsubscribeAllEvents();

                if (_viewModel != null)
                {
                    UnsubscribeFromViewModel(_viewModel);
                    _viewModel.Dispose();
                    _viewModel = null;
                }

                _rowElements.Clear();
                _headerElements.Clear();
                this.DataContext = null;

                _disposed = true;
                _logger?.LogInformation("AdvancedDataGridControl disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during disposal");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdvancedDataGridControl));
        }

        protected void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        #endregion
    }
}