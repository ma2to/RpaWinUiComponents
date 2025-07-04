# 🚀 RpaWinUiComponents - Advanced DataGrid

**Pokročilý WinUI 3 DataGrid komponent s real-time validáciou, copy/paste funkcionalitou a Clean Architecture.**

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![WinUI 3](https://img.shields.io/badge/WinUI-3.0-green.svg)](https://docs.microsoft.com/en-us/windows/apps/winui/)
[![C#](https://img.shields.io/badge/C%23-11.0-purple.svg)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## 📋 Obsah

- [🌟 Kľúčové funkcie](#-kľúčové-funkcie)
- [🏗️ Architektúra](#️-architektúra)
- [📦 Inštalácia](#-inštalácia)
- [🚀 Rýchly štart](#-rýchly-štart)
- [📖 Pokročilé použitie](#-pokročilé-použitie)
- [🎮 Ovládanie](#-ovládanie)
- [🧪 Testovanie](#-testovanie)
- [🔧 Konfigurácia](#-konfigurácia)
- [📝 API dokumentácia](#-api-dokumentácia)
- [🤝 Prispievanie](#-prispievanie)

## 🌟 Kľúčové funkcie

### ✅ Implementované funkcionality

- **🏛️ Clean Architecture** s Dependency Injection a MVVM
- **⚡ Real-time validácie** s throttling podporou (100-500ms)
- **📋 Copy/Paste** funkcionalita kompatibilná s Excel formátom
- **🔤 Keyboard Navigation** (Tab, Enter, F2, ESC, Delete, Shift+Enter)
- **📊 Flexibilné stĺpce** s konfigurovateľnými typmi a validáciami
- **🎯 Špeciálne stĺpce** (DeleteAction, ValidAlerts) s automatickým umiestnením
- **📤 Export/Import** dát (DataTable, Dictionary, CSV)
- **🔍 Custom validačné pravidlá** s podmienečnou aplikáciou
- **🎨 Moderné WinUI 3 dizajn** s Visual States a animáciami
- **📱 Responsive design** s podporou rôznych veľkostí obrazovky
- **♿ Accessibility** podpora s správnym focus managementom
- **🚀 Vysoký výkon** vďaka ItemsRepeater a virtualizácii
- **🔧 Extensibility** cez interfaces a dependency injection

### 🎮 Ovládanie klávesnicou

| Klávesa | Funkcia |
|---------|---------|
| `Tab` / `Shift+Tab` | Navigácia medzi bunkami |
| `Enter` | Potvrdenie a prechod na ďalší riadok |
| `F2` | Začatie editácie bunky |
| `ESC` | Zrušenie zmien |
| `Delete` | Vymazanie obsahu bunky |
| `Shift+Enter` | Nový riadok v bunke |
| `Ctrl+C` | Kopírovanie |
| `Ctrl+V` | Vkladanie |

## 🏗️ Architektúra

```
RpaWinUiComponents/
├── AdvancedWinUiDataGrid/           # 🏠 Hlavný komponent
│   ├── Views/                       # 🖼️ UserControls a XAML
│   │   ├── AdvancedDataGridControl.xaml
│   │   └── AdvancedDataGridControl.xaml.cs
│   ├── ViewModels/                  # 🧠 Business Logic (MVVM)
│   │   └── AdvancedDataGridViewModel.cs
│   ├── Models/                      # 📊 Dátové modely
│   │   ├── ColumnDefinition.cs
│   │   ├── DataGridRow.cs
│   │   ├── DataGridCell.cs
│   │   ├── ValidationRule.cs
│   │   ├── ValidationResult.cs
│   │   └── ThrottlingConfig.cs
│   ├── Services/                    # ⚙️ Business Services
│   │   ├── Interfaces/
│   │   └── Implementation/
│   │       ├── DataService.cs       # 📊 Data management
│   │       ├── ValidationService.cs # ✅ Validation logic
│   │       ├── ClipboardService.cs  # 📋 Copy/Paste
│   │       ├── ColumnService.cs     # 📏 Column management
│   │       ├── ExportService.cs     # 📤 Export/Import
│   │       └── NavigationService.cs # 🧭 Keyboard navigation
│   ├── Commands/                    # 🎯 Command Pattern
│   │   ├── RelayCommand.cs
│   │   └── AsyncRelayCommand.cs
│   ├── Converters/                  # 🔄 Data Binding Converters
│   ├── Controls/                    # 🎛️ Custom Controls
│   │   └── EditableTextBlock.cs    # ✏️ In-place editing
│   ├── Events/                      # 📢 Event Args
│   ├── Helpers/                     # 🛠️ Utility classes
│   ├── Collections/                 # 📦 Specialized collections
│   │   └── ObservableRangeCollection.cs
│   └── Configuration/               # ⚙️ DI & Setup
│       ├── DependencyInjectionConfig.cs
│       └── ServiceCollectionExtensions.cs
├── Themes/                          # 🎨 XAML Styles & Resources
│   └── Generic.xaml
└── build/                           # 📦 NuGet packaging
    └── RpaWinUiComponents.targets
```

### 🧩 Clean Architecture Layers

1. **Presentation Layer** - Views, ViewModels, Converters
2. **Application Layer** - Services, Commands, Events  
3. **Domain Layer** - Models, Validation Rules, Business Logic
4. **Infrastructure Layer** - Configuration, DI, Helpers

## 📦 Inštalácia

### Cez NuGet Package Manager

```powershell
Install-Package RpaWinUiComponents
```

### Cez .NET CLI

```bash
dotnet add package RpaWinUiComponents
```

### Cez PackageReference

```xml
<PackageReference Include="RpaWinUiComponents" Version="1.0.6" />
```

## 🚀 Rýchly štart

### 1. Základná konfigurácia v App.xaml.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using RpaWinUiComponents.AdvancedWinUiDataGrid;

public partial class App : Application
{
    private IHost? _host;

    public App()
    {
        this.InitializeComponent();
        InitializeServices();
    }

    private void InitializeServices()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .ConfigureServices((context, services) =>
            {
                // Registrácia služieb pre AdvancedWinUiDataGrid
                services.AddAdvancedWinUiDataGrid();
                
                // Vaše služby
                services.AddSingleton<MainWindow>();
            });

        _host = hostBuilder.Build();

        // Konfigurácia RpaWinUiComponents
        AdvancedWinUiDataGridControl.Configuration.ConfigureServices(_host.Services);
        
        var loggerFactory = _host.Services.GetRequiredService<ILoggerFactory>();
        AdvancedWinUiDataGridControl.Configuration.ConfigureLogging(loggerFactory);
        
        // Zapnutie debug loggu pre vývoj (voliteľné)
        AdvancedWinUiDataGridControl.Configuration.SetDebugLogging(true);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _host?.StartAsync();
        
        var window = _host?.Services.GetService<MainWindow>() ?? new MainWindow();
        window.Activate();
    }
}
```

### 2. Pridanie do XAML

```xml
<Window x:Class="YourApp.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="using:RpaWinUiComponents.AdvancedWinUiDataGrid">
    <Grid>
        <controls:AdvancedWinUiDataGridControl x:Name="DataGridControl" />
    </Grid>
</Window>
```

### 3. Základné použitie v code-behind

```csharp
using RpaWinUiComponents.AdvancedWinUiDataGrid.Models;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        _ = InitializeDataGridAsync();
    }

    private async Task InitializeDataGridAsync()
    {
        try
        {
            // Definícia stĺpcov
            var columns = new List<ColumnDefinition>
            {
                new("Meno", typeof(string)) { MinWidth = 120, Header = "👤 Meno" },
                new("Email", typeof(string)) { MinWidth = 200, Header = "📧 Email" },
                new("Vek", typeof(int)) { MinWidth = 80, Header = "🎂 Vek" }
            };

            // Validačné pravidlá
            var validationRules = new List<ValidationRule>
            {
                new("Meno", (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
                    "Meno je povinné"),
                new("Email", (value, row) => 
                {
                    var email = value?.ToString();
                    return string.IsNullOrEmpty(email) || email.Contains("@");
                }, "Neplatný email formát"),
                new("Vek", (value, row) =>
                {
                    if (int.TryParse(value?.ToString(), out int age))
                        return age >= 18 && age <= 100;
                    return true; // Prázdne hodnoty sú OK
                }, "Vek musí byť medzi 18-100")
            };

            // Inicializácia komponentu
            await DataGridControl.InitializeAsync(columns, validationRules);
            
            // Načítanie ukážkových dát
            await LoadSampleDataAsync();
        }
        catch (Exception ex)
        {
            // Error handling
            System.Diagnostics.Debug.WriteLine($"Chyba: {ex.Message}");
        }
    }

    private async Task LoadSampleDataAsync()
    {
        var sampleData = new List<Dictionary<string, object?>>
        {
            new() { ["Meno"] = "Ján Novák", ["Email"] = "jan@example.com", ["Vek"] = 30 },
            new() { ["Meno"] = "Mária Svoboda", ["Email"] = "maria@example.com", ["Vek"] = 25 },
            new() { ["Meno"] = "", ["Email"] = "invalid-email", ["Vek"] = 15 } // Nevalidný riadok
        };

        await DataGridControl.LoadDataAsync(sampleData);
    }
}
```

## 📖 Pokročilé použitie

### Custom validačné pravidlá

```csharp
// Podmienené validačné pravidlo
var conditionalRule = new ValidationRule("Telefon", 
    (value, row) => !string.IsNullOrEmpty(value?.ToString()),
    "Telefón je povinný pre manažérov")
{
    ApplyCondition = row => row.GetValue<string>("Pozicia") == "Manager"
};

// Async validačné pravidlo (napr. kontrola v databáze)
var asyncRule = new ValidationRule("Email", (_, _) => true, "Email už existuje")
{
    IsAsync = true,
    AsyncValidationFunction = async (value, row, cancellationToken) =>
    {
        var email = value?.ToString();
        if (string.IsNullOrEmpty(email)) return true;
        
        // Simulácia async kontroly
        await Task.Delay(100, cancellationToken);
        return !email.Contains("test"); // Test emails nie sú povolené
    },
    ValidationTimeout = TimeSpan.FromSeconds(2)
};
```

### Throttling konfigurácia

```csharp
// Rýchla validácia pre jednoduché pravidlá
var fastThrottling = ThrottlingConfig.Fast; // 150ms delay

// Pomalá validácia pre zložité pravidlá
var slowThrottling = ThrottlingConfig.Slow; // 500ms delay

// Custom konfigurácia
var customThrottling = ThrottlingConfig.Custom(300, maxConcurrentValidations: 3);

await DataGridControl.InitializeAsync(columns, validationRules, customThrottling);
```

### Export dát

```csharp
// Export do DataTable
var dataTable = await DataGridControl.ExportToDataTableAsync();

// Validácia všetkých riadkov
var allValid = await DataGridControl.ValidateAllRowsAsync();

// Vymazanie dát
await DataGridControl.ClearAllDataAsync();

// Odstránenie prázdnych riadkov
await DataGridControl.RemoveEmptyRowsAsync();
```

### Helper metódy pre validáciu

```csharp
using static RpaWinUiComponents.AdvancedWinUiDataGrid.AdvancedWinUiDataGridControl;

// Použitie helper metód
var validationRules = new List<ValidationRule>
{
    Validation.Required("Meno"),
    Validation.Email("Email", "Zadajte platný email"),
    Validation.Range("Plat", 1000, 10000, "Plat musí byť 1000-10000 EUR"),
    Validation.Length("Poznámka", 0, 500, "Poznámka môže mať max 500 znakov"),
    Validation.Numeric("Vek", "Vek musí byť číslo")
};
```

## 🎮 Ovládanie

### Klávesové skratky

- **Tab/Shift+Tab** - Navigácia medzi bunkami
- **Enter** - Potvrdenie a prechod na ďalší riadok  
- **F2** - Začatie editácie aktuálnej bunky
- **ESC** - Zrušenie zmien v bunke
- **Delete** - Vymazanie obsahu bunky
- **Shift+Enter** - Nový riadok v bunke (multiline)
- **Ctrl+C** - Kopírovanie vybraných buniek
- **Ctrl+V** - Vkladanie zo schránky

### Myš

- **Single Click** - Výber bunky
- **Double Click** - Začatie editácie bunky  
- **Delete Button** - Vymazanie riadku (špeciálny stĺpec)

## 🧪 Testovanie

Projekt obsahuje demo aplikáciu pre testovanie všetkých funkcií:

```bash
git clone https://github.com/your-repo/RpaWinUiComponents
cd RpaWinUiComponents
dotnet run --project RpaWinUiComponents.Demo
```

### Testované scenáre

- ✅ Načítanie dát z DataTable a Dictionary
- ✅ Real-time validácie s rôznymi pravidlami
- ✅ Copy/Paste funkcionalita s Excel
- ✅ Keyboard navigation vo všetkých smeroch
- ✅ Export validných dát
- ✅ Performance testing s 1000+ riadkov
- ✅ Error handling a recovery

## 🔧 Konfigurácia

### Throttling nastavenia

```csharp
public class ThrottlingConfig
{
    public int TypingDelayMs { get; set; } = 300;      // Delay počas písania
    public int PasteDelayMs { get; set; } = 100;       // Delay po paste
    public int BatchValidationDelayMs { get; set; } = 200; // Batch validácia
    public int MaxConcurrentValidations { get; set; } = 5;  // Max súčasných validácií
    public bool IsEnabled { get; set; } = true;        // Zapnutie/vypnutie
    public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
```

### Logging konfigurácia

```csharp
// V App.xaml.cs
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddDebug();
    builder.SetMinimumLevel(LogLevel.Information);
});

// Konfigurácia pre komponent
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
AdvancedWinUiDataGridControl.Configuration.ConfigureLogging(loggerFactory);
AdvancedWinUiDataGridControl.Configuration.SetDebugLogging(true); // Pre development
```

## 📝 API dokumentácia

### Hlavné triedy

#### AdvancedWinUiDataGridControl
```csharp
public sealed partial class AdvancedWinUiDataGridControl : UserControl, IDisposable
{
    // Inicializácia
    Task InitializeAsync(List<ColumnDefinition> columns, List<ValidationRule>? validationRules = null, 
                        ThrottlingConfig? throttling = null, int initialRowCount = 100);
    
    // Načítanie dát
    Task LoadDataAsync(DataTable dataTable);
    Task LoadDataAsync(List<Dictionary<string, object?>> data);
    
    // Export
    Task<DataTable> ExportToDataTableAsync();
    
    // Validácia
    Task<bool> ValidateAllRowsAsync();
    
    // Manipulácia
    Task ClearAllDataAsync();
    Task RemoveEmptyRowsAsync();
    void Reset();
    
    // Events
    event EventHandler<ComponentErrorEventArgs>? ErrorOccurred;
}
```

#### ColumnDefinition
```csharp
public class ColumnDefinition
{
    public string Name { get; set; }
    public Type DataType { get; set; }
    public double MinWidth { get; set; } = 80;
    public double MaxWidth { get; set; } = 300;
    public double Width { get; set; } = 150;
    public bool AllowResize { get; set; } = true;
    public bool AllowSort { get; set; } = true;
    public bool IsReadOnly { get; set; } = false;
    public string? Header { get; set; }
    public string? ToolTip { get; set; }
}
```

#### ValidationRule  
```csharp
public class ValidationRule
{
    public string ColumnName { get; set; }
    public Func<object?, DataGridRow, bool> ValidationFunction { get; set; }
    public string ErrorMessage { get; set; }
    public Func<DataGridRow, bool> ApplyCondition { get; set; } = _ => true;
    public int Priority { get; set; } = 0;
    public string RuleName { get; set; }
    public bool IsAsync { get; set; } = false;
    public Func<object?, DataGridRow, CancellationToken, Task<bool>>? AsyncValidationFunction { get; set; }
    public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(5);
}
```

### Helper metódy

```csharp
// Validation helpers
public static class Validation
{
    public static ValidationRule Required(string columnName, string? errorMessage = null);
    public static ValidationRule Length(string columnName, int minLength, int maxLength = int.MaxValue, string? errorMessage = null);
    public static ValidationRule Range(string columnName, double min, double max, string? errorMessage = null);
    public static ValidationRule Email(string columnName, string? errorMessage = null);
    public static ValidationRule Numeric(string columnName, string? errorMessage = null);
    public static ValidationRule Conditional(string columnName, Func<object?, GridDataRow, bool> validationFunction, 
                                           Func<GridDataRow, bool> condition, string errorMessage, string? ruleName = null);
}
```

## 🚨 Riešenie problémov

### Časté problémy

1. **"Component must be initialized first"**
   ```csharp
   // Najprv zavolajte InitializeAsync
   await DataGridControl.InitializeAsync(columns, validationRules);
   // Potom môžete načítať dáta
   await DataGridControl.LoadDataAsync(data);
   ```

2. **Pomalé validácie**
   ```csharp
   // Použite rýchlejšiu throttling konfiguráciu
   var fastConfig = ThrottlingConfig.Fast;
   await DataGridControl.InitializeAsync(columns, validationRules, fastConfig);
   ```

3. **Memory leaks**
   ```csharp
   // Vždy dispose komponent
   public void OnWindowClosed()
   {
       DataGridControl?.Dispose();
   }
   ```

### Debugging

```csharp
// Zapnutie debug logov
AdvancedWinUiDataGridControl.Configuration.SetDebugLogging(true);

// Error handling
DataGridControl.ErrorOccurred += (sender, e) =>
{
    System.Diagnostics.Debug.WriteLine($"Error in {e.Operation}: {e.Exception.Message}");
};
```

## 🔄 Verzie a Changelog

### v1.0.6 (Aktuálna)
- ✅ Oprava všetkých CS1061 a CS1537 chýb
- ✅ Kompletná oprava zacyklenia validácie buniek  
- ✅ Optimalizácia pre NuGet distribúciu
- ✅ Cross-platform support (x86/x64/ARM64)
- ✅ Vylepšený error handling
- ✅ Throttling optimalizácia

### v1.0.5
- ✅ Základná implementácia real-time validácie
- ✅ Copy/Paste funkcionalita
- ✅ Keyboard navigation

## 📄 Licencia

Tento projekt je licencovaný pod MIT licenciou - pozrite si [LICENSE](LICENSE) súbor pre detaily.
