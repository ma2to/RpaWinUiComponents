// OPRAVA CS0246: AdvancedWinUiDataGridControl.cs - Chýbajúci using
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/AdvancedWinUiDataGridControl.cs

using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Views; // ✅ KĽÚČOVÁ OPRAVA - pridaný using
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

// ✅ KĽÚČOVÁ OPRAVA: Aliasy pre rozlíšenie PUBLIC vs INTERNAL typov
using InternalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition;
using InternalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.ValidationRule;
using InternalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// HLAVNÝ VSTUPNÝ BOD - PUBLIC API pre AdvancedWinUiDataGrid komponent
    /// OPRAVA CS0246: Používa UnifiedAdvancedDataGridControl ako internal view
    /// </summary>
    public class AdvancedWinUiDataGridControl : UserControl, IDisposable
    {
        // ✅ OPRAVA CS0246: Použitie správneho typu
        private readonly UnifiedAdvancedDataGridControl _internalView;
        private bool _disposed = false;
        private bool _isInitialized = false;
        private readonly object _initializationLock = new object();

        public AdvancedWinUiDataGridControl()
        {
            // ✅ OPRAVA CS0246: Vytvorenie internal view
            _internalView = new UnifiedAdvancedDataGridControl();
            Content = _internalView;
            _internalView.ErrorOccurred += OnInternalError;
        }

        #region Events
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        #endregion

        #region MODULÁRNA KONFIGURÁCIA - Static Methods pre tento komponent

        /// <summary>
        /// MODULÁRNA KONFIGURÁCIA: Statické metódy pre AdvancedWinUiDataGrid komponent
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

        #region HLAVNÉ PUBLIC API METÓDY

        /// <summary>
        /// JEDNODUCHÉ API: Inteligentné načítanie dát s automatickou detekciou stĺpcov a validácií
        /// </summary>
        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📊 LoadDataAsync: {data?.Count ?? 0} riadkov");

                if (!_isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Komponent nie je inicializovaný, spúšťam automatickú inicializáciu...");
                    await AutoInitializeFromDataAsync(data);
                }

                if (data == null || data.Count == 0)
                {
                    await LoadDataAsync(new DataTable());
                    return;
                }

                await _internalView.LoadDataAsync(data);
                System.Diagnostics.Debug.WriteLine("✅ LoadDataAsync úspešne dokončené");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadDataAsync chyba: {ex.Message}");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// JEDNODUCHÉ API: Načítanie DataTable
        /// </summary>
        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📊 LoadDataAsync DataTable: {dataTable?.Rows.Count ?? 0} riadkov");

                if (!_isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Komponent nie je inicializovaný, spúšťam automatickú inicializáciu...");
                    await AutoInitializeFromDataTableAsync(dataTable);
                }

                await _internalView.LoadDataAsync(dataTable);
                System.Diagnostics.Debug.WriteLine("✅ LoadDataAsync DataTable úspešne dokončené");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ LoadDataAsync DataTable chyba: {ex.Message}");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// POKROČILÉ API: Explicitná inicializácia s plnou kontrolou
        /// POUŽÍVA PUBLIC typy na vstupe, konvertuje na INTERNAL typy
        /// </summary>
        public async Task InitializeAsync(
            List<ColumnDefinition> columns,
            List<ValidationRule>? validationRules = null,
            ThrottlingConfig? throttling = null,
            int initialRowCount = 15)
        {
            try
            {
                lock (_initializationLock)
                {
                    if (_isInitialized)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ Komponent už je inicializovaný");
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"🚀 InitializeAsync s {columns?.Count ?? 0} stĺpcami, {initialRowCount} riadkov");

                // ✅ OPRAVA: Konverzia z PUBLIC typov na INTERNAL typy
                var internalColumns = ConvertToInternalColumns(columns ?? new List<ColumnDefinition>());
                var internalRules = ConvertToInternalValidationRules(validationRules ?? new List<ValidationRule>());
                var internalThrottling = ConvertToInternalThrottling(throttling ?? ThrottlingConfig.Default);

                // Volanie internal view s INTERNAL typmi
                await _internalView.InitializeAsync(internalColumns, internalRules, internalThrottling, initialRowCount);

                lock (_initializationLock)
                {
                    _isInitialized = true;
                }

                System.Diagnostics.Debug.WriteLine("✅ InitializeAsync úspešne dokončené");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ InitializeAsync chyba: {ex.Message}");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
                throw;
            }
        }

        #endregion

        #region CONVERSION METHODS - Konverzia medzi PUBLIC a INTERNAL typmi

        /// <summary>
        /// ✅ OPRAVA: Konvertuje PUBLIC ColumnDefinition na INTERNAL ColumnDefinition
        /// </summary>
        private List<InternalColumnDefinition> ConvertToInternalColumns(List<ColumnDefinition> publicColumns)
        {
            var internalColumns = new List<InternalColumnDefinition>();

            foreach (var publicCol in publicColumns)
            {
                var internalCol = new InternalColumnDefinition(publicCol.Name, publicCol.DataType)
                {
                    MinWidth = publicCol.MinWidth,
                    MaxWidth = publicCol.MaxWidth,
                    Width = publicCol.Width,
                    AllowResize = publicCol.AllowResize,
                    AllowSort = publicCol.AllowSort,
                    IsReadOnly = publicCol.IsReadOnly,
                    Header = publicCol.Header,
                    ToolTip = publicCol.ToolTip
                };
                internalColumns.Add(internalCol);
            }

            return internalColumns;
        }

        /// <summary>
        /// ✅ OPRAVA: Konvertuje PUBLIC ValidationRule na INTERNAL ValidationRule
        /// </summary>
        private List<InternalValidationRule> ConvertToInternalValidationRules(List<ValidationRule> publicRules)
        {
            var internalRules = new List<InternalValidationRule>();

            foreach (var publicRule in publicRules)
            {
                var internalRule = new InternalValidationRule(publicRule.ColumnName, publicRule.ValidationFunction, publicRule.ErrorMessage)
                {
                    ApplyCondition = publicRule.ApplyCondition,
                    Priority = publicRule.Priority,
                    RuleName = publicRule.RuleName,
                    IsAsync = publicRule.IsAsync,
                    AsyncValidationFunction = publicRule.AsyncValidationFunction,
                    ValidationTimeout = publicRule.ValidationTimeout
                };
                internalRules.Add(internalRule);
            }

            return internalRules;
        }

        /// <summary>
        /// ✅ OPRAVA: Konvertuje PUBLIC ThrottlingConfig na INTERNAL ThrottlingConfig
        /// </summary>
        private InternalThrottlingConfig ConvertToInternalThrottling(ThrottlingConfig publicThrottling)
        {
            return new InternalThrottlingConfig
            {
                TypingDelayMs = publicThrottling.TypingDelayMs,
                PasteDelayMs = publicThrottling.PasteDelayMs,
                BatchValidationDelayMs = publicThrottling.BatchValidationDelayMs,
                MaxConcurrentValidations = publicThrottling.MaxConcurrentValidations,
                IsEnabled = publicThrottling.IsEnabled,
                ValidationTimeout = publicThrottling.ValidationTimeout,
                MinValidationIntervalMs = publicThrottling.MinValidationIntervalMs
            };
        }

        #endregion

        #region AUTOMATICKÁ INICIALIZÁCIA (internal metódy)

        private async Task AutoInitializeFromDataAsync(List<Dictionary<string, object?>>? data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🤖 AutoInitializeFromDataAsync začína...");

                var detectedColumns = new List<ColumnDefinition>(); // PUBLIC typ
                var basicValidations = new List<ValidationRule>(); // PUBLIC typ

                if (data?.Count > 0)
                {
                    detectedColumns = AutoDetectColumns(data);
                    basicValidations = AutoCreateBasicValidations(detectedColumns);
                }
                else
                {
                    detectedColumns = CreateDefaultColumns();
                }

                var defaultThrottling = ThrottlingConfig.Default; // PUBLIC typ

                await InitializeAsync(detectedColumns, basicValidations, defaultThrottling, 15);

                System.Diagnostics.Debug.WriteLine("✅ AutoInitializeFromDataAsync dokončené");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AutoInitializeFromDataAsync chyba: {ex.Message}");
                throw;
            }
        }

        private async Task AutoInitializeFromDataTableAsync(DataTable? dataTable)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🤖 AutoInitializeFromDataTableAsync začína...");

                var detectedColumns = new List<ColumnDefinition>(); // PUBLIC typ
                var basicValidations = new List<ValidationRule>(); // PUBLIC typ

                if (dataTable?.Columns.Count > 0)
                {
                    detectedColumns = AutoDetectColumns(dataTable);
                    basicValidations = AutoCreateBasicValidations(detectedColumns);
                }
                else
                {
                    detectedColumns = CreateDefaultColumns();
                }

                var defaultThrottling = ThrottlingConfig.Default; // PUBLIC typ

                await InitializeAsync(detectedColumns, basicValidations, defaultThrottling, 15);

                System.Diagnostics.Debug.WriteLine("✅ AutoInitializeFromDataTableAsync dokončené");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AutoInitializeFromDataTableAsync chyba: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region AUTOMATICKÁ DETEKCIA stĺpcov a validácií (PUBLIC typy)

        private List<ColumnDefinition> AutoDetectColumns(List<Dictionary<string, object?>> data)
        {
            var columns = new List<ColumnDefinition>(); // PUBLIC typ

            if (data?.Count > 0)
            {
                var firstRow = data[0];
                foreach (var kvp in firstRow)
                {
                    var columnName = kvp.Key;
                    var value = kvp.Value;
                    var dataType = DetectDataType(value, data, columnName);

                    var column = new ColumnDefinition(columnName, dataType) // PUBLIC typ
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

        private List<ColumnDefinition> AutoDetectColumns(DataTable dataTable)
        {
            var columns = new List<ColumnDefinition>(); // PUBLIC typ

            foreach (DataColumn dataColumn in dataTable.Columns)
            {
                var column = new ColumnDefinition(dataColumn.ColumnName, dataColumn.DataType) // PUBLIC typ
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

        private List<ColumnDefinition> CreateDefaultColumns()
        {
            return new List<ColumnDefinition> // PUBLIC typ
            {
                new ColumnDefinition("Stĺpec1", typeof(string)) { Header = "📝 Stĺpec 1", Width = 150 },
                new ColumnDefinition("Stĺpec2", typeof(string)) { Header = "📝 Stĺpec 2", Width = 150 },
                new ColumnDefinition("Stĺpec3", typeof(string)) { Header = "📝 Stĺpec 3", Width = 150 }
            };
        }

        private List<ValidationRule> AutoCreateBasicValidations(List<ColumnDefinition> columns)
        {
            var rules = new List<ValidationRule>(); // PUBLIC typ

            foreach (var column in columns)
            {
                if (column.Name.ToLower().Contains("email"))
                {
                    rules.Add(ValidationRule.Email(column.Name));
                }
                else if (column.Name.ToLower().Contains("vek") || column.Name.ToLower().Contains("age"))
                {
                    rules.Add(ValidationRule.Range(column.Name, 0, 120, "Vek musí byť 0-120"));
                }
                else if (column.Name.ToLower().Contains("meno") || column.Name.ToLower().Contains("name"))
                {
                    rules.Add(ValidationRule.Required(column.Name));
                }
            }

            return rules;
        }

        #endregion

        #region Helper metódy

        private Type DetectDataType(object? sampleValue, List<Dictionary<string, object?>> allData, string columnName)
        {
            var sampleValues = allData.Take(5).Select(d => d.ContainsKey(columnName) ? d[columnName] : null).ToList();

            if (sampleValues.Any(v => v is int)) return typeof(int);
            if (sampleValues.Any(v => v is decimal)) return typeof(decimal);
            if (sampleValues.Any(v => v is double)) return typeof(double);
            if (sampleValues.Any(v => v is DateTime)) return typeof(DateTime);
            if (sampleValues.Any(v => v is bool)) return typeof(bool);

            return typeof(string);
        }

        private string FormatHeader(string columnName)
        {
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
            return 80;
        }

        private double GetMaxWidthForType(Type dataType)
        {
            if (dataType == typeof(int)) return 100;
            if (dataType == typeof(bool)) return 80;
            if (dataType == typeof(DateTime)) return 160;
            if (dataType == typeof(decimal) || dataType == typeof(double)) return 150;
            return 300;
        }

        private double GetDefaultWidthForType(Type dataType)
        {
            if (dataType == typeof(int)) return 80;
            if (dataType == typeof(bool)) return 60;
            if (dataType == typeof(DateTime)) return 140;
            if (dataType == typeof(decimal) || dataType == typeof(double)) return 120;
            return 150;
        }

        #endregion

        #region Zostávajúce Public Methods

        public void Reset()
        {
            try
            {
                _internalView.Reset();
                lock (_initializationLock)
                {
                    _isInitialized = false;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Reset"));
            }
        }

        public bool IsInitialized()
        {
            ThrowIfDisposed();
            lock (_initializationLock)
            {
                return _isInitialized;
            }
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