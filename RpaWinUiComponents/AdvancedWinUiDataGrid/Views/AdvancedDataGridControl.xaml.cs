//Views/AdvancedDataGridControl.xaml.cs - FINÁLNA OPRAVA TYPOV s CUSTOM ROW COUNT
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

// KĽÚČOVÁ OPRAVA: V internal views používame iba INTERNAL typy
using LocalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
using LocalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
using LocalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Views
{
    /// <summary>
    /// FINÁLNA OPRAVA - Internal view používa internal API bez konverzií
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
            this.InitializeComponent();

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
                    _logger.LogError("❌ DataGridContainer not found!");
                    return;
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
        private Border CreateCellBorder(DataGridCell? cell, LocalColumnDefinition column, int rowIndex)
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
        private TextBox CreateEditableTextBox(DataGridCell? cell, LocalColumnDefinition column)
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

        #region Public API Methods - FINÁLNA OPRAVA: INTERNAL TYPY + INTERNAL API + CUSTOM ROW COUNT

        /// <summary>
        /// FINÁLNA OPRAVA: Internal view používa INTERNAL API s internal typmi + CUSTOM ROW COUNT
        /// ŽIADNE KONVERZIE, priama kompatibilita
        /// </summary>
        public async Task InitializeAsync(
            List<LocalColumnDefinition> columns,
            List<LocalValidationRule>? validationRules = null,
            LocalThrottlingConfig? throttling = null,
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

                // KĽÚČOVÁ OPRAVA CS1503: Volanie INTERNAL API metódy ViewModel s internal typmi + custom row count
                // Žiadne konverzie, priama kompatibilita
                await _viewModel.InitializeAsync(columns, validationRules ?? new List<LocalValidationRule>(), throttling, initialRowCount);

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