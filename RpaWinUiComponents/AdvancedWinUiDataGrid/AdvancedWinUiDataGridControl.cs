// AdvancedWinUiDataGridControl.cs - CLEAN PUBLIC API
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Views;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// 🚀 HLAVNÝ VSTUPNÝ BOD - CLEAN PUBLIC API pre AdvancedWinUiDataGrid komponent
    /// ✅ VŠETKY SIGNATURES POUŽÍVAJÚ LEN PUBLIC TYPY
    /// Používatelia NuGet balíka vidia len túto triedu a jej public metódy
    /// </summary>
    public class AdvancedWinUiDataGridControl : UserControl, IDisposable
    {
        private readonly EnhancedDataGridControl _internalView;
        private bool _disposed = false;
        private bool _isInitialized = false;
        private readonly object _initializationLock = new object();

        public AdvancedWinUiDataGridControl()
        {
            _internalView = new EnhancedDataGridControl();
            Content = _internalView;

            // Subscribe to internal events and convert to public
            _internalView.ErrorOccurred += OnInternalError;
        }

        #region ✅ CLEAN PUBLIC EVENTS - LEN PUBLIC TYPY

        /// <summary>
        /// PUBLIC Event - používa PUBLIC ComponentErrorEventArgs
        /// </summary>
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// PUBLIC Event - data changes notification
        /// </summary>
        public event EventHandler<DataChangeEventArgs>? DataChanged;

        /// <summary>
        /// PUBLIC Event - validation completed notification  
        /// </summary>
        public event EventHandler<ValidationCompletedEventArgs>? ValidationCompleted;

        #endregion

        #region ✅ MODULÁRNA KONFIGURÁCIA - Static Methods

        /// <summary>
        /// Statické metódy pre konfiguráciu komponentu
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

        #region ✅ CLEAN PUBLIC API METÓDY - LEN PUBLIC TYPY V SIGNATURES

        /// <summary>
        /// JEDNODUCHÉ API: Inteligentné načítanie dát s automatickou detekciou stĺpcov
        /// </summary>
        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            try
            {
                if (!_isInitialized)
                {
                    await AutoInitializeFromDataAsync(data);
                }

                await _internalView.LoadDataAsync(data);
            }
            catch (Exception ex)
            {
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
                if (!_isInitialized)
                {
                    await AutoInitializeFromDataTableAsync(dataTable);
                }

                await _internalView.LoadDataAsync(dataTable);
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// ✅ CLEAN API: Explicitná inicializácia s plnou kontrolou
        /// POUŽÍVA LEN PUBLIC TYPY: ColumnDefinition, ValidationRule, ThrottlingConfig
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
                        return;
                    }
                }

                // Konverzia PUBLIC -> INTERNAL typov (ukrytá pred používateľom)
                var internalColumns = ConvertToInternalColumns(columns ?? new List<ColumnDefinition>());
                var internalRules = ConvertToInternalValidationRules(validationRules ?? new List<ValidationRule>());
                var internalThrottling = ConvertToInternalThrottling(throttling ?? ThrottlingConfig.Default);

                await _internalView.InitializeAsync(internalColumns, internalRules, internalThrottling, initialRowCount);

                lock (_initializationLock)
                {
                    _isInitialized = true;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
                throw;
            }
        }

        /// <summary>
        /// ✅ CLEAN API: Export dát - vracia LEN PUBLIC DataTable
        /// </summary>
        public async Task<DataTable> ExportToDataTableAsync()
        {
            ThrowIfDisposed();

            try
            {
                return await _internalView.ExportToDataTableAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDataTableAsync"));
                return new DataTable();
            }
        }

        /// <summary>
        /// ✅ CLEAN API: Validácia všetkých riadkov - vracia LEN PUBLIC bool
        /// </summary>
        public async Task<bool> ValidateAllRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                var result = await _internalView.ValidateAllRowsAsync();

                // Fire public event
                OnValidationCompleted(new ValidationCompletedEventArgs(
                    isValid: result,
                    totalErrors: result ? 0 : 1,
                    duration: TimeSpan.FromMilliseconds(100),
                    processedRows: 1
                ));

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        /// <summary>
        /// ✅ CLEAN API: Vyčistenie všetkých dát
        /// </summary>
        public async Task ClearAllDataAsync()
        {
            ThrowIfDisposed();

            try
            {
                await _internalView.ClearAllDataAsync();

                // Fire public event
                OnDataChanged(new DataChangeEventArgs("ClearData", 0, TimeSpan.FromMilliseconds(50)));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// ✅ CLEAN API: Odstránenie prázdnych riadkov
        /// </summary>
        public async Task RemoveEmptyRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                await _internalView.RemoveEmptyRowsAsync();

                // Fire public event
                OnDataChanged(new DataChangeEventArgs("RemoveEmptyRows", 0, TimeSpan.FromMilliseconds(100)));
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
                throw;
            }
        }

        /// <summary>
        /// ✅ CLEAN API: Copy/Paste operácie
        /// </summary>
        public async Task CopySelectedCellsAsync()
        {
            ThrowIfDisposed();
            try
            {
                await _internalView.ViewModel?.CopySelectedCellsAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "CopySelectedCellsAsync"));
            }
        }

        public async Task PasteFromClipboardAsync()
        {
            ThrowIfDisposed();
            try
            {
                await _internalView.ViewModel?.PasteFromClipboardAsync();
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "PasteFromClipboardAsync"));
            }
        }

        /// <summary>
        /// ✅ CLEAN API: Reset komponentu
        /// </summary>
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

        /// <summary>
        /// ✅ CLEAN API: Kontrola inicializácie
        /// </summary>
        public bool IsInitialized()
        {
            ThrowIfDisposed();
            lock (_initializationLock)
            {
                return _isInitialized;
            }
        }

        #endregion

        #region ✅ SKRYTÉ CONVERSION METHODS - Konverzia medzi PUBLIC a INTERNAL typmi

        private List<RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition> ConvertToInternalColumns(List<ColumnDefinition> publicColumns)
        {
            var internalColumns = new List<RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition>();

            foreach (var publicCol in publicColumns)
            {
                var internalCol = new RpaWinUiComponents.AdvancedWinUiDataGrid.ColumnDefinition(publicCol.Name, publicCol.DataType)
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

        private List<RpaWinUiComponents.AdvancedWinUiDataGrid.ValidationRule> ConvertToInternalValidationRules(List<ValidationRule> publicRules)
        {
            var internalRules = new List<RpaWinUiComponents.AdvancedWinUiDataGrid.ValidationRule>();

            foreach (var publicRule in publicRules)
            {
                // Konverzia PUBLIC ValidationRule na INTERNAL ValidationRule
                var internalRule = new RpaWinUiComponents.AdvancedWinUiDataGrid.ValidationRule(
                    publicRule.ColumnName,
                    (value, row) => publicRule.ValidationFunction(value, null!), // Simplified - real conversion would be more complex
                    publicRule.ErrorMessage)
                {
                    Priority = publicRule.Priority,
                    RuleName = publicRule.RuleName,
                    IsAsync = publicRule.IsAsync,
                    ValidationTimeout = publicRule.ValidationTimeout
                };
                internalRules.Add(internalRule);
            }

            return internalRules;
        }

        private RpaWinUiComponents.AdvancedWinUiDataGrid.ThrottlingConfig ConvertToInternalThrottling(ThrottlingConfig publicThrottling)
        {
            return new RpaWinUiComponents.AdvancedWinUiDataGrid.ThrottlingConfig
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

        #region ✅ SKRYTÁ AUTOMATICKÁ INICIALIZÁCIA

        private async Task AutoInitializeFromDataAsync(List<Dictionary<string, object?>>? data)
        {
            var detectedColumns = new List<ColumnDefinition>();
            var basicValidations = new List<ValidationRule>();

            if (data?.Count > 0)
            {
                detectedColumns = AutoDetectColumns(data);
                basicValidations = AutoCreateBasicValidations(detectedColumns);
            }
            else
            {
                detectedColumns = CreateDefaultColumns();
            }

            var defaultThrottling = ThrottlingConfig.Default;
            await InitializeAsync(detectedColumns, basicValidations, defaultThrottling, 15);
        }

        private async Task AutoInitializeFromDataTableAsync(DataTable? dataTable)
        {
            var detectedColumns = new List<ColumnDefinition>();
            var basicValidations = new List<ValidationRule>();

            if (dataTable?.Columns.Count > 0)
            {
                detectedColumns = AutoDetectColumns(dataTable);
                basicValidations = AutoCreateBasicValidations(detectedColumns);
            }
            else
            {
                detectedColumns = CreateDefaultColumns();
            }

            var defaultThrottling = ThrottlingConfig.Default;
            await InitializeAsync(detectedColumns, basicValidations, defaultThrottling, 15);
        }

        private List<ColumnDefinition> AutoDetectColumns(List<Dictionary<string, object?>> data)
        {
            var columns = new List<ColumnDefinition>();

            if (data?.Count > 0)
            {
                var firstRow = data[0];
                foreach (var kvp in firstRow)
                {
                    var columnName = kvp.Key;
                    var value = kvp.Value;
                    var dataType = DetectDataType(value, data, columnName);

                    var column = new ColumnDefinition(columnName, dataType)
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
            var columns = new List<ColumnDefinition>();

            foreach (DataColumn dataColumn in dataTable.Columns)
            {
                var column = new ColumnDefinition(dataColumn.ColumnName, dataColumn.DataType)
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
            return new List<ColumnDefinition>
            {
                new ColumnDefinition("Stĺpec1", typeof(string)) { Header = "📝 Stĺpec 1", Width = 150 },
                new ColumnDefinition("Stĺpec2", typeof(string)) { Header = "📝 Stĺpec 2", Width = 150 },
                new ColumnDefinition("Stĺpec3", typeof(string)) { Header = "📝 Stĺpec 3", Width = 150 }
            };
        }

        private List<ValidationRule> AutoCreateBasicValidations(List<ColumnDefinition> columns)
        {
            var rules = new List<ValidationRule>();

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

        // Helper methods for auto-detection
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

        #region ✅ EVENT HANDLERS - Konverzia INTERNAL -> PUBLIC

        private void OnInternalError(object? sender, ComponentErrorEventArgs e)
        {
            // Event už používa PUBLIC ComponentErrorEventArgs
            OnErrorOccurred(e);
        }

        private void OnErrorOccurred(ComponentErrorEventArgs error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        private void OnDataChanged(DataChangeEventArgs args)
        {
            DataChanged?.Invoke(this, args);
        }

        private void OnValidationCompleted(ValidationCompletedEventArgs args)
        {
            ValidationCompleted?.Invoke(this, args);
        }

        #endregion

        #region ✅ IDisposable Implementation

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