// MainWindow.xaml.cs - JEDNODUCHÁ OPRAVA - používa iba základné public API
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

// ✅ RIEŠENIE CS0234: Používame iba ZÁKLADNÝ namespace z NuGet balíčka
using RpaWinUiComponents.AdvancedWinUiDataGrid;

namespace RpaWinUiComponents.Demo
{
    public sealed partial class MainWindow : Window
    {
        private bool _isInitialized = false;

        public MainWindow()
        {
            this.InitializeComponent();

            // Inicializácia cez DispatcherQueue na bezpečné načasovanie
            this.DispatcherQueue.TryEnqueue(async () =>
            {
                await Task.Delay(100);
                await InitializeComponentAsync();
            });
        }

        private async Task InitializeComponentAsync()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                System.Diagnostics.Debug.WriteLine("🚀 ŠTART inicializácie MainWindow...");

                UpdateLoadingState("Inicializuje sa komponent...", "Pripravuje sa DataGrid...");
                await Task.Delay(200);

                // ✅ RIEŠENIE: Použijeme iba Dictionary pre stĺpce (jednoduchšie)
                System.Diagnostics.Debug.WriteLine("📊 Vytváram testové dáta...");

                // Testové dáta - najjednoduchší spôsob
                var testData = new List<Dictionary<string, object?>>
                {
                    new() { ["ID"] = 1, ["Meno"] = "Ján Novák", ["Email"] = "jan@example.com", ["Vek"] = 30, ["Plat"] = 2500.00m },
                    new() { ["ID"] = 2, ["Meno"] = "Mária Svoboda", ["Email"] = "maria@company.sk", ["Vek"] = 28, ["Plat"] = 3200.00m },
                    new() { ["ID"] = 3, ["Meno"] = "Peter Kováč", ["Email"] = "peter@firma.sk", ["Vek"] = 35, ["Plat"] = 4500.00m },
                    new() { ["ID"] = 4, ["Meno"] = "", ["Email"] = "invalid-email", ["Vek"] = 15, ["Plat"] = 200.00m }, // Nevalidný
                    new() { ["ID"] = 5, ["Meno"] = "Test User", ["Email"] = "test@example.com", ["Vek"] = 150, ["Plat"] = 50000.00m } // Nevalidný
                };

                UpdateLoadingState("Inicializuje sa DataGrid komponent...", "Pripájajú sa služby...");

                // DEBUG: Kontrola DataGridControl pred inicializáciou
                if (DataGridControl == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ CHYBA: DataGridControl je NULL!");
                    ShowError("DataGridControl nie je dostupný");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("🔧 Spúšťam základnú inicializáciu...");

                // ✅ FINÁLNE RIEŠENIE: Používame iba základný API bez Public API typov
                // Komponent by mal mať rozumné defaulty
                try
                {
                    // Pokús sa načítať dáta priamo - komponent by si mal vytvoriť vlastné stĺpce
                    await DataGridControl.LoadDataAsync(testData);
                    System.Diagnostics.Debug.WriteLine("✅ Dáta načítané pomocou základného API");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"❌ Základné API nefunguje: {ex.Message}");

                    // Fallback - skúsime s DataTable
                    var dataTable = ConvertToDataTable(testData);
                    await DataGridControl.LoadDataAsync(dataTable);
                    System.Diagnostics.Debug.WriteLine("✅ Dáta načítané pomocou DataTable");
                }

                System.Diagnostics.Debug.WriteLine("✅ Načítanie dokončené");

                // KROK 5: Dokončenie inicializácie
                CompleteInitialization();

