// MainWindow.xaml.cs - OPRAVENÁ VERZIA S DEBUG METÓDAMI
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

                // KROK 1: Definícia stĺpcov s debug
                System.Diagnostics.Debug.WriteLine("📊 Vytváram definície stĺpcov...");
                var columns = CreateColumnDefinitions();
                System.Diagnostics.Debug.WriteLine($"✅ Vytvorených {columns.Count} stĺpcov");
                foreach (var col in columns)
                {
                    System.Diagnostics.Debug.WriteLine($"   📏 {col.Name} - {col.Header} (Width: {col.Width})");
                }

                UpdateLoadingState("Nastavujú sa validačné pravidlá...", "Definujú sa validačné pravidlá...");

                // KROK 2: Definícia validačných pravidiel s debug
                System.Diagnostics.Debug.WriteLine("✅ Vytváram validačné pravidlá...");
                var validationRules = CreateValidationRules();
                System.Diagnostics.Debug.WriteLine($"✅ Vytvorených {validationRules.Count} validačných pravidiel");

                UpdateLoadingState("Inicializuje sa DataGrid komponent...", "Pripájajú sa služby...");

                // KROK 3: Konfigurovateľný počet riadkov - NOVÁ FUNKCIONALITA
                int customRowCount = 25; // Môžete nastaviť ľubovoľný počet
                System.Diagnostics.Debug.WriteLine($"🔧 Nastavujem počet riadkov na: {customRowCount}");

                // Throttling config pre stabilitu
                var throttlingConfig = new ThrottlingConfig
                {
                    TypingDelayMs = 500,
                    PasteDelayMs = 200,
                    BatchValidationDelayMs = 300,
                    MaxConcurrentValidations = 3,
                    IsEnabled = true
                };

                // DEBUG: Kontrola DataGridControl pred inicializáciou
                if (DataGridControl == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ CHYBA: DataGridControl je NULL!");
                    ShowError("DataGridControl nie je dostupný");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("🔧 Spúšťam InitializeAsync...");

                // KĽÚČOVÁ OPRAVA: Explicit inicializácia s custom počtom riadkov
                await DataGridControl.InitializeAsync(columns, validationRules, throttlingConfig, customRowCount);

                System.Diagnostics.Debug.WriteLine("✅ InitializeAsync dokončený");

                // DEBUG: Kontrola komponentu po inicializácii pomocou nových metód
                if (DataGridControl.IsInitialized())
                {
                    System.Diagnostics.Debug.WriteLine("✅ Komponent je inicializovaný");
                    System.Diagnostics.Debug.WriteLine($"📊 Počet stĺpcov: {DataGridControl.GetColumnCount()}");
                    System.Diagnostics.Debug.WriteLine($"📊 Počet riadkov: {DataGridControl.GetRowCount()}");

                    // Výpis názvov stĺpcov
                    var columnNames = DataGridControl.GetColumnNames();
                    System.Diagnostics.Debug.WriteLine($"📋 Stĺpce: {string.Join(", ", columnNames)}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("❌ CHYBA: Komponent nie je inicializovaný!");
                    ShowError("Komponent sa neinicializoval správne");
                    return;
                }

                UpdateLoadingState("Načítavajú sa testové dáta...", "Vytváraju sa ukážkové záznamy...");

                // KROK 4: Načítanie testovacích dát
                System.Diagnostics.Debug.WriteLine("📊 Načítavam testové dáta...");
                await LoadTestDataAsync();

                // DEBUG: Kontrola dát po načítaní
                System.Diagnostics.Debug.WriteLine($"✅ Dáta načítané. Dátových riadkov: {DataGridControl.GetDataRowCount()}");
                System.Diagnostics.Debug.WriteLine($"📊 Celkovo riadkov: {DataGridControl.GetRowCount()}");

                // KROK 5: Dokončenie inicializácie
                CompleteInitialization();

                System.Diagnostics.Debug.WriteLine("🎉 Inicializácia ÚSPEŠNE dokončená!");

                // KOMPLETNÝ DEBUG OUTPUT
                System.Diagnostics.Debug.WriteLine("📋 FINÁLNY STAV KOMPONENTA:");
                System.Diagnostics.Debug.WriteLine(DataGridControl.GetDebugInfo());
                System.Diagnostics.Debug.WriteLine(DataGridControl.GetColumnsInfo());

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ KRITICKÁ CHYBA pri inicializácii: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"❌ Stack trace: {ex.StackTrace}");

                ShowError($"Chyba pri inicializácii: {ex.Message}");
            }
        }

        /// <summary>
        /// NOVÁ FUNKCIONALITA: Konfigurovateľné vytvorenie stĺpcov
        /// Môžete upraviť podľa potreby vašej aplikácie
        /// </summary>
        private List<ColumnDefinition> CreateColumnDefinitions()
        {
            // KONFIGUROVATEĽNÉ STĹPCE - môžete zmeniť podľa potreby
            var columns = new List<ColumnDefinition>
            {
                new("ID", typeof(int))
                {
                    MinWidth = 60,
                    MaxWidth = 100,
                    Width = 80,
                    Header = "🔢 ID",
                    ToolTip = "Jedinečný identifikátor záznamu",
                    IsReadOnly = true
                },
                new("Meno", typeof(string))
                {
                    MinWidth = 120,
                    MaxWidth = 250,
                    Width = 180,
                    Header = "👤 Meno a Priezvisko",
                    ToolTip = "Zadajte celé meno osoby"
                },
                new("Email", typeof(string))
                {
                    MinWidth = 180,
                    MaxWidth = 350,
                    Width = 250,
                    Header = "📧 Email adresa",
                    ToolTip = "Platná email adresa v správnom formáte"
                },
                new("Vek", typeof(int))
                {
                    MinWidth = 60,
                    MaxWidth = 100,
                    Width = 80,
                    Header = "🎂 Vek",
                    ToolTip = "Vek v rokoch (18-100)"
                },
                new("Plat", typeof(decimal))
                {
                    MinWidth = 100,
                    MaxWidth = 180,
                    Width = 140,
                    Header = "💰 Plat (€)",
                    ToolTip = "Mesačný plat v eurách"
                },
                new("Pozicia", typeof(string))
                {
                    MinWidth = 120,
                    MaxWidth = 200,
                    Width = 160,
                    Header = "💼 Pracovná pozícia",
                    ToolTip = "Aktuálna pracovná pozícia"
                }
            };

            return columns;
        }

        /// <summary>
        /// KONFIGUROVATEĽNÉ validačné pravidlá
        /// </summary>
        private List<ValidationRule> CreateValidationRules()
        {
            var rules = new List<ValidationRule>
            {
                // ID - povinné a jedinečné
                new ValidationRule("ID",
                    (value, row) => value != null && int.TryParse(value.ToString(), out int id) && id > 0,
                    "ID musí byť kladné číslo")
                {
                    RuleName = "ID_Required"
                },

                // Meno - povinné
                new ValidationRule("Meno",
                    (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
                    "Meno je povinné pole")
                {
                    RuleName = "Meno_Required"
                },

                // Email - formát
                new ValidationRule("Email",
                    (value, row) =>
                    {
                        var email = value?.ToString();
                        return string.IsNullOrEmpty(email) || (email.Contains("@") && email.Contains(".") && email.Length > 5);
                    },
                    "Email musí mať platný formát (@, . a min 5 znakov)")
                {
                    RuleName = "Email_Format"
                },

                // Vek - rozsah
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

                // Plat - rozsah
                new ValidationRule("Plat",
                    (value, row) =>
                    {
                        if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                            return true;

                        if (decimal.TryParse(value.ToString(), out decimal salary))
                            return salary >= 500 && salary <= 15000;

                        return false;
                    },
                    "Plat musí byť medzi 500-15000 €")
                {
                    RuleName = "Plat_Range"
                },

                // Pozícia - voliteľná ale ak je zadaná, min dĺžka
                new ValidationRule("Pozicia",
                    (value, row) =>
                    {
                        var pozicia = value?.ToString();
                        return string.IsNullOrEmpty(pozicia) || pozicia.Length >= 3;
                    },
                    "Pozícia musí mať aspoň 3 znaky")
                {
                    RuleName = "Pozicia_MinLength"
                }
            };

            return rules;
        }

        /// <summary>
        /// ROZŠÍRENÉ testové dáta s novými stĺpcami
        /// </summary>
        private async Task LoadTestDataAsync()
        {
            try
            {
                var dataTable = new DataTable();

                // Pridanie stĺpcov podľa definícií
                dataTable.Columns.Add("ID", typeof(int));
                dataTable.Columns.Add("Meno", typeof(string));
                dataTable.Columns.Add("Email", typeof(string));
                dataTable.Columns.Add("Vek", typeof(int));
                dataTable.Columns.Add("Plat", typeof(decimal));
                dataTable.Columns.Add("Pozicia", typeof(string));

                // ROZŠÍRENÉ testové dáta
                var testData = new object[][]
                {
                    new object[] { 1, "Ján Novák", "jan.novak@example.com", 30, 2500.00m, "Programátor" },
                    new object[] { 2, "Mária Svoboda", "maria.svoboda@example.com", 28, 3200.00m, "Analytik" },
                    new object[] { 3, "Peter Kováč", "peter.kovac@example.com", 35, 4500.00m, "Team Lead" },
                    new object[] { 4, "Anna Horváthová", "anna.horvath@example.com", 32, 3800.00m, "Designer" },
                    new object[] { 5, "Tomáš Varga", "tomas.varga@example.com", 27, 2800.00m, "Junior Dev" },
                    new object[] { 6, "Lucia Mrázová", "lucia.mrazova@example.com", 29, 3500.00m, "Tester" },
                    new object[] { 7, "Michal Novotný", "michal.novotny@example.com", 33, 4200.00m, "Architekt" },
                    
                    // Nevalidné dáta na testovanie validácií
                    new object[] { 0, "", "invalid-email", 15, 200.00m, "X" },  // Všetko nevalidné
                    new object[] { 9, "Test User", "", 150, 50000.00m, "" }    // Vysoký vek a plat
                };

                foreach (var rowData in testData)
                {
                    var row = dataTable.NewRow();
                    row.ItemArray = rowData;
                    dataTable.Rows.Add(row);
                }

                System.Diagnostics.Debug.WriteLine($"📊 Vytvorený DataTable s {dataTable.Rows.Count} riadkami a {dataTable.Columns.Count} stĺpcami");

                // Načítanie do DataGrid
                await DataGridControl.LoadDataAsync(dataTable);

                System.Diagnostics.Debug.WriteLine("✅ Testové dáta úspešne načítané do DataGrid");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Chyba pri načítaní testových dát: {ex.Message}");
                throw;
            }
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
                    var columnCount = DataGridControl.GetColumnCount();
                    var rowCount = DataGridControl.GetDataRowCount();
                    StatusTextBlock.Text = $"DataGrid pripravený: {columnCount} stĺpcov, {rowCount} dátových riadkov";
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

                await LoadTestDataAsync();

                if (StatusTextBlock != null)
                {
                    var dataRows = DataGridControl.GetDataRowCount();
                    StatusTextBlock.Text = $"Ukážkové dáta načítané - {dataRows} dátových riadkov";
                }

                System.Diagnostics.Debug.WriteLine($"✅ TEST úspešný: {DataGridControl.GetDataRowCount()} dátových riadkov");
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