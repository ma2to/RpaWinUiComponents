//Views/AdvancedDataGridControl.xaml.cs - OPRAVA: Vyriešené všetky CS chyby
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

// LOKÁLNE ALIASY pre zamedzenie CS0104 chýb
using LocalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
using LocalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
using LocalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Views
{
    /// <summary>
    /// Hlavný UserControl pre AdvancedWinUiDataGrid komponent - OPRAVA: Všetky CS chyby vyriešené
    /// </summary>
    public sealed partial class AdvancedDataGridControl : UserControl, IDisposable
    {
        private AdvancedDataGridViewModel? _viewModel;
        private readonly ILogger<AdvancedDataGridControl> _logger;
        private bool _disposed = false;
        private bool _isKeyboardShortcutsVisible = false;
        private bool _isInitialized = false;

        // UI tracking - OPRAVA: Správne typy
        private readonly Dictionary<DataGridRow, Border> _rowElements = new();
        private readonly List<Border> _headerElements = new();

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

                    // OPRAVA: Spustiť UI update keď sa nastaví ViewModel
                    _ = UpdateUIAsync();
                }

                this.DataContext = _viewModel;
                OnPropertyChanged(nameof(ViewModel));
            }
        }

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region OPRAVA: Dynamické UI generovanie s opraveným kódom

        /// <summary>
        /// OPRAVA: Aktualizuje celé UI na základe ViewModel dát
        /// </summary>
        private async Task UpdateUIAsync()
        {
            if (_disposed || _viewModel == null) return;

            try
            {
                await Task.Run(() =>
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        try
                        {
                            UpdateHeadersUI();
                            UpdateRowsUI();
                            _logger.LogDebug("✅ UI updated successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Error updating UI");
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in UpdateUIAsync");
            }
        }

        /// <summary>
        /// OPRAVA: Aktualizuje headers na základe Columns z ViewModel
        /// </summary>
        private void UpdateHeadersUI()
        {
            try
            {
                var headersGrid = this.FindName("HeadersGrid") as Grid;
                if (headersGrid == null || _viewModel?.Columns == null) return;

                // Vyčistiť existujúce headers
                headersGrid.Children.Clear();
                headersGrid.ColumnDefinitions.Clear();
                _headerElements.Clear();

                _logger.LogDebug("🔄 Updating headers UI with {Count} columns", _viewModel.Columns.Count);

                // Vytvoriť column definitions
                foreach (var column in _viewModel.Columns)
                {
                    var colDef = new Microsoft.UI.Xaml.Controls.ColumnDefinition
                    {
                        Width = new GridLength(column.Width),
                        MinWidth = column.MinWidth,
                        MaxWidth = column.MaxWidth
                    };
                    headersGrid.ColumnDefinitions.Add(colDef);
                }

                // Vytvoriť header elements
                for (int i = 0; i < _viewModel.Columns.Count; i++)
                {
                    var column = _viewModel.Columns[i];
                    var headerElement = CreateHeaderElement(column, i);

                    Grid.SetColumn(headerElement, i);
                    headersGrid.Children.Add(headerElement);
                    _headerElements.Add(headerElement);
                }

                _logger.LogDebug("✅ Headers UI updated with {Count} elements", _headerElements.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating headers UI");
            }
        }

        /// <summary>
        /// OPRAVA: Vytvorí header element
        /// </summary>
        private Border CreateHeaderElement(LocalColumnDefinition column, int columnIndex)
        {
            var headerBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(0, 0, 1, 0),
                Padding = new Thickness(8, 6, 8, 6), // OPRAVA: 4 parametre
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            var headerText = new TextBlock
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
                ToolTipService.SetToolTip(headerBorder, column.ToolTip);
            }

            headerBorder.Child = headerText;

            return headerBorder;
        }

        /// <summary>
        /// OPRAVA: Aktualizuje riadky na základe Rows z ViewModel
        /// </summary>
        private void UpdateRowsUI()
        {
            try
            {
                var dataRowsContainer = this.FindName("DataRowsContainer") as StackPanel;
                if (dataRowsContainer == null || _viewModel?.Rows == null || _viewModel?.Columns == null) return;

                // Vyčistiť existujúce riadky
                dataRowsContainer.Children.Clear();
                _rowElements.Clear();

                _logger.LogDebug("🔄 Updating rows UI with {Count} rows", _viewModel.Rows.Count);

                // Limit pre výkon - zobraz max 50 riadkov
                var rowsToShow = Math.Min(50, _viewModel.Rows.Count);

                for (int i = 0; i < rowsToShow; i++)
                {
                    var row = _viewModel.Rows[i];
                    row.IsEvenRow = i % 2 == 0;

                    var rowElement = CreateRowElement(row, i);
                    dataRowsContainer.Children.Add(rowElement);
                    _rowElements[row] = rowElement; // OPRAVA: Správny typ Border
                }

                _logger.LogDebug("✅ Rows UI updated with {Count} visible rows", rowsToShow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating rows UI");
            }
        }

        /// <summary>
        /// OPRAVA: Vytvorí row element - vracia Border nie StackPanel
        /// </summary>
        private Border CreateRowElement(DataGridRow row, int rowIndex)
        {
            var rowContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                MinHeight = 32
            };

            // Add border wrapper - OPRAVA: Správne typy
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(1, 0, 1, 1), // OPRAVA: 4 parametre
                Background = row.IsEvenRow
                    ? new SolidColorBrush(Microsoft.UI.Colors.LightGray) { Opacity = 0.1 }
                    : new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                Child = rowContainer
            };

            // Vytvor bunky pre každý stĺpec
            for (int colIndex = 0; colIndex < _viewModel!.Columns.Count; colIndex++)
            {
                var column = _viewModel.Columns[colIndex];
                var cell = row.GetCell(column.Name);

                if (cell != null)
                {
                    var cellElement = CreateCellElement(cell, column, colIndex);
                    rowContainer.Children.Add(cellElement);
                }
            }

            return border; // OPRAVA: Return Border nie StackPanel
        }

        /// <summary>
        /// OPRAVA: Vytvorí cell element s opraveným Thickness
        /// </summary>
        private Border CreateCellElement(DataGridCell cell, LocalColumnDefinition column, int columnIndex)
        {
            var cellBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                BorderThickness = new Thickness(0, 0, 1, 0), // OPRAVA: 4 parametre
                Padding = new Thickness(8, 4, 8, 4), // OPRAVA: 4 parametre
                Width = column.Width,
                MinWidth = column.MinWidth,
                Background = cell.HasValidationError
                    ? new SolidColorBrush(Microsoft.UI.Colors.MistyRose)
                    : new SolidColorBrush(Microsoft.UI.Colors.Transparent)
            };

            FrameworkElement cellContent;

            // Rôzne typy buniek
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
        /// OPRAVA: Vytvorí delete button
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
                BorderThickness = new Thickness(0), // OPRAVA: 1 parameter
                CornerRadius = new CornerRadius(3),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            deleteButton.Click += (s, e) =>
            {
                try
                {
                    // Nájdi riadok a vymaž jeho dáta
                    var row = _viewModel?.Rows?.FirstOrDefault(r => r.Cells.ContainsValue(cell));
                    if (row != null)
                    {
                        foreach (var c in row.Cells.Values.Where(c => !IsSpecialColumn(c.ColumnName)))
                        {
                            c.Value = null;
                            c.ClearValidationErrors();
                        }

                        _ = UpdateUIAsync();
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
        /// OPRAVA: Vytvorí validation alerts
        /// </summary>
        private TextBlock CreateValidationAlerts(DataGridCell cell)
        {
            return new TextBlock
            {
                Text = cell.ValidationErrorsText,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 10,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkRed),
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = cell.HasValidationError ? Visibility.Visible : Visibility.Collapsed
            };
        }

        /// <summary>
        /// OPRAVA: Vytvorí editable cell s opraveným Padding
        /// </summary>
        private TextBox CreateEditableCell(DataGridCell cell, LocalColumnDefinition column)
        {
            var textBox = new TextBox
            {
                Text = cell.Value?.ToString() ?? "",
                BorderThickness = new Thickness(0), // OPRAVA: 1 parameter
                Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
                VerticalAlignment = VerticalAlignment.Center,
                IsReadOnly = cell.IsReadOnly || column.IsReadOnly,
                FontSize = 12,
                Padding = new Thickness(4, 2, 4, 2) // OPRAVA: 4 parametre
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

        private static bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        #endregion

        #region ViewModel Event Handling

        /// <summary>
        /// Prihlásenie na ViewModel events
        /// </summary>
        private void SubscribeToViewModel(AdvancedDataGridViewModel viewModel)
        {
            try
            {
                viewModel.ErrorOccurred += OnViewModelError;
                viewModel.PropertyChanged += OnViewModelPropertyChanged;

                // Subscribe na zmeny v kolekciách
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

        /// <summary>
        /// Odhlásenie z ViewModel events
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
            if (!_disposed && _isInitialized)
            {
                _logger.LogDebug("🔄 Columns changed, updating UI");
                _ = UpdateUIAsync();
            }
        }

        private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_disposed && _isInitialized)
            {
                _logger.LogDebug("🔄 Rows changed, updating UI");
                _ = UpdateUIAsync();
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

                // OPRAVA: Force UI update po inicializácii
                await UpdateUIAsync();

                _logger.LogInformation("✅ AdvancedDataGrid initialized successfully");

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

                // OPRAVA: Force UI update po načítaní dát
                await UpdateUIAsync();

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

        // Zvyšok API metód zostáva rovnaký...
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

                    await UpdateUIAsync();
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

                    await UpdateUIAsync();
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

                // Clear UI
                var headersGrid = this.FindName("HeadersGrid") as Grid;
                var dataRowsContainer = this.FindName("DataRowsContainer") as StackPanel;

                headersGrid?.Children.Clear();
                headersGrid?.ColumnDefinitions.Clear();
                dataRowsContainer?.Children.Clear();

                _rowElements.Clear();
                _headerElements.Clear();
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

        #region Event Handlers

        public void OnToggleKeyboardShortcuts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                // Clear UI elements
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