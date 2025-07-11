// AdvancedWinUiDataGridControl.cs - FINÁLNA OPRAVA všetkých CS chýb
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
    /// Hlavný wrapper komponent pre AdvancedWinUiDataGrid - RIEŠENIE VŠETKÝCH CS CHÝB
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

        #region Inicializácia a Konfigurácia - OPRAVA CS1503

        /// <summary>
        /// KĽÚČOVÁ OPRAVA CS1503: Public API s automatickou konverziou typov
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
                await _internalView.InitializeAsync(internalColumns, internalRules, internalThrottling, initialRowCount);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
                throw;
            }
        }

        #endregion

        #region Public Methods

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

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdvancedWinUiDataGridControl));
        }

        #endregion

        #region Načítanie Dát

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
        /// RIEŠENIE CS1503: Odstráni riadky ktoré nevyhovujú vlastným validačným pravidlám - public API
        /// </summary>
        public async Task<int> RemoveRowsByValidationAsync(List<PublicValidationRule> customRules)
        {
            try
            {
                if (!_isInitialized)
                    return 0;

                if (_internalView.ViewModel != null)
                {
                    // KĽÚČOVÁ OPRAVA CS1503: Konverzia z public API na internal API
                    var internalRules = customRules?.ToInternal() ?? new List<InternalValidationRule>();
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