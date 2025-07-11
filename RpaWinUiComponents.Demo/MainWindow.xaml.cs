// MainWindow.xaml.cs - FINÁLNA OPRAVA - používa PUBLIC API s novým namespace
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// ✅ FINÁLNA OPRAVA: Demo projekt používa PUBLIC API triedy s novým namespace
using RpaWinUiComponents.AdvancedWinUiDataGrid;
using PublicColumnDefinition = RpaWinUiComponents.PublicApi.ColumnDefinition;
using PublicValidationRule = RpaWinUiComponents.PublicApi.ValidationRule;
using PublicThrottlingConfig = RpaWinUiComponents.PublicApi.ThrottlingConfig;

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

                // KROK 1: Definícia stĺpcov - POUŽÍVAME PUBLIC API s novým namespace
                System.Diagnostics.Debug.WriteLine("📊 Vytváram definície stĺpcov...");
                var columns = CreateColumnDefinitions();
                System.Diagnostics.Debug.WriteLine($"✅ Vytvorených {columns.Count} stĺpcov");

                UpdateLoadingState("Nastavujú sa validačné pravidlá...", "Definujú sa validačné pravidlá...");

                // KROK 2: Definícia validačných pravidiel - POUŽÍVAME PUBLIC API s novým namespace
                System.Diagnostics.Debug.WriteLine("✅ Vytváram validačné pravidlá...");
                var validationRules = CreateValidationRules();
                System.Diagnostics.Debug.WriteLine($"✅ Vytvorených {validationRules.Count} validačných pravidiel");

                UpdateLoadingState("Inicializuje sa DataGrid komponent...", "Pripájajú sa služby...");

                // KROK 3: OPRAVA - Konfigurovateľný počet riadkov s DEFAULT 25
                int customRowCount = 25;
                System.Diagnostics.Debug.WriteLine($"🔧 Nastavujem počet riadkov na: {customRowCount}");

                // Throttling config pre stabilitu - POUŽÍVAME PUBLIC API s novým namespace
                var throttlingConfig = new PublicThrottlingConfig
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

                // KĽÚČOVÁ OPRAVA: Používanie PUBLIC API s novým namespace
                await DataGridControl.InitializeAsync(columns, validationRules, throttlingConfig, customRowCount);

                System.Diagnostics.Debug.WriteLine("✅ InitializeAsync dokončený");

                UpdateLoadingState("Načítavajú sa testové dáta...", "Vytváraju sa ukážkové záznamy...");

                // KROK 4: Načítanie testovacích dát
                System.Diagnostics.Debug.WriteLine("📊 Načítavam testové dáta...");
                await LoadTestDataAsync();

                System.Diagnostics.Debug.WriteLine("✅ Dáta načítané úspešne");

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
        /// Konfigurovateľné vytvorenie stĺpcov - POUŽÍVA PUBLIC API s novým namespace
        /// </summary>
        private List<PublicColumnDefinition> CreateColumnDefinitions()
        {
            var columns = new List<PublicColumnDefinition>
            {
                new PublicColumnDefinition("ID", typeof(int))
                {
                    MinWidth = 60,
                    MaxWidth = 100,
                    Width = 80,
                    Header = "🔢 ID",
                    ToolTip = "Jedinečný identifikátor záznamu",
                    IsReadOnly = true
                },
                new PublicColumnDefinition("Meno", typeof(string))
                {
                    MinWidth = 120,
                    MaxWidth = 250,
                    Width = 180,
                    Header = "👤 Meno a Priezvisko",
                    ToolTip = "Zadajte celé meno osoby"
                },
                new PublicColumnDefinition("Email", typeof(string))
                {
                    MinWidth = 180,
                    MaxWidth = 350,
                    Width = 250,
                    Header = "📧 Email adresa",
                    ToolTip = "Platná email adresa v správnom formáte"
                },
                new PublicColumnDefinition("Vek", typeof(int))
                {
                    MinWidth = 60,
                    MaxWidth = 100,
                    Width = 80,
                    Header = "🎂 Vek",
                    ToolTip = "Vek v rokoch (18-100)"
                },
                new PublicColumnDefinition("Plat", typeof(decimal))
                {
                    MinWidth = 100,
                    MaxWidth = 180,
                    Width = 140,
                    Header = "💰 Plat (€)",
                    ToolTip = "Mesačný plat v eurách"
                },
                new PublicColumnDefinition("Pozicia", typeof(string))
                {
                    MinWidth = 120,
                    MaxWidth = 200,
                    Width = 160,
                    Header = "💼 Pracovná pozícia",
                    ToolTip = "Aktuálna pracovná pozícia"
                },
                new PublicColumnDefinition("Oddelenie", typeof(string))
                {
                    MinWidth = 100,
                    MaxWidth = 180,
                    Width = 140,
                    Header = "🏢 Oddelenie",
                    ToolTip = "Oddelenie v spoločnosti"
                },
                new PublicColumnDefinition("DatumNastupu", typeof(DateTime))
                {
                    MinWidth = 120,
                    MaxWidth = 160,
                    Width = 140,
                    Header = "📅 Dátum nástupu",
                    ToolTip = "Dátum nástupu do práce"
                }
            };

            return columns;
        }

        /// <summary>
        /// Rozšírené validačné pravidlá - POUŽÍVA PUBLIC API s novým namespace
        /// </summary>
        private List<PublicValidationRule> CreateValidationRules()
        {
            var rules = new List<PublicValidationRule>();

            // ✅ 1. ZÁKLADNÉ POMOCNÉ VALIDÁCIE - používame static helper metódy z PUBLIC API
            rules.Add(PublicValidationRule.Required("ID", "ID je povinné pole"));
            rules.Add(PublicValidationRule.Required("Meno", "Meno je povinné pole"));
            rules.Add(PublicValidationRule.Email("Email", "Email musí mať platný formát"));
            rules.Add(PublicValidationRule.Range("Vek", 18, 100, "Vek musí byť medzi 18-100 rokmi"));
            rules.Add(PublicValidationRule.Range("Plat", 500, 15000, "Plat musí byť medzi 500-15000 €"));
            rules.Add(PublicValidationRule.Length("Pozicia", 0, 50, "Pozícia môže mať max 50 znakov"));

            // 🎯 2. CUSTOM VALIDÁCIA - Kontrola dĺžky mena s PUBLIC API
            var nameRule = new PublicValidationRule("Meno", (value, row) =>
            {
                var meno = value?.ToString() ?? "";
                var slova = meno.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return slova.Length >= 2 && slova.All(s => s.Length >= 2);
            }, "Meno musí obsahovať aspoň meno a priezvisko (min 2 znaky každé)")
            {
                RuleName = "Meno_CompleteNameValidation",
                Priority = 10
            };
            rules.Add(nameRule);

            // 🎯 3. CUSTOM VALIDÁCIA - Kontrola formátu ID s PUBLIC API
            var idRule = new PublicValidationRule("ID", (value, row) =>
            {
                if (int.TryParse(value?.ToString(), out int id))
                {
                    return id > 0 && id % 2 == 1; // ID musí byť kladné nepárne
                }
                return false;
            }, "ID musí byť kladné nepárne číslo")
            {
                RuleName = "ID_OddNumberValidation",
                Priority = 5
            };
            rules.Add(idRule);

            // 🎯 4. ASYNC VALIDÁCIA - Simulácia kontroly duplicitného emailu s PUBLIC API
            var asyncEmailRule = new PublicValidationRule()
            {
                ColumnName = "Email",
                RuleName = "Email_DuplicateCheckAsync",
                ErrorMessage = "Email už existuje v systéme",
                IsAsync = true,
                ValidationTimeout = TimeSpan.FromSeconds(5)
            };

            // Nastavenie async function cez property
            asyncEmailRule.AsyncValidationFunction = async (value, row, cancellationToken) =>
            {
                var email = value?.ToString() ?? "";
                if (string.IsNullOrEmpty(email)) return true;

                // Simulácia async kontroly
                await Task.Delay(500, cancellationToken);

                var forbiddenEmails = new[]
                {
                    "admin@example.com",
                    "test@example.com",
                    "duplicate@company.sk"
                };

                return !forbiddenEmails.Contains(email.ToLower());
            };
            rules.Add(asyncEmailRule);

            // 🎯 5. CUSTOM DÁTUM VALIDÁCIA s PUBLIC API
            var dateRule = new PublicValidationRule("DatumNastupu", (value, row) =>
            {
                if (value == null) return true;

                if (DateTime.TryParse(value.ToString(), out DateTime datum))
                {
                    var dnes = DateTime.Now.Date;
                    var pred5Rokmi = dnes.AddYears(-5);
                    var za1Rok = dnes.AddYears(1);
                    return datum >= pred5Rokmi && datum <= za1Rok;
                }
                return false;
            }, "Dátum nástupu môže byť max 5 rokov v minulosti alebo 1 rok v budúcnosti")
            {
                RuleName = "DatumNastupu_RangeValidation",
                Priority = 5
            };
            rules.Add(dateRule);

            System.Diagnostics.Debug.WriteLine($"📋 Vytvorených {rules.Count} validačných pravidiel:");
            foreach (var rule in rules.OrderByDescending(r => r.Priority))
            {
                System.Diagnostics.Debug.WriteLine($"   ✅ {rule.RuleName} (Priority: {rule.Priority}) - {rule.ErrorMessage}");
            }

            return rules;
        }

        /// <summary>
        /// Rozšírené testové dáta s validačnými scenármi
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
                dataTable.Columns.Add("Oddelenie", typeof(string));
                dataTable.Columns.Add("DatumNastupu", typeof(DateTime));

                // Testové dáta s validačnými scenármi
                var testData = new object[][]
                {
                    // ✅ VALIDNÉ ZÁZNAMY
                    new object[] { 1, "Ján Novák", "jan.novak@example.com", 30, 2500.00m, "Programátor", "IT", DateTime.Now.AddYears(-2) },
                    new object[] { 3, "Mária Svoboda", "maria.svoboda@company.sk", 28, 3200.00m, "Senior Analytik", "IT", DateTime.Now.AddYears(-1) },
                    new object[] { 5, "Peter Kováč", "peter.kovac@firma.sk", 35, 4500.00m, "Team Lead", "IT", DateTime.Now.AddYears(-3) },
                    new object[] { 7, "Anna Horváthová", "anna.horvath@example.com", 32, 3800.00m, "Designer", "Marketing", DateTime.Now.AddMonths(-8) },
                    new object[] { 9, "Tomáš Varga", "tomas.varga@test.com", 24, 2000.00m, "Junior Programátor", "IT", DateTime.Now.AddMonths(-6) },

                    // ❌ NEVALIDNÉ ZÁZNAMY - na testovanie validácií
                    new object[] { 2, "Lucia", "lucia@gmail.com", 15, 200.00m, "X", "Unknown", DateTime.Now.AddYears(-10) },
                    new object[] { 4, "Michal Novotný", "admin@example.com", 22, 5000.00m, "Senior Architekt", "IT", DateTime.Now.AddYears(2) },
                    new object[] { 6, "", "invalid-email", 150, 50000.00m, "HR Manažér", "IT", DateTime.Now.AddYears(-20) },
                    new object[] { 8, "Test User Name", "test@example.com", 55, 1500.00m, "Programátor", "Finance", DateTime.Now.AddMonths(18) },
                    new object[] { 11, "Junior Developer", "duplicate@company.sk", 30, 1800.00m, "Senior Lead", "Sales", DateTime.Now.AddDays(-1) }
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

                await LoadTestDataAsync();

                if (StatusTextBlock != null)
                {
                    StatusTextBlock.Text = "Ukážkové dáta načítané - 10 záznamov (5 validných, 5 nevalidných)";
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
                    StatusTextBlock.Text = isValid ? "Všetky dáta sú validné" : "Nájdené validačné chyby (očakávané pre demo)";

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