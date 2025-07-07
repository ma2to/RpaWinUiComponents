// Testovací kód pre správnu inicializáciu komponentu - MainWindow.xaml.cs
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

namespace RpaWinUiComponents.Demo
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Activated += OnMainWindowActivated;
        }

        private async void OnMainWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 Spúšťa sa inicializácia DataGrid komponentu...");

                // KROK 1: Definícia stĺpcov s debug výstupmi
                var columns = new List<ColumnDefinition>
                {
                    new("Meno", typeof(string))
                    {
                        MinWidth = 120,
                        MaxWidth = 200,
                        Width = 150,
                        Header = "👤 Meno a Priezvisko",
                        ToolTip = "Zadajte celé meno osoby"
                    },
                    new("Email", typeof(string))
                    {
                        MinWidth = 180,
                        MaxWidth = 300,
                        Width = 220,
                        Header = "📧 Email adresa",
                        ToolTip = "Platná email adresa v správnom formáte"
                    },
                    new("Vek", typeof(int))
                    {
                        MinWidth = 60,
                        MaxWidth = 80,
                        Width = 70,
                        Header = "🎂 Vek",
                        ToolTip = "Vek v rokoch (18-100)"
                    },
                    new("Plat", typeof(decimal))
                    {
                        MinWidth = 100,
                        MaxWidth = 150,
                        Width = 120,
                        Header = "💰 Plat (€)",
                        ToolTip = "Mesačný plat v eurách"
                    }
                };

                System.Diagnostics.Debug.WriteLine($"✅ Definovaných {columns.Count} stĺpcov");
                foreach (var col in columns)
                {
                    System.Diagnostics.Debug.WriteLine($"   📏 {col.Name} ({col.Header}) - Width: {col.Width}");
                }

                // KROK 2: Definícia validačných pravidiel
                var validationRules = new List<ValidationRule>
                {
                    new ValidationRule("Meno",
                        (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
                        "Meno je povinné pole")
                    {
                        RuleName = "Meno_Required"
                    },

                    new ValidationRule("Email",
                        (value, row) =>
                        {
                            var email = value?.ToString();
                            return string.IsNullOrEmpty(email) || (email.Contains("@") && email.Contains("."));
                        },
                        "Email musí mať platný formát")
                    {
                        RuleName = "Email_Format"
                    },

                    new ValidationRule("Vek",
                        (value, row) =>
                        {
                            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                return true;

                            if (int.TryParse(value.ToString(), out int age))
                                return age >= 18 && age <= 100;

                            return false;
                        },
                        "Vek musí byť medzi 18-100 rokmi")
                    {
                        RuleName = "Vek_Range"
                    },

                    new ValidationRule("Plat",
                        (value, row) =>
                        {
                            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                                return true;

                            if (decimal.TryParse(value.ToString(), out decimal salary))
                                return salary >= 500 && salary <= 10000;

                            return false;
                        },
                        "Plat musí byť medzi 500-10000 €")
                    {
                        RuleName = "Plat_Range"
                    }
                };

                System.Diagnostics.Debug.WriteLine($"✅ Definovaných {validationRules.Count} validačných pravidiel");

                // KROK 3: Inicializácia komponentu
                System.Diagnostics.Debug.WriteLine("🔧 Inicializuje sa DataGrid komponent...");
                await DataGridControl.InitializeAsync(columns, validationRules);
                System.Diagnostics.Debug.WriteLine("✅ DataGrid komponent inicializovaný");

                // KROK 4: Načítanie testovacích dát
                System.Diagnostics.Debug.WriteLine("📊 Načítavajú sa testové dáta...");
                await LoadTestDataAsync();
                System.Diagnostics.Debug.WriteLine("✅ Testové dáta načítané");

                System.Diagnostics.Debug.WriteLine("🎉 Inicializácia úspešne dokončená!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Chyba pri inicializácii: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            }
        }

        private async Task LoadTestDataAsync()
        {
            try
            {
                // Vytvorenie DataTable s testovými dátami
                var dataTable = new DataTable();

                // Pridanie stĺpcov
                dataTable.Columns.Add("Meno", typeof(string));
                dataTable.Columns.Add("Email", typeof(string));
                dataTable.Columns.Add("Vek", typeof(int));
                dataTable.Columns.Add("Plat", typeof(decimal));

                // Pridanie testovacích dát
                var testData = new object[][]
                {
                    new object[] { "Ján Novák", "jan.novak@example.com", 30, 2500.00m },
                    new object[] { "Mária Svoboda", "maria.svoboda@example.com", 28, 3200.00m },
                    new object[] { "Peter Kováč", "peter.kovac@example.com", 35, 4500.00m },
                    new object[] { "Anna Horváthová", "anna.horvath@example.com", 32, 3800.00m },
                    new object[] { "", "invalid-email", 15, 200.00m }, // Nevalidný riadok pre testovanie
                    new object[] { "Test Manager", "", 45, 5500.00m }  // Ďalší nevalidný riadok
                };

                foreach (var rowData in testData)
                {
                    var row = dataTable.NewRow();
                    row.ItemArray = rowData;
                    dataTable.Rows.Add(row);
                }

                System.Diagnostics.Debug.WriteLine($"📊 DataTable vytvorený s {dataTable.Rows.Count} riadkami a {dataTable.Columns.Count} stĺpcami");

                // Načítanie do DataGrid
                await DataGridControl.LoadDataAsync(dataTable);

                System.Diagnostics.Debug.WriteLine("✅ Dáta úspešne načítané do DataGrid");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Chyba pri načítaní testových dát: {ex.Message}");
                throw;
            }
        }

        // Test metódy pre overenie funkcionality
        private async void OnLoadSampleDataClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Test: Načítavanie ukážkových dát...");
                await LoadTestDataAsync();
                System.Diagnostics.Debug.WriteLine("✅ Test úspešný: Dáta načítané");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Test neúspešný: {ex.Message}");
            }
        }

        private async void OnValidateAllClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Test: Validácia všetkých riadkov...");
                var isValid = await DataGridControl.ValidateAllRowsAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Test dokončený: Všetky validné = {isValid}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Test neúspešný: {ex.Message}");
            }
        }

        private async void OnClearDataClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Test: Vymazávanie dát...");
                await DataGridControl.ClearAllDataAsync();
                System.Diagnostics.Debug.WriteLine("✅ Test úspešný: Dáta vymazané");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Test neúspešný: {ex.Message}");
            }
        }

        private async void OnExportDataClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 Test: Export dát...");
                var exportedData = await DataGridControl.ExportToDataTableAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Test úspešný: Exportovaných {exportedData.Rows.Count} riadkov");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Test neúspešný: {ex.Message}");
            }
        }
    }
}