// Debug Helper - pridajte do MainWindow.xaml.cs pre diagnostiku
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

namespace RpaWinUiComponents.Demo
{
    public static class DataGridDebugHelper
    {
        /// <summary>
        /// Diagnostikuje stav DataGrid komponentu a ViewModel
        /// </summary>
        public static void DiagnoseDataGrid(RpaWinUiComponents.AdvancedWinUiDataGrid.Views.AdvancedDataGridControl dataGridControl)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 === DIAGNOSTIKA DATAGRID KOMPONENTU ===");

                // Kontrola základného stavu
                System.Diagnostics.Debug.WriteLine($"📊 DataGrid Control: {(dataGridControl != null ? "✅ OK" : "❌ NULL")}");

                if (dataGridControl?.ViewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ KRITICKÁ CHYBA: ViewModel je NULL!");
                    System.Diagnostics.Debug.WriteLine("💡 Riešenie: Skontrolujte či sa volá InitializeAsync()");
                    return;
                }

                var viewModel = dataGridControl.ViewModel;
                System.Diagnostics.Debug.WriteLine($"🧠 ViewModel: ✅ OK");
                System.Diagnostics.Debug.WriteLine($"🔧 Je inicializovaný: {(viewModel.IsInitialized ? "✅ ÁNO" : "❌ NIE")}");

                // Kontrola stĺpcov
                System.Diagnostics.Debug.WriteLine($"\n📏 === STĹPCE ===");
                if (viewModel.Columns == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ CHYBA: Columns collection je NULL!");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"📊 Počet stĺpcov: {viewModel.Columns.Count}");

                if (viewModel.Columns.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ PROBLÉM: Žiadne stĺpce nie sú definované!");
                    System.Diagnostics.Debug.WriteLine("💡 Riešenie: Skontrolujte či sa volá InitializeAsync() s parametrom columns");
                    return;
                }

                for (int i = 0; i < viewModel.Columns.Count; i++)
                {
                    var col = viewModel.Columns[i];
                    System.Diagnostics.Debug.WriteLine($"   {i + 1}. {col.Name} ({col.Header}) - Width: {col.Width}, DataType: {col.DataType.Name}");
                }

                // Kontrola riadkov
                System.Diagnostics.Debug.WriteLine($"\n📋 === RIADKY ===");
                if (viewModel.Rows == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ CHYBA: Rows collection je NULL!");
                    return;
                }

                System.Diagnostics.Debug.WriteLine($"📊 Počet riadkov: {viewModel.Rows.Count}");

                if (viewModel.Rows.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ PROBLÉM: Žiadne riadky nie sú vytvorené!");
                    System.Diagnostics.Debug.WriteLine("💡 Riešenie: Skontrolujte či sa volá LoadDataAsync() alebo či sa vytvárajú initial rows");
                    return;
                }

                // Analýza prvých 3 riadkov
                var rowsToAnalyze = Math.Min(3, viewModel.Rows.Count);
                for (int i = 0; i < rowsToAnalyze; i++)
                {
                    var row = viewModel.Rows[i];
                    System.Diagnostics.Debug.WriteLine($"\n   Riadok {i + 1}:");
                    System.Diagnostics.Debug.WriteLine($"     - IsEmpty: {row.IsEmpty}");
                    System.Diagnostics.Debug.WriteLine($"     - HasValidationErrors: {row.HasValidationErrors}");
                    System.Diagnostics.Debug.WriteLine($"     - Počet buniek: {row.Cells.Count}");

                    if (row.Cells.Count == 0)
                    {
                        System.Diagnostics.Debug.WriteLine("     ❌ PROBLÉM: Riadok nemá žiadne bunky!");
                        continue;
                    }

                    // Analýza buniek v riadku
                    foreach (var cell in row.Cells.Values.Take(3))
                    {
                        var value = cell.Value?.ToString() ?? "NULL";
                        System.Diagnostics.Debug.WriteLine($"     - {cell.ColumnName}: '{value}' (HasError: {cell.HasValidationError})");
                    }
                }

                // Kontrola validácie
                System.Diagnostics.Debug.WriteLine($"\n✅ === VALIDÁCIA ===");
                System.Diagnostics.Debug.WriteLine($"🔄 Prebieha validácia: {(viewModel.IsValidating ? "✅ ÁNO" : "❌ NIE")}");
                System.Diagnostics.Debug.WriteLine($"📊 Progress: {viewModel.ValidationProgress:F1}%");
                System.Diagnostics.Debug.WriteLine($"📝 Status: {viewModel.ValidationStatus}");

                // Štatistiky
                System.Diagnostics.Debug.WriteLine($"\n📈 === ŠTATISTIKY ===");
                var nonEmptyRows = viewModel.Rows.Where(r => !r.IsEmpty).Count();
                var invalidRows = viewModel.Rows.Where(r => r.HasValidationErrors).Count();

                System.Diagnostics.Debug.WriteLine($"📊 Riadky s dátami: {nonEmptyRows}/{viewModel.Rows.Count}");
                System.Diagnostics.Debug.WriteLine($"❌ Nevalidné riadky: {invalidRows}/{viewModel.Rows.Count}");

                System.Diagnostics.Debug.WriteLine($"\n🎉 === DIAGNOSTIKA DOKONČENÁ ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Chyba počas diagnostiky: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Testuje XAML binding - skontroluje či sa UI elementy správne bindujú na data
        /// </summary>
        public static void TestXamlBinding(RpaWinUiComponents.AdvancedWinUiDataGrid.Views.AdvancedDataGridControl dataGridControl)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔍 === TEST XAML BINDING ===");

                if (dataGridControl?.ViewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ ViewModel je NULL - binding nemôže fungovať");
                    return;
                }

                var viewModel = dataGridControl.ViewModel;

                // Test DataContext
                var dataContext = dataGridControl.DataContext;
                System.Diagnostics.Debug.WriteLine($"📊 DataContext: {(dataContext == viewModel ? "✅ OK" : "❌ PROBLÉM")}");

                if (dataContext != viewModel)
                {
                    System.Diagnostics.Debug.WriteLine("❌ DataContext nie je nastavený na ViewModel!");
                    System.Diagnostics.Debug.WriteLine("💡 Riešenie: Skontrolujte setter property ViewModel v code-behind");
                }

                // Test Collections binding
                System.Diagnostics.Debug.WriteLine($"📏 Columns pre binding: {viewModel.Columns?.Count ?? 0}");
                System.Diagnostics.Debug.WriteLine($"📋 Rows pre binding: {viewModel.Rows?.Count ?? 0}");

                // Test INotifyPropertyChanged
                System.Diagnostics.Debug.WriteLine("🔔 Testovanie PropertyChanged events...");

                bool propertyChangedWorks = false;
                viewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "ValidationStatus")
                    {
                        propertyChangedWorks = true;
                        System.Diagnostics.Debug.WriteLine("✅ PropertyChanged funguje správne");
                    }
                };

