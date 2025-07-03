using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.ViewModels;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Configuration;
using ValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
using ThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Views
{
    /// <summary>
    /// Hlavný UserControl pre AdvancedWinUiDataGrid komponent
    /// </summary>
    public sealed partial class AdvancedDataGridControl : UserControl, IDisposable
    {
        private AdvancedDataGridViewModel? _viewModel;
        private readonly ILogger<AdvancedDataGridControl> _logger;
        private bool _disposed = false;

        public AdvancedDataGridControl()
        {
            this.InitializeComponent();

            var loggerProvider = GetLoggerProvider();
            _logger = loggerProvider.CreateLogger<AdvancedDataGridControl>();

            _logger.LogDebug("AdvancedDataGridControl created");
        }

        #region Properties and Events

        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        #endregion

        #region Public API Methods

        /// <summary>
        /// Inicializuje komponent s konfiguráciou stĺpcov a validáciami
        /// </summary>
        public async Task InitializeAsync(
            List<DataGridColumnDefinition> columns,
            List<ValidationRule>? validationRules = null,
            ThrottlingConfig? throttling = null,
            int initialRowCount = 100)
        {
            ThrowIfDisposed();

            try
            {
                _logger.LogInformation("Initializing AdvancedDataGrid with {ColumnCount} columns, {InitialRowCount} rows",
                    columns?.Count ?? 0, initialRowCount);

                if (_viewModel == null)
                {
                    _viewModel = CreateViewModel();
                }

                await _viewModel.InitializeAsync(columns, validationRules ?? new List<ValidationRule>(), throttling, initialRowCount);

                _logger.LogInformation("AdvancedDataGrid initialized successfully with {InitialRowCount} rows", initialRowCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing AdvancedDataGrid");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "InitializeAsync"));
                throw;
            }
        }

        /// <summary>
        /// Načíta dáta z DataTable s automatickou validáciou
        /// </summary>
        public async Task LoadDataAsync(DataTable dataTable)
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null)
                    throw new InvalidOperationException("Component must be initialized first! Call InitializeAsync() before LoadDataAsync().");

                if (!_viewModel.IsInitialized)
                    throw new InvalidOperationException("Component not properly initialized! Call InitializeAsync() with validation rules first.");

                _logger.LogInformation("Loading data from DataTable with {RowCount} rows", dataTable?.Rows.Count ?? 0);
                await _viewModel.LoadDataAsync(dataTable);
                _logger.LogInformation("Data loaded successfully with applied validations");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// Načíta dáta zo zoznamu dictionary objektov
        /// </summary>
        public async Task LoadDataAsync(List<Dictionary<string, object?>> data)
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null)
                    throw new InvalidOperationException("Component must be initialized first!");

                var dataTable = ConvertToDataTable(data);
                await _viewModel.LoadDataAsync(dataTable);
                _logger.LogInformation("Data loaded from dictionary list successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data from dictionary list");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "LoadDataAsync"));
                throw;
            }
        }

        /// <summary>
        /// Exportuje validné dáta do DataTable
        /// </summary>
        public async Task<DataTable> ExportToDataTableAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null)
                    return new DataTable();

                var result = await _viewModel.ExportDataAsync();
                _logger.LogInformation("Data exported to DataTable with {RowCount} rows", result.Rows.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDataTableAsync"));
                return new DataTable();
            }
        }

        /// <summary>
        /// Validuje všetky riadky a vráti true ak sú všetky validné
        /// </summary>
        public async Task<bool> ValidateAllRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null)
                    return false;

                var result = await _viewModel.ValidateAllRowsAsync();
                _logger.LogInformation("Validation completed, all valid: {AllValid}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating all rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ValidateAllRowsAsync"));
                return false;
            }
        }

        /// <summary>
        /// Vymaže všetky dáta zo všetkých buniek
        /// </summary>
        public async Task ClearAllDataAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null) return;

                await _viewModel.ClearAllDataInternalAsync();
                _logger.LogInformation("All data cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing all data");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ClearAllDataAsync"));
            }
        }

        /// <summary>
        /// Odstráni všetky prázdne riadky
        /// </summary>
        public async Task RemoveEmptyRowsAsync()
        {
            ThrowIfDisposed();

            try
            {
                if (_viewModel == null) return;

                await _viewModel.RemoveEmptyRowsInternalAsync();
                _logger.LogInformation("Empty rows removed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing empty rows");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "RemoveEmptyRowsAsync"));
            }
        }

        /// <summary>
        /// Reset komponentu do pôvodného stavu
        /// </summary>
        public void Reset()
        {
            if (_disposed) return;

            try
            {
                _logger.LogInformation("Resetting AdvancedDataGrid");
                _viewModel?.Reset();
                _logger.LogInformation("AdvancedDataGrid reset completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting AdvancedDataGrid");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "Reset"));
            }
        }

        #endregion

        #region Private Helper Methods

        private AdvancedDataGridViewModel CreateViewModel()
        {
            try
            {
                return DependencyInjectionConfig.GetService<AdvancedDataGridViewModel>()
                       ?? DependencyInjectionConfig.CreateViewModelWithoutDI();
            }
            catch
            {
                return DependencyInjectionConfig.CreateViewModelWithoutDI();
            }
        }

        private IDataGridLoggerProvider GetLoggerProvider()
        {
            try
            {
                return DependencyInjectionConfig.GetService<IDataGridLoggerProvider>()
                       ?? NullDataGridLoggerProvider.Instance;
            }
            catch
            {
                return NullDataGridLoggerProvider.Instance;
            }
        }

        private DataTable ConvertToDataTable(List<Dictionary<string, object?>> data)
        {
            var dataTable = new DataTable();

            if (data?.Count > 0)
            {
                // Create columns from first dictionary
                foreach (var key in data[0].Keys)
                {
                    dataTable.Columns.Add(key, typeof(object));
                }

                // Add rows
                foreach (var row in data)
                {
                    var dataRow = dataTable.NewRow();
                    foreach (var kvp in row)
                    {
                        dataRow[kvp.Key] = kvp.Value ?? DBNull.Value;
                    }
                    dataTable.Rows.Add(dataRow);
                }
            }

            return dataTable;
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _logger?.LogDebug("Disposing AdvancedDataGridControl...");

                if (_viewModel != null)
                {
                    _viewModel.Dispose();
                    _viewModel = null;
                }

                this.DataContext = null;

                _disposed = true;
                _logger?.LogInformation("AdvancedDataGridControl disposed successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during disposal");
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AdvancedDataGridControl));
        }

        #endregion

        #region Error Handling

        protected void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        #endregion
    }
}