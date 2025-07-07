// MainWindow.xaml.cs - DEMO APLIKÁCIA S OPRAVENÝMI EVENTS PRE WINDOW
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using RpaWinUiComponents.AdvancedWinUiDataGrid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

// LOKÁLNE ALIASY pre zamedzenie CS0104 chýb  
using LocalColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;
using LocalValidationRule = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ValidationRule;
using LocalThrottlingConfig = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ThrottlingConfig;

namespace RpaWinUiComponents.Demo
{
    /// <summary>
    /// Demo aplikácia pre testovanie RpaWinUiComponents balíka
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly ILogger<MainWindow> _logger;
        private readonly IDemoDataService _demoDataService;
        private bool _isInitialized = false;

        public MainWindow()
        {
            this.InitializeComponent();

            // Získaj služby z DI kontajnera (ak sú dostupné)
            try
            {
                var app = Application.Current as App;
                var host = app?.GetHost();

                _logger = host?.Services?.GetService<ILogger<MainWindow>>()
                         ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MainWindow>.Instance;

                _demoDataService = host?.Services?.GetService<IDemoDataService>()
                                 ?? new DemoDataService(_logger as ILogger<DemoDataService>
                                                       ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<DemoDataService>.Instance);
            }
            catch
            {
                _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<MainWindow>.Instance;
                _demoDataService = new DemoDataService(Microsoft.Extensions.Logging.Abstractions.NullLogger<DemoDataService>.Instance);
            }

            // OPRAVA CS1061: Window nemá Loaded event, použijeme Activated
            this.Activated += OnMainWindowActivated;
        }

        private async void OnMainWindowActivated(object sender, WindowActivatedEventArgs e)
        {
            // Inicializuj len raz
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                UpdateStatus("Inicializuje sa DataGrid komponent...");
                InitStatusText.Text = " - Inicializuje sa...";

                await InitializeDataGridAsync();

                InitStatusText.Text = " - Pripravené";
                InitStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Green);
                UpdateStatus("DataGrid komponent je pripravený na použitie");

