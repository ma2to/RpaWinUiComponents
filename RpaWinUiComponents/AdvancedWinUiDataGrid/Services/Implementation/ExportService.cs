// SÚBOR: Services/Implementation/ExportService.cs - OPRAVENÉ
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Events;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Interfaces;

namespace RpaWinUiComponents.AdvancedWinUiDataGrid.Services.Implementation
{
    /// <summary>
    /// ✅ OPRAVA CS0738, CS0051: INTERNAL class s internal parameters
    /// </summary>
    internal class ExportService : IExportService
    {
        private readonly ILogger<ExportService> _logger;

        // ✅ OPRAVA CS0738: Správny event type
        public event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;

        public ExportService(ILogger<ExportService> logger)
        {
            _logger = logger;
        }

        // ✅ OPRAVA CS0051: internal parameters v internal method
        public async Task<DataTable> ExportToDataTableAsync(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns, bool includeValidAlerts = false)
        {
            try
            {
                var rowsList = rows?.ToList() ?? new List<DataGridRow>();
                var columnsList = columns?.ToList() ?? new List<ColumnDefinition>();

                _logger.LogDebug("Exporting {RowCount} rows with {ColumnCount} columns to DataTable, includeValidAlerts: {IncludeValidAlerts}",
                    rowsList.Count, columnsList.Count, includeValidAlerts);

                var dataTable = new DataTable();

                if (rowsList.Count == 0 || columnsList.Count == 0)
                {
                    _logger.LogWarning("Cannot export empty rows or columns");
                    return dataTable;
                }

                var exportColumns = GetExportColumns(columnsList, includeValidAlerts);

                // Create DataTable columns
                await Task.Run(() =>
                {
                    foreach (var column in exportColumns)
                    {
                        var dataType = Nullable.GetUnderlyingType(column.DataType) ?? column.DataType;
                        dataTable.Columns.Add(column.Name, dataType);
                        _logger.LogTrace("Added column to DataTable: {ColumnName} ({DataType})", column.Name, dataType.Name);
                    }
                });

                // Add data rows (only non-empty rows)
                var dataRows = rowsList.Where(r => !r.IsEmpty).ToList();

                await Task.Run(() =>
                {
                    foreach (var row in dataRows)
                    {
                        var dataRow = dataTable.NewRow();

                        foreach (var column in exportColumns)
                        {
                            var value = row.GetValue<object>(column.Name);
                            dataRow[column.Name] = value ?? DBNull.Value;
                        }

                        dataTable.Rows.Add(dataRow);
                    }
                });

                _logger.LogInformation("Successfully exported {RowCount} rows with {ColumnCount} columns to DataTable",
                    dataTable.Rows.Count, dataTable.Columns.Count);
                return dataTable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to DataTable");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDataTableAsync"));
                return new DataTable();
            }
        }

        // ✅ OPRAVA CS0051: internal parameters
        public async Task<string> ExportToCsvAsync(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns, bool includeValidAlerts = false)
        {
            try
            {
                var rowsList = rows?.ToList() ?? new List<DataGridRow>();
                var columnsList = columns?.ToList() ?? new List<ColumnDefinition>();

                _logger.LogDebug("Exporting {RowCount} rows to CSV format, includeValidAlerts: {IncludeValidAlerts}",
                    rowsList.Count, includeValidAlerts);

                if (rowsList.Count == 0 || columnsList.Count == 0)
                {
                    _logger.LogWarning("Cannot export empty rows or columns to CSV");
                    return string.Empty;
                }

                var result = await Task.Run(() =>
                {
                    var sb = new StringBuilder();
                    var exportColumns = GetExportColumns(columnsList, includeValidAlerts);

                    // Add header row
                    var headers = exportColumns.Select(c => EscapeCsvValue(c.Header ?? c.Name));
                    sb.AppendLine(string.Join(",", headers));

                    // Add data rows (only non-empty rows)
                    var dataRows = rowsList.Where(r => !r.IsEmpty).ToList();
                    foreach (var row in dataRows)
                    {
                        var values = exportColumns.Select(c =>
                        {
                            var value = row.GetValue<object>(c.Name);
                            return EscapeCsvValue(value?.ToString() ?? "");
                        });
                        sb.AppendLine(string.Join(",", values));
                    }

                    return sb.ToString();
                });

                _logger.LogInformation("Successfully exported to CSV, length: {Length}", result.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to CSV");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToCsvAsync"));
                return string.Empty;
            }
        }

        // ✅ OPRAVA CS0051: internal parameters
        public async Task<byte[]> ExportToExcelAsync(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns, bool includeValidAlerts = false)
        {
            try
            {
                var rowsList = rows?.ToList() ?? new List<DataGridRow>();

                _logger.LogDebug("Exporting {RowCount} rows to Excel format, includeValidAlerts: {IncludeValidAlerts}",
                    rowsList.Count, includeValidAlerts);

                // For this implementation, we'll export as CSV and convert to bytes
                var csv = await ExportToCsvAsync(rows, columns, includeValidAlerts);
                var result = Encoding.UTF8.GetBytes(csv);

                _logger.LogInformation("Successfully exported to Excel format, bytes: {ByteCount}", result.Length);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to Excel");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToExcelAsync"));
                return Array.Empty<byte>();
            }
        }

        // ✅ OPRAVA CS0051: internal parameters  
        public async Task<List<Dictionary<string, object?>>> ExportToDictionariesAsync(IEnumerable<DataGridRow> rows, IEnumerable<ColumnDefinition> columns)
        {
            try
            {
                var rowsList = rows?.ToList() ?? new List<DataGridRow>();
                var columnsList = columns?.ToList() ?? new List<ColumnDefinition>();

                _logger.LogDebug("Exporting {RowCount} rows to dictionary list", rowsList.Count);

                if (rowsList.Count == 0 || columnsList.Count == 0)
                {
                    _logger.LogWarning("Cannot export empty rows or columns to dictionaries");
                    return new List<Dictionary<string, object?>>();
                }

                var result = await Task.Run(() =>
                {
                    var dictionaries = new List<Dictionary<string, object?>>();
                    var exportColumns = GetExportColumns(columnsList, false); // Exclude ValidAlerts by default

                    var dataRows = rowsList.Where(r => !r.IsEmpty).ToList();
                    foreach (var row in dataRows)
                    {
                        var dict = new Dictionary<string, object?>();
                        foreach (var column in exportColumns)
                        {
                            dict[column.Name] = row.GetValue<object>(column.Name);
                        }
                        dictionaries.Add(dict);
                    }

                    return dictionaries;
                });

                _logger.LogInformation("Successfully exported {RowCount} rows to dictionary list", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to dictionaries");
                OnErrorOccurred(new ComponentErrorEventArgs(ex, "ExportToDictionariesAsync"));
                return new List<Dictionary<string, object?>>();
            }
        }

        private List<ColumnDefinition> GetExportColumns(List<ColumnDefinition> originalColumns, bool includeValidAlerts)
        {
            var exportColumns = new List<ColumnDefinition>();

            // Add normal columns (exclude DeleteAction and ValidAlerts initially)
            var normalColumns = originalColumns
                .Where(c => c.Name != "DeleteAction" && c.Name != "ValidAlerts")
                .ToList();

            exportColumns.AddRange(normalColumns);

            _logger.LogDebug("Added {NormalColumnCount} normal columns to export", normalColumns.Count);

            // Add ValidAlerts column at the end if requested
            if (includeValidAlerts)
            {
                var validAlertsColumn = originalColumns.FirstOrDefault(c => c.Name == "ValidAlerts");
                if (validAlertsColumn != null)
                {
                    exportColumns.Add(validAlertsColumn);
                    _logger.LogDebug("Added ValidAlerts column to export (at end)");
                }
                else
                {
                    _logger.LogWarning("ValidAlerts column requested but not found in original columns");
                }
            }

            _logger.LogInformation("Export columns prepared: {ColumnCount} total, includeValidAlerts: {IncludeValidAlerts}",
                exportColumns.Count, includeValidAlerts);

            return exportColumns;
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            // If value contains comma, quote, newline, or carriage return, escape it
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                // Escape quotes by doubling them and wrap in quotes
                return $"\"{value.Replace("\"", "\"\"")}\"";
            }

            return value;
        }

        protected virtual void OnErrorOccurred(ComponentErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }
    }
}