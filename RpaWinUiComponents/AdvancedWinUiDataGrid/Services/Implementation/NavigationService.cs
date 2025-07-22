// SÚBOR: Services/Implementation/NavigationService.cs - OPRAVENÉ
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Implementation
{
    /// <summary>
    /// ✅ OPRAVA CS0738, CS0051: INTERNAL class s internal parameters
    /// </summary>
    internal class NavigationService : INavigationService
    {
        private readonly ILogger<NavigationService> _logger;
        private List<DataGridRow> _rows = new();
        private List<ColumnDefinition> _columns = new();
        private int _currentRowIndex = -1;
        private int _currentColumnIndex = -1;
        private DataGridCell? _currentCell;

        // ✅ OPRAVA CS7025, CS0053: internal event type a property type
        public event EventHandler<CellNavigationEventArgs>? CellChanged;
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        public NavigationService(ILogger<NavigationService> logger)
        {
            _logger = logger;
        }

        // ✅ OPRAVA CS0053: internal property type
        public DataGridCell? CurrentCell
        {
            get => _currentCell;
            private set
            {
                if (_currentCell != value)
                {
                    _currentCell = value;
                    _logger.LogTrace("Current cell changed to: {ColumnName}[{RowIndex},{ColumnIndex}]",
                        _currentCell?.ColumnName, _currentCell?.RowIndex, _currentCell?.ColumnIndex);
                }
            }
        }

        public int CurrentRowIndex => _currentRowIndex;
        public int CurrentColumnIndex => _currentColumnIndex;

        // ✅ OPRAVA CS0051: internal parameters
        public void Initialize(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns)
        {
            try
            {
                _rows = rows?.ToList() ?? throw new ArgumentNullException(nameof(rows));
                _columns = columns?.ToList() ?? throw new ArgumentNullException(nameof(columns));

                _currentRowIndex = -1;
                _currentColumnIndex = -1;
                CurrentCell = null;

                _logger.LogInformation("NavigationService initialized with {RowCount} rows and {ColumnCount} columns",
                    _rows.Count, _columns.Count);

                // Move to first editable cell if available
                if (_rows.Count > 0)
                {
                    var editableColumns = GetEditableColumns();
                    if (editableColumns.Count > 0)
                    {
                        MoveToCell(0, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing NavigationService");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Initialize"));
                throw;
            }
        }

        public void MoveToNextCell()
        {
            try
            {
                var editableColumns = GetEditableColumns();
                if (editableColumns.Count == 0 || _rows.Count == 0)
                {
                    _logger.LogDebug("No editable columns or rows available for navigation");
                    return;
                }

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                var nextColumnIndex = _currentColumnIndex + 1;
                var nextRowIndex = _currentRowIndex;

                // Move to next column, or wrap to next row
                if (nextColumnIndex >= editableColumns.Count)
                {
                    nextColumnIndex = 0;
                    nextRowIndex = _currentRowIndex + 1;
                    if (nextRowIndex >= _rows.Count)
                        nextRowIndex = 0; // Wrap to beginning
                }

                MoveToCell(nextRowIndex, nextColumnIndex);

                _logger.LogDebug("Moved to next cell: [{Row},{Col}]", nextRowIndex, nextColumnIndex);

                // ✅ OPRAVA CS0051: internal parameter type
                OnCellChanged(new CellNavigationEventArgs
                {
                    OldRowIndex = oldRowIndex,
                    OldColumnIndex = oldColumnIndex,
                    NewRowIndex = _currentRowIndex,
                    NewColumnIndex = _currentColumnIndex,
                    OldCell = oldCell,
                    NewCell = CurrentCell,
                    Direction = NavigationDirection.Next
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to next cell");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToNextCell"));
            }
        }

        public void MoveToPreviousCell()
        {
            try
            {
                var editableColumns = GetEditableColumns();
                if (editableColumns.Count == 0 || _rows.Count == 0)
                {
                    _logger.LogDebug("No editable columns or rows available for navigation");
                    return;
                }

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                var prevColumnIndex = _currentColumnIndex - 1;
                var prevRowIndex = _currentRowIndex;

                // Move to previous column, or wrap to previous row
                if (prevColumnIndex < 0)
                {
                    prevColumnIndex = editableColumns.Count - 1;
                    prevRowIndex = _currentRowIndex - 1;
                    if (prevRowIndex < 0)
                        prevRowIndex = _rows.Count - 1; // Wrap to end
                }

                MoveToCell(prevRowIndex, prevColumnIndex);

                _logger.LogDebug("Moved to previous cell: [{Row},{Col}]", prevRowIndex, prevColumnIndex);

                OnCellChanged(new CellNavigationEventArgs
                {
                    OldRowIndex = oldRowIndex,
                    OldColumnIndex = oldColumnIndex,
                    NewRowIndex = _currentRowIndex,
                    NewColumnIndex = _currentColumnIndex,
                    OldCell = oldCell,
                    NewCell = CurrentCell,
                    Direction = NavigationDirection.Previous
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to previous cell");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToPreviousCell"));
            }
        }

        public void MoveToNextRow()
        {
            try
            {
                if (_rows.Count == 0)
                {
                    _logger.LogDebug("No rows available for navigation");
                    return;
                }

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                var nextRowIndex = (_currentRowIndex + 1) % _rows.Count;
                MoveToCell(nextRowIndex, _currentColumnIndex);

                _logger.LogDebug("Moved to next row: {Row}", nextRowIndex);

                OnCellChanged(new CellNavigationEventArgs
                {
                    OldRowIndex = oldRowIndex,
                    OldColumnIndex = oldColumnIndex,
                    NewRowIndex = _currentRowIndex,
                    NewColumnIndex = _currentColumnIndex,
                    OldCell = oldCell,
                    NewCell = CurrentCell,
                    Direction = NavigationDirection.Down
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to next row");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToNextRow"));
            }
        }

        public void MoveToPreviousRow()
        {
            try
            {
                if (_rows.Count == 0)
                {
                    _logger.LogDebug("No rows available for navigation");
                    return;
                }

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                var prevRowIndex = _currentRowIndex - 1;
                if (prevRowIndex < 0)
                    prevRowIndex = _rows.Count - 1;

                MoveToCell(prevRowIndex, _currentColumnIndex);

                _logger.LogDebug("Moved to previous row: {Row}", prevRowIndex);

                OnCellChanged(new CellNavigationEventArgs
                {
                    OldRowIndex = oldRowIndex,
                    OldColumnIndex = oldColumnIndex,
                    NewRowIndex = _currentRowIndex,
                    NewColumnIndex = _currentColumnIndex,
                    OldCell = oldCell,
                    NewCell = CurrentCell,
                    Direction = NavigationDirection.Up
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to previous row");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToPreviousRow"));
            }
        }

        public void MoveToCell(int rowIndex, int columnIndex)
        {
            try
            {
                // Validate row index
                if (rowIndex < 0 || rowIndex >= _rows.Count)
                {
                    _logger.LogWarning("Invalid row index: {RowIndex}, available rows: {RowCount}", rowIndex, _rows.Count);
                    return;
                }

                var editableColumns = GetEditableColumns();

                // Validate column index
                if (columnIndex < 0 || columnIndex >= editableColumns.Count)
                {
                    _logger.LogWarning("Invalid column index: {ColumnIndex}, available editable columns: {ColumnCount}",
                        columnIndex, editableColumns.Count);
                    return;
                }

                var oldRowIndex = _currentRowIndex;
                var oldColumnIndex = _currentColumnIndex;
                var oldCell = CurrentCell;

                _currentRowIndex = rowIndex;
                _currentColumnIndex = columnIndex;

                var columnName = editableColumns[columnIndex].Name;
                CurrentCell = _rows[rowIndex].GetCell(columnName);

                _logger.LogTrace("Moved to cell: [{Row},{Col}] = {ColumnName}", rowIndex, columnIndex, columnName);

                // Only fire event if position actually changed
                if (oldRowIndex != _currentRowIndex || oldColumnIndex != _currentColumnIndex)
                {
                    OnCellChanged(new CellNavigationEventArgs
                    {
                        OldRowIndex = oldRowIndex,
                        OldColumnIndex = oldColumnIndex,
                        NewRowIndex = _currentRowIndex,
                        NewColumnIndex = _currentColumnIndex,
                        OldCell = oldCell,
                        NewCell = CurrentCell,
                        Direction = NavigationDirection.None
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving to cell [{Row},{Col}]", rowIndex, columnIndex);
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "MoveToCell"));
            }
        }

        /// <summary>
        /// Získa zoznam editovateľných stĺpcov (bez špeciálnych stĺpcov)
        /// </summary>
        private List<ColumnDefinition> GetEditableColumns()
        {
            return _columns.Where(c => !IsSpecialColumn(c.Name)).ToList();
        }

        /// <summary>
        /// Kontroluje či je stĺpec špeciálny
        /// </summary>
        private static bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        // ✅ OPRAVA CS0051: internal parameter type
        protected virtual void OnCellChanged(CellNavigationEventArgs e)
        {
            CellChanged?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}