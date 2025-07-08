//AdvancedWinUiDataGrid/AdvancedWinUiDataGridControl.cs - OPRAVENÝ s top-level triedami
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

// Importy pre public API triedy s premenevanými názvami
using PublicColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.PublicColumnDefinition;
using PublicValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.PublicValidationRule;
using PublicThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.PublicThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid
{
    /// <summary>
    /// Hlavný wrapper komponent pre AdvancedWinUiDataGrid - OPRAVENÝ API s top-level triedami
    /// Demo aplikácie vidia len tento komponent a top-level triedy v Models namespace
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

        #region Inicializácia a Konfigurácia

        /// <summary>
        /// Inicializuje komponent s konfiguráciou stĺpcov a validáciami - OPRAVENÝ API
        /// </summary>
        public async Task InitializeAsync(
            List<PublicColumnDefinition> columns,
            List<PublicValidationRule>? validationRules = null,
            PublicThrottlingConfig? throttling = null,
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

        #region Public Info Methods

        /// <summary>
        /// Kontroluje či je komponent inicializovaný
        /// </summary>
        public bool IsInitialized()
        {
            ThrowIfDisposed();
            return _isInitialized && _internalView?.ViewModel?.IsInitialized == true;
        }

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
        public async Task<int> RemoveRowsByValidationAsync(List<PublicValidationRule> customRules)
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