                // Trigger property change
                var originalStatus = viewModel.ValidationStatus;
                viewModel.GetType().GetProperty("ValidationStatus")?.SetValue(viewModel, "Test");
                viewModel.GetType().GetProperty("ValidationStatus")?.SetValue(viewModel, originalStatus);

                if (!propertyChangedWorks)
                {
                    System.Diagnostics.Debug.WriteLine("❌ PropertyChanged nefunguje - UI sa nebude aktualizovať");
                }

                System.Diagnostics.Debug.WriteLine("🎉 === TEST XAML BINDING DOKONČENÝ ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Chyba pri teste XAML binding: {ex.Message}");
            }
        }

        /// <summary>
        /// Vygeneruje testové dáta pre debugging
        /// </summary>
        public static DataTable CreateTestDataTable()
        {
            var dataTable = new DataTable();

            // Pridanie stĺpcov
            dataTable.Columns.Add("Meno", typeof(string));
            dataTable.Columns.Add("Email", typeof(string));
            dataTable.Columns.Add("Vek", typeof(int));
            dataTable.Columns.Add("Plat", typeof(decimal));

            // Pridanie testovacích dát
            dataTable.Rows.Add("Test Používateľ 1", "test1@example.com", 25, 2000.00m);
            dataTable.Rows.Add("Test Používateľ 2", "test2@example.com", 30, 2500.00m);
            dataTable.Rows.Add("Test Používateľ 3", "test3@example.com", 35, 3000.00m);

            System.Diagnostics.Debug.WriteLine($"✅ Vygenerovaný test DataTable s {dataTable.Rows.Count} riadkami");
            return dataTable;
        }

        /// <summary>
        /// Vygeneruje validačné pravidlá pre debugging
        /// </summary>
        public static List<ValidationRule> CreateTestValidationRules()
        {
            var rules = new List<ValidationRule>
            {
                new ValidationRule("Meno",
                    (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
                    "Meno je povinné") { RuleName = "Meno_Required" },

                new ValidationRule("Email",
                    (value, row) =>
                    {
                        var email = value?.ToString();
                        return string.IsNullOrEmpty(email) || email.Contains("@");
                    },
                    "Neplatný email") { RuleName = "Email_Format" }
            };

            System.Diagnostics.Debug.WriteLine($"✅ Vygenerovaných {rules.Count} validačných pravidiel");
            return rules;
        }
    }

    // Extension metódy pre ľahšie debugging
    public static class DebugExtensions
    {
        public static void DebugDump(this RpaWinUiComponents.AdvancedWinUiDataGrid.Views.AdvancedDataGridControl dataGrid)
        {
            DataGridDebugHelper.DiagnoseDataGrid(dataGrid);
        }

        public static void TestBinding(this RpaWinUiComponents.AdvancedWinUiDataGrid.Views.AdvancedDataGridControl dataGrid)
        {
            DataGridDebugHelper.TestXamlBinding(dataGrid);
        }
    }
}