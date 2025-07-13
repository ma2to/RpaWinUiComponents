// AdvancedWinUiDataGridControl.cs - KOMPLETNÝ INTELIGENTNÝ komponent
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Views;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// KRITICKÁ OPRAVA: Používame JASNE ODDELENÉ namespace pre public API
using PublicColumnDefinition = RpaWinUiComponents.PublicApi.ColumnDefinition;
using PublicValidationRule = RpaWinUiComponents.PublicApi.ValidationRule;
using PublicThrottlingConfig = RpaWinUiComponents.PublicApi.ThrottlingConfig;

// Internal typy s explicitnými aliasmi
using InternalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
using InternalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
using InternalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// Hlavný wrapper komponent pre AdvancedWinUiDataGrid - INTELIGENTNÁ VERZIA
    /// Automaticky detekuje stĺpce a vytvára základné validácie
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
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Static Configuration Methods
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

        #region INTELIGENTNÉ PUBLIC API - Automatická detekcia

        /// <summary>
        /// NOVÉ: Inteligentné načítanie dát s automatickou detekciou stĺpcov a validácií
        /// </summary>
        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            try
            {
                if (data == null || data.Count == 0)
                {
                    await LoadDataAsync(new DataTable());
                    return;
                }

                // AUTOMATICKÁ DETEKCIA stĺpcov z dát
                var detectedColumns = AutoDetectColumns(data);
                var basicValidations = AutoCreateBasicValidations(detectedColumns);
                var defaultThrottling = InternalThrottlingConfig.Default;

                // Automatická inicializácia ak ešte nie je
                if (!_isInitialized)
                {
                    await InitializeAsync(detectedColumns, basicValidations, defaultThrottling, 50);
                }

                // Načítanie dát
                await _internalView.LoadDataAsync(data);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// NOVÉ: Inteligentné načítanie DataTable s automatickou detekciou
        /// </summary>
        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                // AUTOMATICKÁ DETEKCIA stĺpcov z DataTable
                var detectedColumns = AutoDetectColumns(dataTable);
                var basicValidations = AutoCreateBasicValidations(detectedColumns);
                var defaultThrottling = InternalThrottlingConfig.Default;

                // Automatická inicializácia ak ešte nie je
                if (!_isInitialized)
                {
                    await InitializeAsync(detectedColumns, basicValidations, defaultThrottling, 50);
                }

                // Načítanie dát
                await _internalView.LoadDataAsync(dataTable);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        #endregion

        #region Inicializácia a Konfigurácia s Public API

        /// <summary>
        /// PÔVODNÉ API: Public API s explicitnou konverziou typov
        /// </summary>
        public async Task InitializeAsync(
            List<PublicColumnDefinition> columns,
            List<PublicValidationRule>? validationRules = null,
            PublicThrottlingConfig? throttling = null,
            int initialRowCount = 15)
        {
            try
            {
                // RIEŠENIE CS1503: Explicitná konverzia public API typov na internal API typy
                var internalColumns = columns?.ToInternal() ?? new List<InternalColumnDefinition>();

                List<InternalValidationRule>? internalRules = null;
                if (validationRules != null)
                {
                    internalRules = validationRules.ToInternal();
                }

                InternalThrottlingConfig? internalThrottling = null;
                if (throttling != null)
                {
                    internalThrottling = throttling.ToInternal();
                }

                // Volanie internal API s internal typmi
                await InitializeAsync(internalColumns, internalRules, internalThrottling, initialRowCount);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
                throw;
            }
        }

        /// <summary>
        /// INTERNAL API: Používa internal typy priamo
        /// </summary>
        private async Task InitializeAsync(
            List<InternalColumnDefinition> columns,
            List<InternalValidationRule>? validationRules = null,
            InternalThrottlingConfig? throttling = null,
            int initialRowCount = 15)
        {
            try
            {
                await _internalView.InitializeAsync(columns, validationRules, throttling, initialRowCount);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
                throw;
            }
        }

        #endregion

        #region AUTOMATICKÁ DETEKCIA stĺpcov a validácií

        /// <summary>
        /// Automaticky detekuje stĺpce z Dictionary dát
        /// </summary>
        private List<InternalColumnDefinition> AutoDetectColumns(List<Dictionary<string, object?>> data)
        {
            var columns = new List<InternalColumnDefinition>();

            if (data?.Count > 0)
            {
                var firstRow = data[0];
                foreach (var kvp in firstRow)
                {
                    var columnName = kvp.Key;
                    var value = kvp.Value;

                    // Detekcia typu na základe hodnoty
                    var dataType = DetectDataType(value, data, columnName);

                    var column = new InternalColumnDefinition(columnName, dataType)
                    {
                        Header = FormatHeader(columnName),
                        MinWidth = GetMinWidthForType(dataType),
                        MaxWidth = GetMaxWidthForType(dataType),
                        Width = GetDefaultWidthForType(dataType),
                        ToolTip = $"Stĺpec {columnName} typu {dataType.Name}"
                    };

                    columns.Add(column);
                }
            }

            return columns;
        }

        /// <summary>
        /// Automaticky detekuje stĺpce z DataTable
        /// </summary>
        private List<InternalColumnDefinition> AutoDetectColumns(DataTable dataTable)
        {
            var columns = new List<InternalColumnDefinition>();

            foreach (DataColumn dataColumn in dataTable.Columns)
            {
                var column = new InternalColumnDefinition(dataColumn.ColumnName, dataColumn.DataType)
                {
                    Header = FormatHeader(dataColumn.ColumnName),
                    MinWidth = GetMinWidthForType(dataColumn.DataType),
                    MaxWidth = GetMaxWidthForType(dataColumn.DataType),
                    Width = GetDefaultWidthForType(dataColumn.DataType),
                    ToolTip = $"Stĺpec {dataColumn.ColumnName} typu {dataColumn.DataType.Name}"
                };

                columns.Add(column);
            }

            return columns;
        }

        /// <summary>
        /// Automaticky vytvára základné validácie na základe typu stĺpca
        /// </summary>
        private List<InternalValidationRule> AutoCreateBasicValidations(List<InternalColumnDefinition> columns)
        {
            var rules = new List<InternalValidationRule>();

            foreach (var column in columns)
            {
                // Základné validácie podľa názvu stĺpca
                if (column.Name.ToLower().Contains("email"))
                {
                    rules.Add(CreateEmailValidation(column.Name));
                }
                else if (column.Name.ToLower().Contains("vek") || column.Name.ToLower().Contains("age"))
                {
                    rules.Add(CreateAgeValidation(column.Name));
                }
                else if (column.Name.ToLower().Contains("meno") || column.Name.ToLower().Contains("name"))
                {
                    rules.Add(CreateNameValidation(column.Name));
                }
                else if (column.Name.ToLower().Contains("plat") || column.Name.ToLower().Contains("salary"))
                {
                    rules.Add(CreateSalaryValidation(column.Name));
                }

                // Validácie podľa typu
                if (column.DataType == typeof(int) || column.DataType == typeof(int?))
                {
                    rules.Add(CreateNumericValidation(column.Name, "Musí byť celé číslo"));
                }
                else if (column.DataType == typeof(decimal) || column.DataType == typeof(double))
                {
                    rules.Add(CreateNumericValidation(column.Name, "Musí byť číslo"));
                }
            }

            return rules;
        }

        #endregion

        #region Helper metódy pre automatickú detekciu

        private Type DetectDataType(object? sampleValue, List<Dictionary<string, object?>> allData, string columnName)
        {
            // Skontroluj niekoľko hodnôt pre lepšiu detekciu
            var sampleValues = allData.Take(5).Select(d => d.ContainsKey(columnName) ? d[columnName] : null).ToList();

            // Priorita detekcie typov
            if (sampleValues.Any(v => v is int)) return typeof(int);
            if (sampleValues.Any(v => v is decimal)) return typeof(decimal);
            if (sampleValues.Any(v => v is double)) return typeof(double);
            if (sampleValues.Any(v => v is DateTime)) return typeof(DateTime);
            if (sampleValues.Any(v => v is bool)) return typeof(bool);

            // Default je string
            return typeof(string);
        }

        private string FormatHeader(string columnName)
        {
            // Pridaj emoji ikony pre známe stĺpce
            var lowerName = columnName.ToLower();

            if (lowerName.Contains("id")) return $"🔢 {columnName}";
            if (lowerName.Contains("meno") || lowerName.Contains("name")) return $"👤 {columnName}";
            if (lowerName.Contains("email")) return $"📧 {columnName}";
            if (lowerName.Contains("vek") || lowerName.Contains("age")) return $"🎂 {columnName}";
            if (lowerName.Contains("plat") || lowerName.Contains("salary")) return $"💰 {columnName}";
            if (lowerName.Contains("datum") || lowerName.Contains("date")) return $"📅 {columnName}";

            return columnName;
        }

        private double GetMinWidthForType(Type dataType)
        {
            if (dataType == typeof(int)) return 60;
            if (dataType == typeof(bool)) return 50;
            if (dataType == typeof(DateTime)) return 120;
            if (dataType == typeof(decimal) || dataType == typeof(double)) return 100;
            return 80; // string default
        }

        private double GetMaxWidthForType(Type dataType)
        {
            if (dataType == typeof(int)) return 100;
            if (dataType == typeof(bool)) return 80;
            if (dataType == typeof(DateTime)) return 160;
            if (dataType == typeof(decimal) || dataType == typeof(double)) return 150;
            return 300; // string default
        }

        private double GetDefaultWidthForType(Type dataType)
        {
            if (dataType == typeof(int)) return 80;
            if (dataType == typeof(bool)) return 60;
            if (dataType == typeof(DateTime)) return 140;
            if (dataType == typeof(decimal) || dataType == typeof(double)) return 120;
            return 150; // string default
        }

        #endregion

        #region Predefinované validácie

        private InternalValidationRule CreateEmailValidation(string columnName)
        {
            return new InternalValidationRule(columnName, (value, row) =>
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                    return true;

                var email = value.ToString();
                return email?.Contains("@") == true && email.Contains(".") && email.Length > 5;
            }, "Email musí mať platný formát")
            {
                RuleName = $"{columnName}_Email"
            };
        }

        private InternalValidationRule CreateAgeValidation(string columnName)
        {
            return new InternalValidationRule(columnName, (value, row) =>
            {
                if (value == null) return true;

                if (int.TryParse(value.ToString(), out int age))
                    return age >= 0 && age <= 120;

                return false;
            }, "Vek musí byť medzi 0-120 rokmi")
            {
                RuleName = $"{columnName}_Age"
            };
        }

        private InternalValidationRule CreateNameValidation(string columnName)
        {
            return new InternalValidationRule(columnName, (value, row) =>
            {
                var name = value?.ToString() ?? "";
                return name.Length >= 2;
            }, "Meno musí mať aspoň 2 znaky")
            {
                RuleName = $"{columnName}_Name"
            };
        }

        private InternalValidationRule CreateSalaryValidation(string columnName)
        {
            return new InternalValidationRule(columnName, (value, row) =>
            {
                if (value == null) return true;

                if (decimal.TryParse(value.ToString(), out decimal salary))
                    return salary >= 0 && salary <= 50000;

                return false;
            }, "Plat musí byť medzi 0-50000")
            {
                RuleName = $"{columnName}_Salary"
            };
        }

        private InternalValidationRule CreateNumericValidation(string columnName, string errorMessage)
        {
            return new InternalValidationRule(columnName, (value, row) =>
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                    return true;

                return double.TryParse(value.ToString(), out _);
            }, errorMessage)
            {
                RuleName = $"{columnName}_Numeric"
            };
        }

        #endregion

        #region Zostávajúce Public Methods

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

        public bool IsInitialized()
        {
            ThrowIfDisposed();
            return _isInitialized && _internalView?.ViewModel?.IsInitialized == true;
        }

        public int GetColumnCount()
        {
            ThrowIfDisposed();
            return _internalView?.ViewModel?.Columns?.Count ?? 0;
        }

        public int GetRowCount()
        {
            ThrowIfDisposed();
            return _internalView?.ViewModel?.Rows?.Count ?? 0;
        }

        public List<string> GetColumnNames()
        {
            ThrowIfDisposed();

            if (_internalView?.ViewModel?.Columns == null)
                return new List<string>();

            return _internalView.ViewModel.Columns.Select(c => c.Name).ToList();
        }

        public int GetDataRowCount()
        {
            ThrowIfDisposed();

            if (_internalView?.ViewModel?.Rows == null)
                return 0;

            return _internalView.ViewModel.Rows.Count(r => !r.IsEmpty);
        }

        public string GetDebugInfo()
        {
            ThrowIfDisposed();

            var info = new System.Text.StringBuilder();
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

        public async Task<DataTable> ExportToDataTableAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_internalView == null)
                    return new DataTable();

                return await _internalView.ExportToDataTableAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDataTableAsync"));
                return new DataTable();
            }
        }

        public async Task<bool> ValidateAllRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_internalView == null)
                    return false;

                return await _internalView.ValidateAllRowsAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        public async Task ClearAllDataAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_internalView == null)
                    return;

                await _internalView.ClearAllDataAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
                throw;
            }
        }

        public async Task RemoveEmptyRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_internalView == null)
                    return;

                await _internalView.RemoveEmptyRowsAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
                throw;
            }
        }

        public async Task RemoveRowsByConditionAsync(string columnName, Func<object?, bool> condition)
        {
            ThrowIfDisposed();

            try
            {
                if (_internalView?.ViewModel == null)
                    return;

                await _internalView.ViewModel.RemoveRowsByConditionAsync(columnName, condition);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByConditionAsync"));
                throw;
            }
        }

        public async Task<int> RemoveRowsByValidationAsync(List<InternalValidationRule> customRules)
        {
            ThrowIfDisposed();

            try
            {
                if (_internalView?.ViewModel == null)
                    return 0;

                return await _internalView.ViewModel.RemoveRowsByValidationAsync(customRules);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveRowsByValidationAsync"));
                return 0;
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

        protected void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdvancedWinUiDataGridControl));
        }

        #endregion
    }
}