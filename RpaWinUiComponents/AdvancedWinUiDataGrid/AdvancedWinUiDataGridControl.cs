// KROK 4: ČISTÁ VERZIA AdvancedWinUiDataGridControl.cs (BEZ DUPLIKÁTOV)
// SÚBOR: RpaWinUiComponents/AdvancedWinUiDataGrid/AdvancedWinUiDataGridControl.cs

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

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// HLAVNÝ VSTUPNÝ BOD - Čistý wrapper pre AdvancedWinUiDataGrid komponent
    /// AUTOMATICKÁ DETEKCIA stĺpcov a validácií + ŽIADNE DUPLIKÁTY TYPOV
    /// </summary>
    public class AdvancedWinUiDataGridControl : UserControl, IDisposable
    {
        private readonly AdvancedDataGridControl _internalView;
        private bool _disposed = false;
        private bool _isInitialized = false;
        private readonly object _initializationLock = new object();

        public AdvancedWinUiDataGridControl()
        {
            _internalView = new AdvancedDataGridControl();
            Content = _internalView;
            _internalView.ErrorOccurred += OnInternalError;
        }

        #region Events
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Static Configuration Methods (CLEAN PUBLIC API)
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

        #region HLAVNÉ PUBLIC API - Automatic Detection (INTELIGENTNÉ METÓDY)

        /// <summary>
        /// JEDNODUCHÉ API: Inteligentné načítanie dát s automatickou detekciou stĺpcov a validácií
        /// </summary>
        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📊 LoadDataAsync: {data?.Count ?? 0} riadkov");

                // KĽÚČOVÁ OPRAVA: Kontrola inicializácie
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

                // Načítanie dát do už inicializovaného komponentu
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
        /// JEDNODUCHÉ API: Inteligentné načítanie DataTable s automatickou detekciou
        /// </summary>
        public async Task LoadDataAsync(DataTable dataTable)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"📊 LoadDataAsync DataTable: {dataTable?.Rows.Count ?? 0} riadkov");

                // KĽÚČOVÁ OPRAVA: Kontrola inicializácie
                if (!_isInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Komponent nie je inicializovaný, spúšťam automatickú inicializáciu...");
                    await AutoInitializeFromDataTableAsync(dataTable);
                }

                // Načítanie dát do už inicializovaného komponentu
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

        #endregion

        #region POKROČILÉ PUBLIC API s explicitnou konfiguráciou

        /// <summary>
        /// POKROČILÉ API: Explicitná inicializácia - ČISTÉ BEZ KONVERZIÍ
        /// </summary>
        public async Task InitializeAsync(
            List<ColumnDefinition> columns,                    // ✅ PRIAMO hlavný typ
            List<ValidationRule>? validationRules = null,      // ✅ PRIAMO hlavný typ
            ThrottlingConfig? throttling = null,               // ✅ PRIAMO hlavný typ
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

                // ✅ ŽIADNE KONVERZIE - používame priamo správne typy
                var safeColumns = columns ?? new List<ColumnDefinition>();
                var safeRules = validationRules ?? new List<ValidationRule>();
                var safeThrottling = throttling ?? ThrottlingConfig.Default;

                // Volanie internal view s rovnakými typmi
                await _internalView.InitializeAsync(safeColumns, safeRules, safeThrottling, initialRowCount);

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

        #region AUTOMATICKÁ INICIALIZÁCIA (internal metódy)

        /// <summary>
        /// INTERNAL: Automatická inicializácia z Dictionary dát
        /// </summary>
        private async Task AutoInitializeFromDataAsync(List<Dictionary<string, object?>>? data)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🤖 AutoInitializeFromDataAsync začína...");

                var detectedColumns = new List<ColumnDefinition>();
                var basicValidations = new List<ValidationRule>();

                if (data?.Count > 0)
                {
                    // AUTOMATICKÁ DETEKCIA stĺpcov z dát
                    detectedColumns = AutoDetectColumns(data);
                    basicValidations = AutoCreateBasicValidations(detectedColumns);
                }
                else
                {
                    // Základné stĺpce ak nie sú dáta
                    detectedColumns = CreateDefaultColumns();
                }

                var defaultThrottling = ThrottlingConfig.Default;

                // Internal inicializácia - ŽIADNE KONVERZIE
                await _internalView.InitializeAsync(detectedColumns, basicValidations, defaultThrottling, 15);

                lock (_initializationLock)
                {
                    _isInitialized = true;
                }

                System.Diagnostics.Debug.WriteLine("✅ AutoInitializeFromDataAsync dokončené");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AutoInitializeFromDataAsync chyba: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// INTERNAL: Automatická inicializácia z DataTable
        /// </summary>
        private async Task AutoInitializeFromDataTableAsync(DataTable? dataTable)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🤖 AutoInitializeFromDataTableAsync začína...");

                var detectedColumns = new List<ColumnDefinition>();
                var basicValidations = new List<ValidationRule>();

                if (dataTable?.Columns.Count > 0)
                {
                    // AUTOMATICKÁ DETEKCIA stĺpcov z DataTable
                    detectedColumns = AutoDetectColumns(dataTable);
                    basicValidations = AutoCreateBasicValidations(detectedColumns);
                }
                else
                {
                    // Základné stĺpce ak nie sú dáta
                    detectedColumns = CreateDefaultColumns();
                }

                var defaultThrottling = ThrottlingConfig.Default;

                // Internal inicializácia - ŽIADNE KONVERZIE
                await _internalView.InitializeAsync(detectedColumns, basicValidations, defaultThrottling, 15);

                lock (_initializationLock)
                {
                    _isInitialized = true;
                }

                System.Diagnostics.Debug.WriteLine("✅ AutoInitializeFromDataTableAsync dokončené");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ AutoInitializeFromDataTableAsync chyba: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region AUTOMATICKÁ DETEKCIA stĺpcov a validácií

        /// <summary>
        /// Automaticky detekuje stĺpce z Dictionary dát
        /// </summary>
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

                    // Detekcia typu na základe hodnoty
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

        /// <summary>
        /// Automaticky detekuje stĺpce z DataTable
        /// </summary>
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

        /// <summary>
        /// Vytvorí základné stĺpce ak nie sú dostupné dáta
        /// </summary>
        private List<ColumnDefinition> CreateDefaultColumns()
        {
            return new List<ColumnDefinition>
            {
                new ColumnDefinition("Stĺpec1", typeof(string)) { Header = "📝 Stĺpec 1", Width = 150 },
                new ColumnDefinition("Stĺpec2", typeof(string)) { Header = "📝 Stĺpec 2", Width = 150 },
                new ColumnDefinition("Stĺpec3", typeof(string)) { Header = "📝 Stĺpec 3", Width = 150 }
            };
        }

        /// <summary>
        /// Automaticky vytvára základné validácie na základe typu stĺpca
        /// </summary>
        private List<ValidationRule> AutoCreateBasicValidations(List<ColumnDefinition> columns)
        {
            var rules = new List<ValidationRule>();

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

        private ValidationRule CreateEmailValidation(string columnName)
        {
            return new ValidationRule(columnName, (value, row) =>
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

        private ValidationRule CreateAgeValidation(string columnName)
        {
            return new ValidationRule(columnName, (value, row) =>
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

        private ValidationRule CreateNameValidation(string columnName)
        {
            return new ValidationRule(columnName, (value, row) =>
            {
                var name = value?.ToString() ?? "";
                return name.Length >= 2;
            }, "Meno musí mať aspoň 2 znaky")
            {
                RuleName = $"{columnName}_Name"
            };
        }

        private ValidationRule CreateSalaryValidation(string columnName)
        {
            return new ValidationRule(columnName, (value, row) =>
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

        private ValidationRule CreateNumericValidation(string columnName, string errorMessage)
        {
            return new ValidationRule(columnName, (value, row) =>
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
                return _isInitialized && _internalView?.ViewModel?.IsInitialized == true;
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