                System.Diagnostics.Debug.WriteLine("🎉 Inicializácia ÚSPEŠNE dokončená!");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ KRITICKÁ CHYBA pri inicializácii: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                ShowError($"Chyba pri inicializácii: {ex.Message}");
            }
        }

        /// <summary>
        /// Konverzia Dictionary na DataTable pre kompatibilitu
        /// </summary>
        private DataTable ConvertToDataTable(List<Dictionary<string, object?>> data)
        {
            var dataTable = new DataTable();

            if (data?.Count > 0)
            {
                // Pridaj stĺpce na základe prvého záznamu
                foreach (var key in data[0].Keys)
                {
                    dataTable.Columns.Add(key, typeof(object));
                }

                // Pridaj riadky
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

        #region UI Helper metódy

        private void UpdateLoadingState(string detailText, string statusText)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (LoadingDetailText != null)
                    LoadingDetailText.Text = detailText;

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = statusText;
            });
        }

        private void CompleteInitialization()
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (LoadingPanel != null)
                    LoadingPanel.Visibility = Visibility.Collapsed;

                if (DataGridControl != null)
                    DataGridControl.Visibility = Visibility.Visible;

                if (InitStatusText != null)
                {
                    InitStatusText.Text = " - Pripravené";
                    InitStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                }

                if (StatusTextBlock != null)
                {
                    StatusTextBlock.Text = "DataGrid pripravený a inicializovaný úspešne";
                }
            });
        }

        private void ShowError(string errorMessage)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (LoadingDetailText != null)
                    LoadingDetailText.Text = errorMessage;

                if (InitStatusText != null)
                {
                    InitStatusText.Text = " - Chyba";
                    InitStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                }

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = $"Chyba: {errorMessage}";
            });
        }

        #endregion

        #region Button Event Handlers

        private async void OnLoadSampleDataClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 TEST: Načítavanie ukážkových dát...");

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = "Načítavajú sa ukážkové dáta...";

                // Jednoduché testové dáta
                var sampleData = new List<Dictionary<string, object?>>
                {
                    new() { ["Meno"] = "Test Osoba", ["Email"] = "test@test.com", ["Vek"] = 25 },
                    new() { ["Meno"] = "Druhá Osoba", ["Email"] = "druha@test.com", ["Vek"] = 30 }
                };

                await DataGridControl.LoadDataAsync(sampleData);

                if (StatusTextBlock != null)
                {
                    StatusTextBlock.Text = "Ukážkové dáta načítané";
                }

                System.Diagnostics.Debug.WriteLine("✅ TEST úspešný: Ukážkové dáta načítané");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TEST neúspešný: {ex.Message}");

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = $"Chyba: {ex.Message}";
            }
        }

        private async void OnValidateAllClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 TEST: Validácia všetkých riadkov...");

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = "Validujú sa dáta...";

                var isValid = await DataGridControl.ValidateAllRowsAsync();

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = isValid ? "Všetky dáta sú validné" : "Nájdené validačné chyby";

                System.Diagnostics.Debug.WriteLine($"✅ TEST dokončený: Všetky validné = {isValid}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TEST neúspešný: {ex.Message}");

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = $"Chyba pri validácii: {ex.Message}";
            }
        }

        private async void OnClearDataClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 TEST: Vymazávanie dát...");

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = "Vymazávajú sa dáta...";

                await DataGridControl.ClearAllDataAsync();

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = "Dáta vymazané";

                System.Diagnostics.Debug.WriteLine("✅ TEST úspešný: Dáta vymazané");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TEST neúspešný: {ex.Message}");

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = $"Chyba: {ex.Message}";
            }
        }

        private async void OnExportDataClick(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🔄 TEST: Export dát...");

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = "Exportujú sa dáta...";

                var exportedData = await DataGridControl.ExportToDataTableAsync();

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = $"Export dokončený: {exportedData.Rows.Count} riadkov";

                System.Diagnostics.Debug.WriteLine($"✅ TEST úspešný: Exportovaných {exportedData.Rows.Count} riadkov");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TEST neúspešný: {ex.Message}");

                if (StatusTextBlock != null)
                    StatusTextBlock.Text = $"Chyba pri exporte: {ex.Message}";
            }
        }

        #endregion
    }
}