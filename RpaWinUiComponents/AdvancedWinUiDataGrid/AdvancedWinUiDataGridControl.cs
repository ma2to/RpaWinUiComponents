//RpaWinUiComponents/AdvancedWinUiDataGrid/AdvancedWinUiDataGridControl.cs
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Views;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// Hlavný wrapper komponent pre AdvancedWinUiDataGrid - ČISTÝ API
    /// Demo aplikácie vidia len tento komponent a jeho vnorené triedy
    /// </summary>
    public class AdvancedWinUiDataGridControl : UserControl, IDisposable
    {
        private readonly AdvancedDataGridControl _internalView;
        private bool _disposed = false;
        private bool _isInitialized = false;

        public AdvancedWinUiDataGridControl()
        {
            _internalView = new AdvancedDataGridControl();
            Content = _internalView;
            _internalView.ErrorOccurred += OnInternalError;
        }

        #region Events

        /// <summary>
        /// Event ktorý sa spustí pri chybe v komponente
        /// </summary>
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        #endregion

        #region Static Configuration Methods

        /// <summary>
        /// Konfiguruje dependency injection pre AdvancedWinUiDataGrid
        /// </summary>
        public static class Configuration
        {
            public static void ConfigureServices(IServiceProvider serviceProvider)
            {
                RpaWinUiComponents.AdvancedWinUiDataGrid.Configuration.DependencyInjectionConfig.ConfigureServices(serviceProvider);
            }

            public static void ConfigureLogging(ILoggerFactory loggerFactory)
            {
                RpaWinUiComponents.AdvancedWinUiDataGrid.Configuration.LoggerFactory.Configure(loggerFactory);
            }

            public static void SetDebugLogging(bool enabled)
            {
                RpaWinUiComponents.AdvancedWinUiDataGrid.Helpers.DebugHelper.IsDebugEnabled = enabled;
            }
        }

        #endregion

        #region Public API Classes - JEDINÉ TRIEDY KTORÉ DEMO VIDÍ

        /// <summary>
        /// Definícia stĺpca pre DataGrid - ČISTÝ API
        /// </summary>
        public class ColumnDefinition
        {
            public string Name { get; set; } = string.Empty;
            public Type DataType { get; set; } = typeof(string);
            public double MinWidth { get; set; } = 80;
            public double MaxWidth { get; set; } = 300;
            public double Width { get; set; } = 150;
            public bool AllowResize { get; set; } = true;
            public bool AllowSort { get; set; } = true;
            public bool IsReadOnly { get; set; } = false;
            public string? Header { get; set; }
            public string? ToolTip { get; set; }

            public ColumnDefinition() { }

            public ColumnDefinition(string name, Type dataType)
            {
                Name = name;
                DataType = dataType;
                Header = name;
            }

            // Konverzia na internú triedu
            internal Models.ColumnDefinition ToInternal()
            {
                return new Models.ColumnDefinition(Name, DataType)
                {
                    MinWidth = MinWidth,
                    MaxWidth = MaxWidth,
                    Width = Width,
                    AllowResize = AllowResize,
                    AllowSort = AllowSort,
                    IsReadOnly = IsReadOnly,
                    Header = Header,
                    ToolTip = ToolTip
                };
            }
        }

        /// <summary>
        /// Validačné pravidlo pre DataGrid - ČISTÝ API
        /// </summary>
        public class ValidationRule
        {
            public string ColumnName { get; set; } = string.Empty;
            public Func<object?, bool> ValidationFunction { get; set; } = _ => true;
            public string ErrorMessage { get; set; } = string.Empty;
            public Func<bool> ApplyCondition { get; set; } = () => true;
            public int Priority { get; set; } = 0;
            public string RuleName { get; set; } = string.Empty;
            public bool IsAsync { get; set; } = false;
            public Func<object?, CancellationToken, Task<bool>>? AsyncValidationFunction { get; set; }
            public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(5);

            public ValidationRule()
            {
                RuleName = Guid.NewGuid().ToString("N")[..8];
            }

            public ValidationRule(string columnName, Func<object?, bool> validationFunction, string errorMessage)
            {
                ColumnName = columnName;
                ValidationFunction = validationFunction;
                ErrorMessage = errorMessage;
                RuleName = $"{columnName}_{Guid.NewGuid().ToString("N")[..8]}";
            }

            // Konverzia na internú triedu
            internal Models.ValidationRule ToInternal()
            {
                return new Models.ValidationRule(ColumnName,
                    (value, row) => ValidationFunction(value),
                    ErrorMessage)
                {
                    Priority = Priority,
                    RuleName = RuleName,
                    IsAsync = IsAsync,
                    AsyncValidationFunction = AsyncValidationFunction != null
                        ? (value, row, token) => AsyncValidationFunction(value, token)
                        : null,
                    ValidationTimeout = ValidationTimeout,
                    ApplyCondition = row => ApplyCondition()
                };
            }
        }

        /// <summary>
        /// Konfigurácia throttling pre DataGrid - ČISTÝ API
        /// </summary>
        public class ThrottlingConfig
        {
            public int TypingDelayMs { get; set; } = 300;
            public int PasteDelayMs { get; set; } = 100;
            public int BatchValidationDelayMs { get; set; } = 200;
            public int MaxConcurrentValidations { get; set; } = 5;
            public bool IsEnabled { get; set; } = true;
            public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(30);
            public int MinValidationIntervalMs { get; set; } = 50;

            public static ThrottlingConfig Default => new();
            public static ThrottlingConfig Fast => new()
            {
                TypingDelayMs = 150,
                PasteDelayMs = 50,
                BatchValidationDelayMs = 100,
                MaxConcurrentValidations = 10,
                MinValidationIntervalMs = 25
            };
            public static ThrottlingConfig Slow => new()
            {
                TypingDelayMs = 500,
                PasteDelayMs = 200,
                BatchValidationDelayMs = 400,
                MaxConcurrentValidations = 3,
                MinValidationIntervalMs = 100
            };
            public static ThrottlingConfig Disabled => new()
            {
                IsEnabled = false,
                TypingDelayMs = 0,
                PasteDelayMs = 0,
                BatchValidationDelayMs = 0,
                MinValidationIntervalMs = 0
            };

            // Konverzia na internú triedu
            internal Models.ThrottlingConfig ToInternal()
            {
                return new Models.ThrottlingConfig
                {
                    TypingDelayMs = TypingDelayMs,
                    PasteDelayMs = PasteDelayMs,
                    BatchValidationDelayMs = BatchValidationDelayMs,
                    MaxConcurrentValidations = MaxConcurrentValidations,
                    IsEnabled = IsEnabled,
                    ValidationTimeout = ValidationTimeout,
                    MinValidationIntervalMs = MinValidationIntervalMs
                };
            }
        }

        /// <summary>
        /// Informácie o chybe v komponente - ČISTÝ API
        /// </summary>
        public class ComponentError : EventArgs
        {
            public Exception Exception { get; set; }
            public string Operation { get; set; }
            public string? AdditionalInfo { get; set; }
            public DateTime Timestamp { get; set; } = DateTime.Now;

            public ComponentError(Exception exception, string operation, string? additionalInfo = null)
            {
                Exception = exception;
                Operation = operation;
                AdditionalInfo = additionalInfo;
            }

            public override string ToString()
            {
                return $"[{Timestamp:yyyy-MM-dd HH:mm:ss}] {Operation}: {Exception.Message}" +
                       (string.IsNullOrEmpty(AdditionalInfo) ? "" : $" - {AdditionalInfo}");
            }
        }

        #endregion

        #region Inicializácia a Konfigurácia

        /// <summary>
        /// Inicializuje komponent s konfiguráciou stĺpcov a validáciami - ČISTÝ API
        /// </summary>
        public async Task InitializeAsync(
            List<ColumnDefinition> columns,
            List<ValidationRule>? validationRules = null,
            ThrottlingConfig? throttling = null,
            int initialRowCount = 100)
        {
            try
            {
                // Konverzia na interné triedy
                var internalColumns = columns.Select(c => c.ToInternal()).ToList();
                var internalRules = validationRules?.Select(r => r.ToInternal()).ToList();
                var internalThrottling = throttling?.ToInternal();

                await _internalView.InitializeAsync(internalColumns, internalRules, internalThrottling, initialRowCount);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
                throw;
            }
        }

        /// <summary>
        /// Resetuje komponent do pôvodného stavu
        /// </summary>
        public void Reset()
        {
            try
            {
                _internalView.Reset();
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Reset"));
            }
        }

        #endregion

        #region Public Info Methods - ČISTÝ API

        /// <summary>
        /// Získa počet stĺpcov v DataGrid
        /// </summary>
        public int GetColumnCount()
        {
            ThrowIfDisposed();
            return _internalView?.ViewModel?.Columns?.Count ?? 0;
        }

        /// <summary>
        /// Získa počet riadkov v DataGrid
        /// </summary>
        public int GetRowCount()
        {
            ThrowIfDisposed();
            return _internalView?.ViewModel?.Rows?.Count ?? 0;
        }

        /// <summary>
        /// Získa názvy všetkých stĺpcov
        /// </summary>
        public List<string> GetColumnNames()
        {
            ThrowIfDisposed();

            if (_internalView?.ViewModel?.Columns == null)
                return new List<string>();

            return _internalView.ViewModel.Columns.Select(c => c.Name).ToList();
        }

        /// <summary>
        /// Získa informácie o stĺpcoch pre debug
        /// </summary>
        public string GetColumnsInfo()
        {
            ThrowIfDisposed();

            if (_internalView?.ViewModel?.Columns == null)
                return "Žiadne stĺpce";

            var info = new StringBuilder();
            info.AppendLine($"Celkovo stĺpcov: {_internalView.ViewModel.Columns.Count}");

            foreach (var col in _internalView.ViewModel.Columns)
            {
                info.AppendLine($"  - {col.Name} ({col.Header}) - {col.Width}px");
            }

            return info.ToString();
        }

        /// <summary>
        /// Kontroluje či je komponent inicializovaný
        /// </summary>
        public bool IsInitialized()
        {
            ThrowIfDisposed();
            return _isInitialized && _internalView?.ViewModel?.IsInitialized == true;
        }

        /// <summary>
        /// Získa počet neprázdnych riadkov
        /// </summary>
        public int GetDataRowCount()
        {
            ThrowIfDisposed();

            if (_internalView?.ViewModel?.Rows == null)
                return 0;

            return _internalView.ViewModel.Rows.Count(r => !r.IsEmpty);
        }

        /// <summary>
        /// Získa debug informácie o stave komponenta
        /// </summary>
        public string GetDebugInfo()
        {
            ThrowIfDisposed();

            var info = new StringBuilder();
            info.AppendLine($"Komponent inicializovaný: {IsInitialized()}");
            info.AppendLine($"Počet stĺpcov: {GetColumnCount()}");
            info.AppendLine($"Počet riadkov: {GetRowCount()}");
            info.AppendLine($"Počet dátových riadkov: {GetDataRowCount()}");

            if (_internalView?.ViewModel != null)
            {
                info.AppendLine($"Validácia prebieha: {_internalView.ViewModel.IsValidating}");
                info.AppendLine($"Validačný status: {_internalView.ViewModel.ValidationStatus}");
            }

            return info.ToString();
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdvancedWinUiDataGridControl));
        }

        #endregion

        #region Načítanie Dát

        /// <summary>
        /// Načíta dáta z DataTable s automatickou validáciou
        /// </summary>
        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("Component must be initialized first");

                await _internalView.LoadDataAsync(dataTable);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// Načíta dáta zo zoznamu dictionary objektov
        /// </summary>
        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            try
            {
                if (!_isInitialized)
                    throw new InvalidOperationException("Component must be initialized first");

                await _internalView.LoadDataAsync(data);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        #endregion

        #region Export Dát

        /// <summary>
        /// Exportuje validné dáta do DataTable
        /// </summary>
        public async Task<DataTable> ExportToDataTableAsync()
        {
            try
            {
                if (!_isInitialized)
                    return new DataTable();

                return await _internalView.ExportToDataTableAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDataTableAsync"));
                return new DataTable();
            }
        }

        #endregion

        #region Validácia

        /// <summary>
        /// Validuje všetky riadky a vráti true ak sú všetky validné
        /// </summary>
        public async Task<bool> ValidateAllRowsAsync()
        {
            try
            {
                if (!_isInitialized)
                    return false;

                return await _internalView.ValidateAllRowsAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        #endregion

        #region Manipulácia s Riadkami

        /// <summary>
        /// Vymaže všetky dáta zo všetkých buniek
        /// </summary>
        public async Task ClearAllDataAsync()
        {
            try
            {
                if (!_isInitialized)
                    return;

                await _internalView.ClearAllDataAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// Odstráni všetky prázdne riadky
        /// </summary>
        public async Task RemoveEmptyRowsAsync()
        {
            try
            {
                if (!_isInitialized)
                    return;

                await _internalView.RemoveEmptyRowsAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
                throw;
            }
        }

        /// <summary>
        /// Odstráni riadky ktoré spĺňajú zadanú podmienku
        /// </summary>
        public async Task RemoveRowsByConditionAsync(string columnName, Func<object?, bool> condition)
        {
            try
            {
                if (!_isInitialized)
                    return;

                if (_internalView.ViewModel != null)
                {
                    await _internalView.ViewModel.RemoveRowsByConditionAsync(columnName, condition);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
                throw;
            }
        }

        /// <summary>
        /// Odstráni riadky ktoré nevyhovujú vlastným validačným pravidlám
        /// </summary>
        public async Task<int> RemoveRowsByValidationAsync(List<ValidationRule> customRules)
        {
            try
            {
                if (!_isInitialized)
                    return 0;

                if (_internalView.ViewModel != null)
                {
                    var internalRules = customRules.Select(r => r.ToInternal()).ToList();
                    return await _internalView.ViewModel.RemoveRowsByValidationAsync(internalRules);
                }
                return 0;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByValidationAsync"));
                return 0;
            }
        }

        #endregion

        #region Static Validation Helpers - ČISTÝ API

        /// <summary>
        /// Pomocné metódy pre tvorbu validačných pravidiel - ČISTÝ API
        /// </summary>
        public static class Validation
        {
            /// <summary>
            /// Vytvorí pravidlo pre povinné pole
            /// </summary>
            public static ValidationRule Required(string columnName, string? errorMessage = null)
            {
                return new ValidationRule(
                    columnName,
                    value => !string.IsNullOrWhiteSpace(value?.ToString()),
                    errorMessage ?? $"{columnName} je povinné pole"
                )
                {
                    RuleName = $"{columnName}_Required"
                };
            }

            /// <summary>
            /// Vytvorí pravidlo pre kontrolu dĺžky textu
            /// </summary>
            public static ValidationRule Length(string columnName, int minLength, int maxLength = int.MaxValue, string? errorMessage = null)
            {
                return new ValidationRule(
                    columnName,
                    value =>
                    {
                        var text = value?.ToString() ?? "";
                        return text.Length >= minLength && text.Length <= maxLength;
                    },
                    errorMessage ?? $"{columnName} musí mať dĺžku medzi {minLength} a {maxLength} znakmi"
                )
                {
                    RuleName = $"{columnName}_Length"
                };
            }

            /// <summary>
            /// Vytvorí pravidlo pre kontrolu číselného rozsahu
            /// </summary>
            public static ValidationRule Range(string columnName, double min, double max, string? errorMessage = null)
            {
                return new ValidationRule(
                    columnName,
                    value =>
                    {
                        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            return true;

                        if (double.TryParse(value.ToString(), out double numValue))
                        {
                            return numValue >= min && numValue <= max;
                        }

                        return false;
                    },
                    errorMessage ?? $"{columnName} musí byť medzi {min} a {max}"
                )
                {
                    RuleName = $"{columnName}_Range"
                };
            }

            /// <summary>
            /// Vytvorí pravidlo pre validáciu číselných hodnôt
            /// </summary>
            public static ValidationRule Numeric(string columnName, string? errorMessage = null)
            {
                return new ValidationRule(
                    columnName,
                    value =>
                    {
                        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            return true;

                        return double.TryParse(value.ToString(), out _);
                    },
                    errorMessage ?? $"{columnName} musí byť číslo"
                )
                {
                    RuleName = $"{columnName}_Numeric"
                };
            }

            /// <summary>
            /// Vytvorí pravidlo pre validáciu emailu
            /// </summary>
            public static ValidationRule Email(string columnName, string? errorMessage = null)
            {
                return new ValidationRule(
                    columnName,
                    value =>
                    {
                        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            return true;

                        var email = value.ToString();
                        return email?.Contains("@") == true && email.Contains(".") && email.Length > 5;
                    },
                    errorMessage ?? $"{columnName} musí mať platný formát emailu"
                )
                {
                    RuleName = $"{columnName}_Email"
                };
            }

            /// <summary>
            /// Vytvorí podmienené validačné pravidlo
            /// </summary>
            public static ValidationRule Conditional(
                string columnName,
                Func<object?, bool> validationFunction,
                Func<bool> condition,
                string errorMessage,
                string? ruleName = null)
            {
                return new ValidationRule(columnName, validationFunction, errorMessage)
                {
                    ApplyCondition = condition,
                    RuleName = ruleName ?? $"{columnName}_Conditional_{Guid.NewGuid().ToString("N")[..8]}"
                };
            }

            /// <summary>
            /// Vytvorí async validačné pravidlo
            /// </summary>
            public static ValidationRule AsyncRule(
                string columnName,
                Func<object?, CancellationToken, Task<bool>> asyncValidationFunction,
                string errorMessage,
                TimeSpan? timeout = null,
                string? ruleName = null)
            {
                return new ValidationRule(columnName, _ => true, errorMessage)
                {
                    IsAsync = true,
                    AsyncValidationFunction = asyncValidationFunction,
                    ValidationTimeout = timeout ?? TimeSpan.FromSeconds(5),
                    RuleName = ruleName ?? $"{columnName}_Async_{Guid.NewGuid().ToString("N")[..8]}"
                };
            }
        }

        #endregion

        #region Private Event Handlers

        private void OnInternalError(object? sender, ComponentErrorEventArgs e)
        {
            OnErrorOccurred(e);
        }

        private void OnErrorOccurred(ComponentErrorEventArgs error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                if (_internalView != null)
                {
                    _internalView.ErrorOccurred -= OnInternalError;
                    _internalView.Dispose();
                }

                _disposed = true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Dispose"));
            }
        }

        #endregion
    }
}