                ShowDataGridAndHideLoading();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MainWindow initialization");
                InitStatusText.Text = " - Chyba pri inicializácii";
                InitStatusText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                UpdateStatus($"Chyba: {ex.Message}");
            }
        }

        private async Task InitializeDataGridAsync()
        {
            try
            {
                LoadingDetailText.Text = "Definujú sa stĺpce...";

                // OPRAVA: Použitie LocalColumnDefinition aliasu
                var columns = new List<LocalColumnDefinition>
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
                    },
                    new("Pozicia", typeof(string))
                    {
                        MinWidth = 120,
                        MaxWidth = 200,
                        Width = 150,
                        Header = "💼 Pozícia",
                        ToolTip = "Pracovná pozícia v spoločnosti"
                    },
                    new("Aktívny", typeof(bool))
                    {
                        MinWidth = 80,
                        MaxWidth = 100,
                        Width = 90,
                        Header = "✅ Aktívny",
                        ToolTip = "Či je zamestnanec aktívny"
                    }
                };

                LoadingDetailText.Text = "Nastavujú sa validačné pravidlá...";

                // OPRAVA: Použitie LocalValidationRule aliasu a helper metód
                var validationRules = new List<LocalValidationRule>
                {
                    // Použitie helper metód z Validation triedy
                    AdvancedWinUiDataGridControl.Validation.Required("Meno", "Meno je povinné pole"),
                    AdvancedWinUiDataGridControl.Validation.Length("Meno", 2, 50, "Meno musí mať 2-50 znakov"),

                    AdvancedWinUiDataGridControl.Validation.Email("Email", "Zadajte platný email formát"),

                    AdvancedWinUiDataGridControl.Validation.Numeric("Vek", "Vek musí byť číslo"),
                    AdvancedWinUiDataGridControl.Validation.Range("Vek", 18, 100, "Vek musí byť medzi 18-100 rokmi"),

                    AdvancedWinUiDataGridControl.Validation.Numeric("Plat", "Plat musí byť číslo"),
                    AdvancedWinUiDataGridControl.Validation.Range("Plat", 500, 10000, "Plat musí byť medzi 500-10000 €"),
                    
                    // Podmienená validácia - telefón je povinný pre manažérov
                    AdvancedWinUiDataGridControl.Validation.Conditional("Email",
                        (value, row) => !string.IsNullOrEmpty(value?.ToString()),
                        row => row.GetValue<string>("Pozicia")?.Contains("Manager") == true,
                        "Email je povinný pre manažérov",
                        "Email_Required_For_Managers")
                };

                LoadingDetailText.Text = "Inicializuje sa komponent...";

                // OPRAVA: Použitie LocalThrottlingConfig aliasu
                var throttlingConfig = LocalThrottlingConfig.Default;

                await DataGridControl.InitializeAsync(columns, validationRules, throttlingConfig, initialRowCount: 50);

                _logger.LogInformation("DataGrid initialized successfully with {ColumnCount} columns and {RuleCount} validation rules",
                    columns.Count, validationRules.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing DataGrid");
                throw;
            }
        }

        #region Event Handlers

        private async void OnLoadSampleDataClick(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Načítavajú sa ukážkové dáta...");

                var sampleData = CreateSampleData();
                await DataGridControl.LoadDataAsync(sampleData);

                UpdateStatus($"Načítaných {sampleData.Count} ukážkových záznamov s automatickou validáciou");
                _logger.LogInformation("Sample data loaded: {RecordCount} records", sampleData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sample data");
                UpdateStatus($"Chyba pri načítavaní dát: {ex.Message}");
            }
        }

        private async void OnValidateAllClick(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Spúšťa sa validácia všetkých riadkov...");

                var isValid = await DataGridControl.ValidateAllRowsAsync();

                if (isValid)
                {
                    UpdateStatus("✅ Všetky riadky sú validné!");
                }
                else
                {
                    UpdateStatus("❌ Nájdené boli validačné chyby. Skontrolujte označené bunky.");
                }

                _logger.LogInformation("Validation completed: {IsValid}", isValid);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during validation");
                UpdateStatus($"Chyba pri validácii: {ex.Message}");
            }
        }

        private async void OnClearDataClick(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Vymazávajú sa všetky dáta...");

                await DataGridControl.ClearAllDataAsync();

                UpdateStatus("Všetky dáta boli vymazané");
                _logger.LogInformation("All data cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing data");
                UpdateStatus($"Chyba pri vymazávaní: {ex.Message}");
            }
        }

        private async void OnExportDataClick(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Exportujú sa dáta...");

                var dataTable = await DataGridControl.ExportToDataTableAsync();

                UpdateStatus($"Export dokončený: {dataTable.Rows.Count} validných riadkov, {dataTable.Columns.Count} stĺpcov");
                _logger.LogInformation("Data exported: {RowCount} rows, {ColumnCount} columns",
                    dataTable.Rows.Count, dataTable.Columns.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting data");
                UpdateStatus($"Chyba pri exporte: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private List<Dictionary<string, object?>> CreateSampleData()
        {
            return new List<Dictionary<string, object?>>
            {
                new()
                {
                    ["Meno"] = "Ján Novák",
                    ["Email"] = "jan.novak@example.com",
                    ["Vek"] = 30,
                    ["Plat"] = 2500.00m,
                    ["Pozicia"] = "Developer",
                    ["Aktívny"] = true
                },
                new()
                {
                    ["Meno"] = "Mária Svoboda",
                    ["Email"] = "maria.svoboda@example.com",
                    ["Vek"] = 28,
                    ["Plat"] = 3200.00m,
                    ["Pozicia"] = "Senior Developer",
                    ["Aktívny"] = true
                },
                new()
                {
                    ["Meno"] = "Peter Kováč",
                    ["Email"] = "peter.kovac@example.com",
                    ["Vek"] = 35,
                    ["Plat"] = 4500.00m,
                    ["Pozicia"] = "Project Manager",
                    ["Aktívny"] = true
                },
                new()
                {
                    ["Meno"] = "Anna Horváthová",
                    ["Email"] = "anna.horvath@example.com",
                    ["Vek"] = 32,
                    ["Plat"] = 3800.00m,
                    ["Pozicia"] = "Team Lead",
                    ["Aktívny"] = false
                },
                new()
                {
                    ["Meno"] = "", // Nevalidný - prázdne meno
                    ["Email"] = "invalid-email", // Nevalidný email
                    ["Vek"] = 15, // Nevalidný vek (mladší ako 18)
                    ["Plat"] = 200.00m, // Nevalidný plat (menej ako 500)
                    ["Pozicia"] = "Intern",
                    ["Aktívny"] = true
                },
                new()
                {
                    ["Meno"] = "Test Manager",
                    ["Email"] = "", // Nevalidný - prázdny email pre manažéra
                    ["Vek"] = 45,
                    ["Plat"] = 5500.00m,
                    ["Pozicia"] = "Senior Manager", // Podmienená validácia
                    ["Aktívny"] = true
                }
            };
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = message;
            _logger.LogDebug("Status updated: {Message}", message);
        }

        private void ShowDataGridAndHideLoading()
        {
            LoadingPanel.Visibility = Visibility.Collapsed;
            DataGridControl.Visibility = Visibility.Visible;
        }

        #endregion
    }

    /// <summary>
    /// Extension metóda pre prístup k Host z App
    /// </summary>
    public static class AppExtensions
    {
        public static Microsoft.Extensions.Hosting.IHost? GetHost(this App app)
        {
            return app.GetType()
                .GetField("_host", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(app) as Microsoft.Extensions.Hosting.IHost;
        }
    }
}