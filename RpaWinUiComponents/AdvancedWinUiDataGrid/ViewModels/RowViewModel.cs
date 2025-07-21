// ZLEPŠENIE 2,4,7: Row Management + Performance + Code Quality
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/ViewModels/RowViewModel.cs

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Collections;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels
{
    /// <summary>
    /// Enhanced ViewModel pre jeden riadok s performance optimizations
    /// ZLEPŠENIE 2: Proper collection management a lazy loading
    /// ZLEPŠENIE 4: Performance optimizations s virtualizáciou
    /// OPRAVA CS0122: UpdateRowStatus je teraz public
    /// </summary>
    internal class RowViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly ObservableRangeCollection<CellViewModel> _cells = new();
        private readonly Dictionary<string, CellViewModel> _cellLookup = new();
        private bool _isSelected;
        private bool _isEvenRow;
        private bool _isEmpty = true;
        private bool _hasValidationErrors;
        private bool _isVisible = true; // Pre virtualizáciu
        private bool _disposed;
        private readonly object _cellsLock = new();

        public RowViewModel(int rowIndex)
        {
            RowIndex = rowIndex;
            RowId = Guid.NewGuid().ToString();
            IsEvenRow = rowIndex % 2 == 0;
        }

        #region Properties

        public int RowIndex { get; }
        public string RowId { get; }

        /// <summary>
        /// Observable kolekcia buniek pre UI binding
        /// </summary>
        public ObservableRangeCollection<CellViewModel> Cells => _cells;

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsEvenRow
        {
            get => _isEvenRow;
            set => SetProperty(ref _isEvenRow, value);
        }

        public bool IsEmpty
        {
            get => _isEmpty;
            set => SetProperty(ref _isEmpty, value);
        }

        public bool HasValidationErrors
        {
            get => _hasValidationErrors;
            set => SetProperty(ref _hasValidationErrors, value);
        }

        /// <summary>
        /// Pre UI virtualizáciu (ZLEPŠENIE 4: Performance)
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        /// <summary>
        /// Počet validačných chýb v riadku
        /// </summary>
        public int ValidationErrorCount => _cells.Count(c => c.HasValidationErrors && !IsSpecialColumn(c.ColumnName));

        /// <summary>
        /// Text všetkých validačných chýb v riadku
        /// </summary>
        public string ValidationErrorsText
        {
            get
            {
                var errors = new List<string>();
                foreach (var cell in _cells.Where(c => c.HasValidationErrors && !IsSpecialColumn(c.ColumnName)))
                {
                    errors.Add($"{cell.ColumnName}: {cell.ValidationErrorsText}");
                }
                return string.Join("; ", errors);
            }
        }

        #endregion

        #region Cell Management (ZLEPŠENIE 4: Performance Optimized)

        /// <summary>
        /// Pridá bunku s performance optimalizáciou
        /// </summary>
        public void AddCell(CellViewModel cell)
        {
            if (cell == null) throw new ArgumentNullException(nameof(cell));

            lock (_cellsLock)
            {
                if (_cellLookup.ContainsKey(cell.ColumnName))
                {
                    RemoveCell(cell.ColumnName);
                }

                _cellLookup[cell.ColumnName] = cell;
                _cells.Add(cell);

                // Subscribe to cell changes
                cell.PropertyChanged += OnCellPropertyChanged;
                cell.ValueChanged += OnCellValueChanged;
            }

            UpdateRowStatus();
        }

        /// <summary>
        /// Odstráni bunku
        /// </summary>
        public void RemoveCell(string columnName)
        {
            lock (_cellsLock)
            {
                if (_cellLookup.TryGetValue(columnName, out var cell))
                {
                    // Unsubscribe events
                    cell.PropertyChanged -= OnCellPropertyChanged;
                    cell.ValueChanged -= OnCellValueChanged;

                    _cellLookup.Remove(columnName);
                    _cells.Remove(cell);

                    cell.Dispose();
                }
            }

            UpdateRowStatus();
        }

        /// <summary>
        /// Získa bunku podľa názvu stĺpca - O(1) lookup
        /// </summary>
        public CellViewModel? GetCell(string columnName)
        {
            lock (_cellsLock)
            {
                return _cellLookup.TryGetValue(columnName, out var cell) ? cell : null;
            }
        }

        /// <summary>
        /// Získa typovanú hodnotu z bunky
        /// </summary>
        public T GetValue<T>(string columnName)
        {
            var cell = GetCell(columnName);
            return cell != null ? cell.GetValue<T>() : default(T)!;
        }

        /// <summary>
        /// Nastaví hodnotu bunky
        /// </summary>
        public void SetValue(string columnName, object? value)
        {
            var cell = GetCell(columnName);
            if (cell != null)
            {
                cell.Value = value;
            }
        }

        /// <summary>
        /// Nastaví hodnotu ticho (pre bulk loading)
        /// </summary>
        public void SetValueSilently(string columnName, object? value)
        {
            var cell = GetCell(columnName);
            if (cell != null)
            {
                cell.SetValueSilently(value);
            }
        }

        #endregion

        #region Row Operations

        /// <summary>
        /// Vyčistí všetky hodnoty (okrem špeciálnych stĺpcov)
        /// </summary>
        public void ClearValues()
        {
            lock (_cellsLock)
            {
                foreach (var cell in _cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                {
                    cell.Value = null;
                    cell.ClearValidationErrors();
                }
            }
            UpdateRowStatus();
        }

        /// <summary>
        /// Začne editáciu konkrétnej bunky
        /// </summary>
        public void StartCellEditing(string columnName)
        {
            var targetCell = GetCell(columnName);
            if (targetCell?.IsReadOnly == false)
            {
                // Ukončí editáciu ostatných buniek v riadku
                lock (_cellsLock)
                {
                    foreach (var cell in _cells.Where(c => c != targetCell && c.IsEditing))
                    {
                        cell.CommitChanges();
                    }
                }

                targetCell.StartEditing();
            }
        }

        /// <summary>
        /// Ukončí editáciu všetkých buniek v riadku
        /// </summary>
        public void CommitAllChanges()
        {
            lock (_cellsLock)
            {
                foreach (var cell in _cells.Where(c => c.IsEditing))
                {
                    cell.CommitChanges();
                }
            }
        }

        /// <summary>
        /// Zruší editáciu všetkých buniek v riadku
        /// </summary>
        public void CancelAllEditing()
        {
            lock (_cellsLock)
            {
                foreach (var cell in _cells.Where(c => c.IsEditing))
                {
                    cell.CancelEditing();
                }
            }
        }

        #endregion

        #region Batch Operations (ZLEPŠENIE 4: Performance)

        /// <summary>
        /// Bulk aktualizácia hodnôt (performance optimized)
        /// </summary>
        public void UpdateValues(Dictionary<string, object?> values, bool silent = false)
        {
            if (values == null) return;

            // Dočasne vypneme notifikácie
            var cellsToUpdate = new List<(CellViewModel cell, object? value)>();

            lock (_cellsLock)
            {
                foreach (var kvp in values)
                {
                    var cell = GetCell(kvp.Key);
                    if (cell != null)
                    {
                        cellsToUpdate.Add((cell, kvp.Value));
                    }
                }
            }

            // Aktualizuj všetky bunky naraz
            foreach (var (cell, value) in cellsToUpdate)
            {
                if (silent)
                    cell.SetValueSilently(value);
                else
                    cell.Value = value;
            }

            UpdateRowStatus();
        }

        /// <summary>
        /// Export riadku ako dictionary (bez špeciálnych stĺpcov)
        /// </summary>
        public Dictionary<string, object?> ExportToDictionary()
        {
            var result = new Dictionary<string, object?>();

            lock (_cellsLock)
            {
                foreach (var cell in _cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                {
                    result[cell.ColumnName] = cell.Value;
                }
            }

            return result;
        }

        #endregion

        #region Validation Management (ZLEPŠENIE 3: Proper Validation)

        /// <summary>
        /// Async validácia celého riadku s performance optimalizáciou
        /// </summary>
        public async Task<bool> ValidateAsync(CancellationToken cancellationToken = default)
        {
            if (IsEmpty) return true;

            var validationTasks = new List<Task<bool>>();

            lock (_cellsLock)
            {
                foreach (var cell in _cells.Where(c => !IsSpecialColumn(c.ColumnName)))
                {
                    // Pre performance môžeme validovať len zmenené bunky
                    if (cell.Value != cell.OriginalValue)
                    {
                        validationTasks.Add(ValidateCellAsync(cell, cancellationToken));
                    }
                }
            }

            if (validationTasks.Count == 0) return !HasValidationErrors;

            var results = await Task.WhenAll(validationTasks);
            var isValid = results.All(r => r);

            UpdateValidationStatus();
            return isValid;
        }

        private async Task<bool> ValidateCellAsync(CellViewModel cell, CancellationToken cancellationToken)
        {
            // Tu by sa volal ValidationService cez parent ViewModel
            // Placeholder implementácia
            await Task.Delay(1, cancellationToken);
            return !cell.HasValidationErrors;
        }

        /// <summary>
        /// Aktualizuje validačný stav celého riadku
        /// </summary>
        public void UpdateValidationStatus()
        {
            var hasErrors = _cells.Any(c => c.HasValidationErrors && !IsSpecialColumn(c.ColumnName));
            HasValidationErrors = hasErrors;

            // Aktualizuj ValidAlerts stĺpec
            var validAlertsCell = GetCell("ValidAlerts");
            if (validAlertsCell != null)
            {
                validAlertsCell.SetValueSilently(ValidationErrorsText);
            }

            OnPropertyChanged(nameof(ValidationErrorsText));
            OnPropertyChanged(nameof(ValidationErrorCount));
        }

        #endregion

        #region Event Handlers

        private void OnCellPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_disposed) return;

            // Reaguj na zmeny v bunkách
            if (e.PropertyName == nameof(CellViewModel.HasValidationErrors))
            {
                UpdateValidationStatus();
            }
            else if (e.PropertyName == nameof(CellViewModel.Value))
            {
                UpdateRowStatus();
            }
        }

        private void OnCellValueChanged(object? sender, object? newValue)
        {
            if (_disposed) return;
            UpdateRowStatus();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// OPRAVA CS0122: UpdateRowStatus je teraz PUBLIC
        /// </summary>
        public void UpdateRowStatus()
        {
            try
            {
                lock (_cellsLock)
                {
                    var dataCells = _cells.Where(c => !IsSpecialColumn(c.ColumnName));
                    IsEmpty = dataCells.All(c => c.IsEmpty);
                }
            }
            catch
            {
                // Ignore errors during status update
            }
        }

        private static bool IsSpecialColumn(string columnName)
        {
            return columnName == "DeleteAction" || columnName == "ValidAlerts";
        }

        #endregion

        #region IDisposable (ZLEPŠENIE 1: Memory Management)

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                lock (_cellsLock)
                {
                    // Unsubscribe a dispose všetkých buniek
                    foreach (var cell in _cells)
                    {
                        cell.PropertyChanged -= OnCellPropertyChanged;
                        cell.ValueChanged -= OnCellValueChanged;
                        cell.Dispose();
                    }

                    _cells.Clear();
                    _cellLookup.Clear();
                }

                PropertyChanged = null;
                _disposed = true;
            }
            catch
            {
                // Suppress disposal errors
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public override string ToString()
        {
            return $"Row {RowIndex}: {_cells.Count} cells, {ValidationErrorCount} errors, Empty: {IsEmpty}";
        }
    }
}