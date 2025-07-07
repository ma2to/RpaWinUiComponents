//Views/AdvancedDataGridControl.xaml.cs - FINÁLNA OPRAVA: UI rebuild triggering
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

// LOKÁLNE ALIASY pre zamedzenie CS0104 chýb
using LocalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
using LocalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
using LocalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Views
{
    /// <summary>
    /// Hlavný UserControl pre AdvancedWinUiDataGrid komponent - FINÁLNA OPRAVA UI timing
    /// </summary>
    public sealed partial class AdvancedDataGridControl : UserControl, IDisposable
    {
        private AdvancedDataGridViewModel? _viewModel;
        private readonly ILogger<AdvancedDataGridControl> _logger;
        private bool _disposed = false;
        private bool _isKeyboardShortcutsVisible = false;

        // OPRAVA: Tracking pre dynamické UI
        private readonly Dictionary<DataGridRow, Grid> _rowGrids = new();
        private readonly List<TextBlock> _headerTextBlocks = new();
        private bool _isUIBuilding = false;
        private bool _isInitialized = false;

        public AdvancedDataGridControl()
        {
            this.InitializeComponent();

            var loggerProvider = GetLoggerProvider();
            _logger = loggerProvider.CreateLogger<AdvancedDataGridControl>();

            this.Loaded += OnLoaded;
            this.Unloaded += OnUnloaded;

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

        #region FINÁLNA OPRAVA: Kompletné UI generovanie

        /// <summary>
        /// FINÁLNA OPRAVA: Prebuduje celé DataGrid UI - s proper timing
        /// </summary>
        private async Task RebuildDataGridUIAsync()
        {
            if (_disposed || _isUIBuilding || _viewModel == null || !_isInitialized)
            {
                _logger.LogDebug("Skipping UI rebuild - disposed: {Disposed}, building: {Building}, viewModel: {HasViewModel}, initialized: {Initialized}",
                    _disposed, _isUIBuilding, _viewModel != null, _isInitialized);
                return;
            }

            try
            {
                _isUIBuilding = true;
                _logger.LogInformation("🔄 Starting DataGrid UI rebuild with {ColumnCount} columns and {RowCount} rows",
                    _viewModel.Columns?.Count ?? 0, _viewModel.Rows?.Count ?? 0);

                // Clear existing UI
                ClearDataGridUI();

                // Build complete UI
                await BuildCompleteDataGridAsync();

                _logger.LogInformation("✅ DataGrid UI rebuilt successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error rebuilding DataGrid UI");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RebuildDataGridUIAsync"));
            }
            finally
            {
                _isUIBuilding = false;
            }
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vybuduje kompletnú DataGrid v UI thread
        /// </summary>
        private async Task BuildCompleteDataGridAsync()
        {
            if (_viewModel?.Columns == null || _viewModel.Rows == null)
            {
                _logger.LogWarning("Cannot build UI - missing columns or rows");
                return;
            }

            await Task.Run(() =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        var scrollViewer = this.FindName("MainScrollViewer") as ScrollViewer;
                        if (scrollViewer == null)
                        {
                            _logger.LogError("MainScrollViewer not found!");
                            return;
                        }

                        // Vytvor kompletný DataGrid
                        var dataGridContainer = CreateCompleteDataGrid();
                        scrollViewer.Content = dataGridContainer;

                        _logger.LogDebug("✅ Complete DataGrid created with {ColumnCount} columns and {RowCount} rows",
                            _viewModel.Columns.Count, _viewModel.Rows.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error in UI thread during DataGrid build");
                    }
                });
            });
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vytvorí kompletný DataGrid container
        /// </summary>
        private Grid CreateCompleteDataGrid()
        {
            var mainGrid = new Grid { MinWidth = 800 };

            // Row definitions
            mainGrid.RowDefinitions.Add(new Microsoft.UI.Xaml.Controls.RowDefinition { Height = GridLength.Auto }); // Headers
            mainGrid.RowDefinitions.Add(new Microsoft.UI.Xaml.Controls.RowDefinition { Height = GridLength.Auto }); // Data

            // 1. Create Headers
            var headerContainer = CreateHeaderContainer();
            Grid.SetRow(headerContainer, 0);
            mainGrid.Children.Add(headerContainer);

            // 2. Create Data Rows
            var dataContainer = CreateDataContainer();
            Grid.SetRow(dataContainer, 1);
            mainGrid.Children.Add(dataContainer);

            return mainGrid;
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vytvorí header container s bordrom
        /// </summary>
        private Border CreateHeaderContainer()
        {
            var headerBorder = new Border
            {
                Background = new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.3 },
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(1, 1, 1, 0),
                Height = 40
            };

            var headerGrid = new Grid();

            // Column definitions pre headers
            foreach (var column in _viewModel!.Columns)
            {
                var colDef = new Microsoft.UI.Xaml.Controls.ColumnDefinition
                {
                    Width = new GridLength(column.Width),
                    MinWidth = column.MinWidth,
                    MaxWidth = column.MaxWidth
                };
                headerGrid.ColumnDefinitions.Add(colDef);
            }

            // Header cells
            for (int i = 0; i < _viewModel.Columns.Count; i++)
            {
                var column = _viewModel.Columns[i];
                var headerCell = CreateHeaderCell(column, i);
                Grid.SetColumn(headerCell, i);
                headerGrid.Children.Add(headerCell);
            }

            headerBorder.Child = headerGrid;
            return headerBorder;
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vytvorí jednotlivú header bunku
        /// </summary>
        private Border CreateHeaderCell(LocalColumnDefinition column, int columnIndex)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(8, 6, 8, 6),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            var textBlock = new TextBlock
            {
                Text = column.Header ?? column.Name,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            if (!string.IsNullOrEmpty(column.ToolTip))
            {
                ToolTipService.SetToolTip(border, column.ToolTip);
            }

            border.Child = textBlock;
            _headerTextBlocks.Add(textBlock);

            _logger.LogTrace("Created header for column {ColumnName} at index {Index}", column.Name, columnIndex);
            return border;
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vytvorí data container s riadkami
        /// </summary>
        private StackPanel CreateDataContainer()
        {
            var dataContainer = new StackPanel { Orientation = Orientation.Vertical };

            var visibleRows = _viewModel!.Rows.Take(50).ToList(); // Limit pre performance

            for (int rowIndex = 0; rowIndex < visibleRows.Count; rowIndex++)
            {
                var row = visibleRows[rowIndex];
                row.IsEvenRow = rowIndex % 2 == 0;

                var rowElement = CreateDataRow(row);
                dataContainer.Children.Add(rowElement);
            }

            _logger.LogDebug("Created data container with {RowCount} visible rows", visibleRows.Count);
            return dataContainer;
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vytvorí jeden data riadok
        /// </summary>
        private Border CreateDataRow(DataGridRow row)
        {
            var rowBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(1, 0, 1, 1),
                Background = row.IsEvenRow
                    ? new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.1 }
                    : new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                MinHeight = 32
            };

            var rowGrid = new Grid();

            // Column definitions pre tento riadok
            foreach (var column in _viewModel!.Columns)
            {
                var colDef = new Microsoft.UI.Xaml.Controls.ColumnDefinition
                {
                    Width = new GridLength(column.Width),
                    MinWidth = column.MinWidth,
                    MaxWidth = column.MaxWidth
                };
                rowGrid.ColumnDefinitions.Add(colDef);
            }

            // Data cells
            for (int colIndex = 0; colIndex < _viewModel.Columns.Count; colIndex++)
            {
                var column = _viewModel.Columns[colIndex];
                var cell = row.GetCell(column.Name);

                if (cell != null)
                {
                    var cellElement = CreateDataCell(cell, column);
                    Grid.SetColumn(cellElement, colIndex);
                    rowGrid.Children.Add(cellElement);
                }
                else
                {
                    _logger.LogWarning("Missing cell for column {ColumnName} in row {RowIndex}", column.Name, row.RowIndex);
                }
            }

            rowBorder.Child = rowGrid;
            _rowGrids[row] = rowGrid;

            _logger.LogTrace("Created data row {RowIndex} with {CellCount} cells", row.RowIndex, row.Cells.Count);
            return rowBorder;
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vytvorí data bunku
        /// </summary>
        private Border CreateDataCell(DataGridCell cell, LocalColumnDefinition column)
        {
            var cellBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(8, 4, 8, 4),
                Background = cell.HasValidationError
                    ? new SolidColorBrush(Microsoft.UI.Colors.MistyRose)
                    : new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            FrameworkElement cellContent;

            // Special columns
            if (column.Name == "DeleteAction")
            {
                cellContent = CreateDeleteButton(cell);
            }
            else if (column.Name == "ValidAlerts")
            {
                cellContent = CreateValidationAlerts(cell);
            }
            else
            {
                cellContent = CreateEditableCell(cell, column);
            }

            cellBorder.Child = cellContent;
            return cellBorder;
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vytvorí delete button
        /// </summary>
        private Button CreateDeleteButton(DataGridCell cell)
        {
            var deleteButton = new Button
            {
                Content = "🗑️",
                Width = 32,
                Height = 24,
                FontSize = 11,
                Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.White),
                BorderThickness = new Thickness(0, 0, 0, 0),
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            deleteButton.Click += (s, e) =>
            {
                try
                {
                    // Find row and clear its data
                    var row = _viewModel?.Rows?.FirstOrDefault(r => r.Cells.ContainsValue(cell));
                    if (row != null)
                    {
                        foreach (var c in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                        {
                            c.Value = null;
                            c.ClearValidationErrors();
                        }

                        _ = RebuildDataGridUIAsync();
                        _logger.LogDebug("Row cleared: {RowIndex}", row.RowIndex);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error clearing row");
                }
            };

            ToolTipService.SetToolTip(deleteButton, "Vymazať riadok");
            return deleteButton;
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vytvorí validation alerts
        /// </summary>
        private TextBlock CreateValidationAlerts(DataGridCell cell)
        {
            var alertText = new TextBlock
            {
                Text = cell.ValidationErrorsText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 10,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = cell.HasValidationError ? Visibility.Visible : Visibility.Collapsed
            };

            return alertText;
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vytvorí editable bunku
        /// </summary>
        private TextBox CreateEditableCell(DataGridCell cell, LocalColumnDefinition column)
        {
            var textBox = new TextBox
            {
                Text = cell.Value?.ToString() ?? "",
                BorderThickness = new Thickness(0, 0, 0, 0),
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                VerticalAlignment = VerticalAlignment.Center,
                IsReadOnly = cell.IsReadOnly || column.IsReadOnly,
                FontSize = 12,
                Padding = new Thickness(4, 2, 4, 2)
            };

            // Event handlers pre editing
            textBox.TextChanged += (s, e) =>
            {
                if (s is TextBox tb && !tb.IsReadOnly)
                {
                    cell.Value = tb.Text;
                    _logger.LogTrace("Cell value changed: {ColumnName} = {Value}", cell.ColumnName, tb.Text);
                }
            };

            textBox.GotFocus += (s, e) =>
            {
                cell.IsEditing = true;
                cell.HasFocus = true;
            };

            textBox.LostFocus += (s, e) =>
            {
                cell.IsEditing = false;
                cell.HasFocus = false;
            };

            return textBox;
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Vyčistí existujúce UI prvky
        /// </summary>
        private void ClearDataGridUI()
        {
            try
            {
                _rowGrids.Clear();
                _headerTextBlocks.Clear();
                _logger.LogTrace("DataGrid UI cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing DataGrid UI");
            }
        }

        private static bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        #endregion

        #region FINÁLNA OPRAVA: ViewModel Event Handling

        /// <summary>
        /// FINÁLNA OPRAVA: Prihlásenie na ViewModel events s proper timing
        /// </summary>
        private void SubscribeToViewModel(AdvancedDataGridViewModel viewModel)
        {
            try
            {
                viewModel.ErrorOccurred += OnViewModelError;
                viewModel.PropertyChanged += OnViewModelPropertyChanged;

                // Subscribe to collections with delayed UI rebuild
                if (viewModel.Columns != null)
                {
                    if (viewModel.Columns is INotifyCollectionChanged columnsCollection)
                    {
                        columnsCollection.CollectionChanged += OnColumnsChanged;
                    }
                }

                if (viewModel.Rows != null)
                {
                    if (viewModel.Rows is INotifyCollectionChanged rowsCollection)
                    {
                        rowsCollection.CollectionChanged += OnRowsChanged;
                    }
                }

                _logger.LogDebug("✅ Subscribed to ViewModel events");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error subscribing to ViewModel");
            }
        }

        /// <summary>
        /// FINÁLNA OPRAVA: Odhlásenie z ViewModel events
        /// </summary>
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
            if (!_disposed && !_isUIBuilding && _isInitialized)
            {
                _logger.LogDebug("🔄 Columns changed, scheduling UI rebuild");
                _ = Task.Delay(100).ContinueWith(_ => RebuildDataGridUIAsync());
            }
        }

        private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_disposed && !_isUIBuilding && _isInitialized)
            {
                _logger.LogDebug("🔄 Rows changed, scheduling UI rebuild");
                _ = Task.Delay(100).ContinueWith(_ => RebuildDataGridUIAsync());
            }
        }

        #endregion

        #region Public API Methods

        public async Task InitializeAsync(
            List<LocalColumnDefinition> columns,
            List<LocalValidationRule>? validationRules = null,
            LocalThrottlingConfig? throttling = null,
            int initialRowCount = 100)
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

                await _viewModel.InitializeAsync(columns, validationRules ?? new List<LocalValidationRule>(), throttling, initialRowCount);

                _isInitialized = true;

                // FINÁLNA OPRAVA: Force UI rebuild po inicializácii
                await Task.Delay(200); // Počkaj na ukončenie inicializácie
                await RebuildDataGridUIAsync();

                _logger.LogInformation("✅ AdvancedDataGrid initialized successfully with {InitialRowCount} rows", initialRowCount);
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
                    throw new InvalidOperationException("Component must be initialized first! Call InitializeAsync() before LoadDataAsync().");

                if (!_viewModel.IsInitialized)
                    throw new InvalidOperationException("Component not properly initialized! Call InitializeAsync() with validation rules first.");

                _logger.LogInformation("📊 Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);
                await _viewModel.LoadDataAsync(dataTable);

                // FINÁLNA OPRAVA: Force UI rebuild po načítaní dát
                await Task.Delay(300); // Počkaj na ukončenie načítavania
                await RebuildDataGridUIAsync();

                _logger.LogInformation("✅ Data loaded successfully with applied validations");
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

        // Zostávajúce metódy rovnaké...
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

                    // FINÁLNA OPRAVA: Rebuild UI po vymazaní
                    await Task.Delay(100);
                    await RebuildDataGridUIAsync();

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

                    // FINÁLNA OPRAVA: Rebuild UI po odstránení
                    await Task.Delay(100);
                    await RebuildDataGridUIAsync();

                    _logger.LogInformation("Empty rows removed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        public void Reset()
        {
            if (_disposed) return;

            try
            {
                _logger.LogInformation("Resetting AdvancedDataGrid");
                _viewModel?.Reset();

                _isKeyboardShortcutsVisible = false;
                UpdateKeyboardShortcutsVisibility();

                ClearDataGridUI();
                _isInitialized = false;

                _logger.LogInformation("AdvancedDataGrid reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting AdvancedDataGrid");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Reset"));
            }
        }

        #endregion

        #region Event Handlers - Unchanged

        public void OnDeleteRowClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.CommandParameter is DataGridRow row)
                {
                    _viewModel?.DeleteRowCommand?.Execute(row);
                    _logger.LogDebug("Delete row button clicked for row: {RowIndex}", row.RowIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling delete row click");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnDeleteRowClick"));
            }
        }

        public void OnCellEditingLostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && textBox.DataContext is DataGridCell cell)
                {
                    cell.IsEditing = false;
                    _logger.LogTrace("Cell editing ended for: {ColumnName}", cell.ColumnName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cell editing lost focus");
            }
        }

        public void OnCellEditingKeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                if (sender is TextBox textBox && textBox.DataContext is DataGridCell cell)
                {
                    switch (e.Key)
                    {
                        case VirtualKey.Enter:
                            if (!e.KeyStatus.IsMenuKeyDown)
                            {
                                cell.IsEditing = false;
                                _viewModel?.NavigationService.MoveToNextRow();
                                e.Handled = true;
                            }
                            break;
                        case VirtualKey.Escape:
                            cell.CancelEditing();
                            cell.IsEditing = false;
                            e.Handled = true;
                            break;
                        case VirtualKey.Tab:
                            cell.IsEditing = false;
                            if (e.KeyStatus.IsMenuKeyDown)
                                _viewModel?.NavigationService.MoveToPreviousCell();
                            else
                                _viewModel?.NavigationService.MoveToNextCell();
                            e.Handled = true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling cell editing key down");
            }
        }

        public void OnToggleKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogDebug("Toggle keyboard shortcuts button clicked");

                _isKeyboardShortcutsVisible = !_isKeyboardShortcutsVisible;
                UpdateKeyboardShortcutsVisibility();

                if (_viewModel != null)
                {
                    _viewModel.IsKeyboardShortcutsVisible = _isKeyboardShortcutsVisible;
                }

                _logger.LogInformation("Keyboard shortcuts visibility toggled to: {IsVisible}", _isKeyboardShortcutsVisible);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling keyboard shortcuts");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "OnToggleKeyboardShortcuts_Click"));
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
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

        private void OnUnloaded(object sender, RoutedEventArgs e)
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

        private void OnMainControlKeyDown(object sender, KeyRoutedEventArgs e)
        {
            try
            {
                var ctrlPressed = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

                if (ctrlPressed)
                {
                    switch (e.Key)
                    {
                        case VirtualKey.C:
                            if (_viewModel?.CopyCommand?.CanExecute(null) == true)
                            {
                                _viewModel.CopyCommand.Execute(null);
                                e.Handled = true;
                            }
                            break;
                        case VirtualKey.V:
                            if (_viewModel?.PasteCommand?.CanExecute(null) == true)
                            {
                                _viewModel.PasteCommand.Execute(null);
                                e.Handled = true;
                            }
                            break;
                    }
                }
                else
                {
                    switch (e.Key)
                    {
                        case VirtualKey.Tab:
                            if (e.KeyStatus.IsMenuKeyDown)
                                _viewModel?.NavigationService.MoveToPreviousCell();
                            else
                                _viewModel?.NavigationService.MoveToNextCell();
                            e.Handled = true;
                            break;
                        case VirtualKey.Enter:
                            _viewModel?.NavigationService.MoveToNextRow();
                            e.Handled = true;
                            break;
                        case VirtualKey.F2:
                            var currentCell = _viewModel?.NavigationService.CurrentCell;
                            if (currentCell != null && !currentCell.IsReadOnly)
                            {
                                currentCell.IsEditing = true;
                                e.Handled = true;
                            }
                            break;
                        case VirtualKey.Delete:
                            var cellToDelete = _viewModel?.NavigationService.CurrentCell;
                            if (cellToDelete != null && !cellToDelete.IsReadOnly)
                            {
                                cellToDelete.Value = null;
                                e.Handled = true;
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling main control key down");
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
            try
            {
                if (!this.IsLoaded)
                {
                    this.Loaded -= OnDelayedUpdate;
                    this.Loaded += OnDelayedUpdate;
                    return;
                }

                this.DispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        var keyboardPanel = this.FindName("KeyboardShortcutsPanel") as Border;
                        if (keyboardPanel != null)
                        {
                            keyboardPanel.Visibility = _isKeyboardShortcutsVisible ? Visibility.Visible : Visibility.Collapsed;
                        }

                        var toggleIcon = this.FindName("ToggleIcon") as TextBlock;
                        if (toggleIcon != null)
                        {
                            toggleIcon.Text = _isKeyboardShortcutsVisible ? "▲" : "▼";
                        }

                        var toggleButton = this.FindName("ToggleKeyboardShortcutsButton") as Button;
                        if (toggleButton != null)
                        {
                            var backgroundColor = _isKeyboardShortcutsVisible
                                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DodgerBlue)
                                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray);

                            var foregroundColor = _isKeyboardShortcutsVisible
                                ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White)
                                : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black);

                            toggleButton.Background = backgroundColor;
                            toggleButton.Foreground = foregroundColor;
                        }

                        _logger.LogDebug("Keyboard shortcuts visibility updated: {IsVisible}", _isKeyboardShortcutsVisible);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in UI thread during keyboard shortcuts update");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating keyboard shortcuts visibility");
            }
        }

        private void OnDelayedUpdate(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnDelayedUpdate;
            UpdateKeyboardShortcutsVisibility();
        }

        private void UnsubscribeAllEvents()
        {
            try
            {
                this.KeyDown -= OnMainControlKeyDown;
                this.Loaded -= OnLoaded;
                this.Loaded -= OnDelayedUpdate;
                this.Unloaded -= OnUnloaded;

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

        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
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

                ClearDataGridUI();
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