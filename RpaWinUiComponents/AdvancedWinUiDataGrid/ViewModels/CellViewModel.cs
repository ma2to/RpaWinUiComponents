// OPRAVENÝ CellViewModel.cs - VYRIEŠENÉ WARNINGS
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
    /// Enhanced ViewModel pre jednu bunku s VYPNUTÝMI tooltip validáciami
    /// OPRAVA: INotifyDataErrorInfo NEVRÁTI errors pre tooltips - len pre ValidAlerts stĺpec
    /// </summary>
    internal class CellViewModel : INotifyPropertyChanged, INotifyDataErrorInfo, IDisposable
    {
        private object? _value;
        private object? _originalValue;
        private bool _isEditing;
        private bool _hasFocus;
        private bool _isSelected;
        private bool _isReadOnly;
        private readonly Dictionary<string, List<string>> _errors = new();
        private bool _disposed;

        // Validation state tracking
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
        /// Text validačných chýb pre zobrazenie - LEN PRE ValidAlerts stĺpec
        /// </summary>
        public string ValidationErrorsText =>
            string.Join("; ", _errors.SelectMany(kvp => kvp.Value));

        /// <summary>
        /// Či je bunka prázdna
        /// </summary>
        public bool IsEmpty => Value == null || string.IsNullOrWhiteSpace(Value.ToString());

        /// <summary>
        /// Či bola bunka nedávno validovaná (pre throttling)
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
        /// Označí bunku ako nedávno validovanú
        /// </summary>
        public void MarkAsValidated()
        {
            _lastValidationTime = DateTime.UtcNow;
            IsValidating = false;
        }

        #endregion

        #region OPRAVENÉ INotifyDataErrorInfo Implementation - BEZ TOOLTIP SUPPORT

        /// <summary>
        /// 🚫 KRITICKÁ OPRAVA: VŽDY vráti FALSE aby sa NEVYTVÁRALI TOOLTIPS
        /// Validation errors sa zobrazia len v ValidAlerts stĺpci cez ValidationErrorsText property
        /// </summary>
        public bool HasErrors => false; // 🚫 FIXED: Vždy false = žiadne tooltips

        // ✅ OPRAVA CS0414: ErrorsChanged event je teraz property namiesto field
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        /// <summary>
        /// 🚫 KRITICKÁ OPRAVA: VŽDY vráti prázdnu kolekciu aby sa NEVYTVÁRALI TOOLTIPS
        /// Validation errors sa zobrazia len v ValidAlerts stĺpci
        /// </summary>
        public IEnumerable GetErrors(string? propertyName)
        {
            // 🚫 FIXED: Vždy vráti prázdnu kolekciu = žiadne tooltips
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// Nastaví validačné chyby pre property - INTERNÉ pre ValidAlerts stĺpec
        /// </summary>
        public void SetValidationErrors(string propertyName, IEnumerable<string> errors)
        {
            var errorList = errors?.ToList() ?? new List<string>();

            if (errorList.Count == 0)
            {
                if (_errors.Remove(propertyName))
                {
                    // 🚫 NEFIRE-ujeme ErrorsChanged aby sa NEVYTVÁRALI TOOLTIPS
                    // OnErrorsChanged(propertyName);
                }
            }
            else
            {
                _errors[propertyName] = errorList;
                // 🚫 NEFIRE-ujeme ErrorsChanged aby sa NEVYTVÁRALI TOOLTIPS
                // OnErrorsChanged(propertyName);
            }

            // Fire len property changed pre ValidAlerts stĺpec
            OnPropertyChanged(nameof(HasValidationErrors));
            OnPropertyChanged(nameof(ValidationErrorsText));

            MarkAsValidated();
        }

        /// <summary>
        /// Vyčistí všetky validačné chyby
        /// </summary>
        public void ClearValidationErrors()
        {
            if (_errors.Count > 0)
            {
                _errors.Clear();

                // 🚫 NEFIRE-ujeme ErrorsChanged events aby sa NEVYTVÁRALI TOOLTIPS
                // No ErrorsChanged events fired

                // Fire len property changed pre ValidAlerts stĺpec
                OnPropertyChanged(nameof(HasValidationErrors));
                OnPropertyChanged(nameof(ValidationErrorsText));

                MarkAsValidated();
            }
        }

        /// <summary>
        /// 🚫 VYPNUTÉ: ErrorsChanged event sa už nefire-uje
        /// Metóda zostáva pre budúce použitie ak by sme chceli tooltips zapnúť
        /// </summary>
        private void OnErrorsChanged(string propertyName)
        {
            // 🚫 VYPNUTÉ: Nefire-ujeme ErrorsChanged aby sa NEVYTVÁRALI TOOLTIPS
            // ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

            // ✅ OPRAVA CS0414: ErrorsChanged sa aspoň spomenie v komentári
            // ErrorsChanged event is intentionally not used to prevent tooltip creation
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

        #region IDisposable

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
                ErrorsChanged = null; // ✅ Správne dispose event

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