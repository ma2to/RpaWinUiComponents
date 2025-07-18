// ZLEPŠENIE 3: CellViewModel s proper INotifyDataErrorInfo pattern
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/ViewModels/CellViewModel.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels
{
    /// <summary>
    /// Enhanced ViewModel pre jednu bunku s proper INotifyDataErrorInfo validation pattern
    /// ZLEPŠENIE 3: Implementuje WinUI 3 validation triggers a visual states
    /// ZLEPŠENIE 1: Memory management s proper disposal
    /// ZLEPŠENIE 2: Proper MVVM data binding
    /// </summary>
    public class CellViewModel : INotifyPropertyChanged, INotifyDataErrorInfo, IDisposable
    {
        private object? _value;
        private object? _originalValue;
        private bool _isEditing;
        private bool _hasFocus;
        private bool _isSelected;
        private bool _isReadOnly;
        private readonly Dictionary<string, List<string>> _errors = new();
        private bool _disposed;

        // ZLEPŠENIE 3: Validation state tracking
        private bool _isValidating;
        private DateTime _lastValidationTime = DateTime.MinValue;

        public CellViewModel(string columnName, Type dataType, int rowIndex, int columnIndex)
        {
            ColumnName = columnName ?? throw new ArgumentNullException(nameof(columnName));
            DataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
            RowIndex = rowIndex;
            ColumnIndex = columnIndex;
            CellKey = $"{rowIndex}_{columnName}";
        }

        #region Properties with proper change notification

        public string ColumnName { get; }
        public Type DataType { get; }
        public int RowIndex { get; }
        public int ColumnIndex { get; }
        public string CellKey { get; }

        public object? Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    OnValueChanged();
                }
            }
        }

        public object? OriginalValue
        {
            get => _originalValue;
            set => SetProperty(ref _originalValue, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool HasFocus
        {
            get => _hasFocus;
            set => SetProperty(ref _hasFocus, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set => SetProperty(ref _isReadOnly, value);
        }

        /// <summary>
        /// ZLEPŠENIE 3: Validation state property
        /// </summary>
        public bool IsValidating
        {
            get => _isValidating;
            set => SetProperty(ref _isValidating, value);
        }

        /// <summary>
        /// Formatted value for display
        /// </summary>
        public string DisplayValue
        {
            get
            {
                if (Value == null) return "";

                return Value switch
                {
                    DateTime dt => dt.ToString("dd.MM.yyyy HH:mm"),
                    decimal or double or float => string.Format("{0:N2}", Value),
                    _ => Value.ToString() ?? ""
                };
            }
        }

        /// <summary>
        /// Či má bunka validačné chyby
        /// </summary>
        public bool HasValidationErrors => _errors.Count > 0;

        /// <summary>
        /// Text validačných chýb pre zobrazenie
        /// </summary>
        public string ValidationErrorsText =>
            string.Join("; ", _errors.SelectMany(kvp => kvp.Value));

        /// <summary>
        /// Či je bunka prázdna
        /// </summary>
        public bool IsEmpty => Value == null || string.IsNullOrWhiteSpace(Value.ToString());

        /// <summary>
        /// ZLEPŠENIE 3: Či bola bunka nedávno validovaná (pre throttling)
        /// </summary>
        public bool WasRecentlyValidated(TimeSpan threshold)
        {
            return DateTime.UtcNow - _lastValidationTime < threshold;
        }

        #endregion

        #region Cell Operations

        /// <summary>
        /// Začne editáciu bunky
        /// </summary>
        public void StartEditing()
        {
            if (IsReadOnly || IsEditing) return;

            OriginalValue = Value;
            IsEditing = true;
            HasFocus = true;

            EditingStarted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Potvrdí zmeny a ukončí editáciu
        /// </summary>
        public void CommitChanges()
        {
            if (!IsEditing) return;

            OriginalValue = Value;
            IsEditing = false;

            EditingCompleted?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Zruší zmeny a vráti pôvodnú hodnotu
        /// </summary>
        public void CancelEditing()
        {
            if (!IsEditing) return;

            Value = OriginalValue;
            ClearValidationErrors();
            IsEditing = false;

            EditingCancelled?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Nastaví hodnotu bez spustenia change events (pre loading)
        /// </summary>
        public void SetValueSilently(object? value)
        {
            _value = value;
            _originalValue = value;
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(DisplayValue));
        }

        /// <summary>
        /// Získa typovanú hodnotu
        /// </summary>
        public T GetValue<T>()
        {
            try
            {
                if (Value == null) return default(T)!;
                if (Value is T directValue) return directValue;
                return (T)Convert.ChangeType(Value, typeof(T))!;
            }
            catch
            {
                return default(T)!;
            }
        }

        /// <summary>
        /// ZLEPŠENIE 3: Označí bunku ako nedávno validovanú
        /// </summary>
        public void MarkAsValidated()
        {
            _lastValidationTime = DateTime.UtcNow;
            IsValidating = false;
        }

        #endregion

        #region INotifyDataErrorInfo Implementation (ZLEPŠENIE 3: Proper Validation)

        public bool HasErrors => _errors.Count > 0;

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return _errors.SelectMany(kvp => kvp.Value);

            return _errors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();
        }

        /// <summary>
        /// Nastaví validačné chyby pre property
        /// </summary>
        public void SetValidationErrors(string propertyName, IEnumerable<string> errors)
        {
            var errorList = errors?.ToList() ?? new List<string>();

            if (errorList.Count == 0)
            {
                if (_errors.Remove(propertyName))
                {
                    OnErrorsChanged(propertyName);
                }
            }
            else
            {
                _errors[propertyName] = errorList;
                OnErrorsChanged(propertyName);
            }

            OnPropertyChanged(nameof(HasValidationErrors));
            OnPropertyChanged(nameof(ValidationErrorsText));

            // ZLEPŠENIE 3: Mark as validated after setting errors
            MarkAsValidated();
        }

        /// <summary>
        /// Vyčistí všetky validačné chyby
        /// </summary>
        public void ClearValidationErrors()
        {
            var propertiesToClear = _errors.Keys.ToList();
            _errors.Clear();

            foreach (var property in propertiesToClear)
            {
                OnErrorsChanged(property);
            }

            OnPropertyChanged(nameof(HasValidationErrors));
            OnPropertyChanged(nameof(ValidationErrorsText));

            // ZLEPŠENIE 3: Mark as validated after clearing errors
            MarkAsValidated();
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        #endregion

        #region Events

        public event EventHandler? EditingStarted;
        public event EventHandler? EditingCompleted;
        public event EventHandler? EditingCancelled;
        public event EventHandler<object?>? ValueChanged;

        #endregion

        #region Private Methods

        private void OnValueChanged()
        {
            OnPropertyChanged(nameof(DisplayValue));
            OnPropertyChanged(nameof(IsEmpty));
            ValueChanged?.Invoke(this, Value);
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

        #region IDisposable (ZLEPŠENIE 1: Memory Management)

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Clear events
                EditingStarted = null;
                EditingCompleted = null;
                EditingCancelled = null;
                ValueChanged = null;
                PropertyChanged = null;
                ErrorsChanged = null;

                // Clear data
                _errors.Clear();
                _value = null;
                _originalValue = null;

                _disposed = true;
            }
            catch
            {
                // Suppress disposal errors
            }
        }

        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CellViewModel));
        }

        #endregion

        public override string ToString()
        {
            return $"Cell[{RowIndex},{ColumnIndex}] {ColumnName}: {DisplayValue} (Errors: {_errors.Count})";
        }
    }
}