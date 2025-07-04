# 🛠️ RpaWinUiComponents - Troubleshooting Guide

Tento průvodce pomôže vyriešiť najčastejšie problémy pri používaní RpaWinUiComponents.

## 📋 Obsah

- [🚨 Kritické chyby](#-kritické-chyby)
- [⚙️ Konfiguračné problémy](#️-konfiguračné-problémy)
- [🔧 Build chyby](#-build-chyby)
- [🐛 Runtime chyby](#-runtime-chyby)
- [⚡ Výkonové problémy](#-výkonové-problémy)
- [📋 Validačné problémy](#-validačné-problémy)
- [🔍 Diagnostické nástroje](#-diagnostické-nástroje)

## 🚨 Kritické chyby

### CS1061: 'IServiceCollection' neobsahuje definíciu pre 'AddAdvancedWinUiDataGrid'

**Príčina:** Chýba using direktíva alebo nesprávny namespace.

**Riešenie:**
```csharp
// ✅ Pridajte using direktívu
using RpaWinUiComponents.AdvancedWinUiDataGrid;

// ✅ Alebo použite plný namespace
services.AddAdvancedWinUiDataGrid();
```

**Overenie:**
```csharp
// Skontrolujte či sa načíta assembly
var assembly = typeof(AdvancedWinUiDataGridControl).Assembly;
Console.WriteLine($"Assembly loaded: {assembly.FullName}");
```

### CS1537: Using alias konflikt

**Príčina:** Konflikty namespace medzi WinUI a našimi komponentmi.

**Riešenie:**
```csharp
// ✅ Použite explicitné aliasy
using WinUIColumnDefinition = Microsoft.UI.Xaml.Controls.ColumnDefinition;
using DataGridColumnDefinition = RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition;

// ✅ Alebo plné názvy
var column = new RpaWinUiComponents.AdvancedWinUiDataGrid.Models.ColumnDefinition("Name", typeof(string));
```

### "Component must be initialized first"

**Príčina:** Volanie metód pred InitializeAsync().

**Riešenie:**
```csharp
// ❌ Nesprávne poradie
await DataGridControl.LoadDataAsync(data);
await DataGridControl.InitializeAsync(columns);

// ✅ Správne poradie
await DataGridControl.InitializeAsync(columns, validationRules);
await DataGridControl.LoadDataAsync(data);
```

## ⚙️ Konfiguračné problémy

### Dependency Injection nie je nakonfigurovaný

**Príznaky:**
- NullReferenceException pri vytváraní ViewModelu
- Services sa nenačítajú

**Riešenie:**
```csharp
// ✅ Kompletná konfigurácia v App.xaml.cs
private void InitializeServices()
{
    var hostBuilder = Host.CreateDefaultBuilder()
        .ConfigureServices((context, services) =>
        {
            // KĽÚČOVÉ: Registrácia služieb
            services.AddAdvancedWinUiDataGrid();
        });

    _host = hostBuilder.Build();

    // KĽÚČOVÉ: Konfigurácia komponentu
    AdvancedWinUiDataGridControl.Configuration.ConfigureServices(_host.Services);
    
    var loggerFactory = _host.Services.GetRequiredService<ILoggerFactory>();
    AdvancedWinUiDataGridControl.Configuration.ConfigureLogging(loggerFactory);
}
```

### Logger nie je dostupný

**Príznaky:**
- Žiadne debug výstupy
- Chýbajúce error logy

**Riešenie:**
```csharp
// ✅ Konfigurácia loggovania
.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    logging.AddDebug();
    logging.SetMinimumLevel(LogLevel.Debug);
})

// ✅ Zapnutie debug logov
AdvancedWinUiDataGridControl.Configuration.SetDebugLogging(true);
```

## 🔧 Build chyby

### XAML compilation errors

**Príčina:** Nesprávne namespace deklarácie v XAML.

**Riešenie:**
```xml
<!-- ✅ Správny namespace -->
<Window xmlns:controls="using:RpaWinUiComponents.AdvancedWinUiDataGrid">
    <controls:AdvancedWinUiDataGridControl x:Name="DataGridControl" />
</Window>
```

### Package restore problémy

**Riešenie:**
```powershell
# Vyčistenie cache
dotnet nuget locals all --clear

# Force restore
dotnet restore --force

# Rebuild
dotnet clean && dotnet build
```

### WindowsAppSDK verzia konflikty

**Riešenie:**
```xml
<!-- V .csproj súbore použite správnu verziu -->
<PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250606001" />
```

## 🐛 Runtime chyby

### Zacyklenie validácie buniek

**Príznaky:**
- Vysoké CPU usage
- UI freezing
- Nekonečné validačné volania

**Riešenie:**
```csharp
// ✅ Použite pomalšiu throttling konfiguráciu
var throttling = ThrottlingConfig.Slow; // 500ms delay
await DataGridControl.InitializeAsync(columns, rules, throttling);

// ✅ Alebo vypnite throttling
var disabled = ThrottlingConfig.Disabled;
```

### Memory leaks

**Príčiny:**
- Nezavolanie Dispose()
- Event handlers sa neodpájajú

**Riešenie:**
```csharp
// ✅ Vždy dispose komponent
public void OnWindowClosed()
{
    DataGridControl?.Dispose();
}

// ✅ Event handling
DataGridControl.ErrorOccurred += OnError;
// Pri cleanup:
DataGridControl.ErrorOccurred -= OnError;
```

### ObjectDisposedException

**Príčina:** Volanie metód na disposed objekte.

**Riešenie:**
```csharp
// ✅ Kontrola pred použitím
if (DataGridControl != null && !DataGridControl.IsDisposed)
{
    await DataGridControl.LoadDataAsync(data);
}
```

## ⚡ Výkonové problémy

### Pomalé načítanie veľkých dát

**Riešenie:**
```csharp
// ✅ Použite batch loading
var batchSize = 100;
for (int i = 0; i < data.Count; i += batchSize)
{
    var batch = data.Skip(i).Take(batchSize).ToList();
    await DataGridControl.LoadDataAsync(batch);
    await Task.Delay(10); // UI breathing room
}

// ✅ Optimalizujte throttling
var optimized = ThrottlingConfig.Custom(200, maxConcurrentValidations: 8);
```

### Pomalé validácie

**Riešenie:**
```csharp
// ✅ Optimalizujte validačné pravidlá
var rule = new ValidationRule("Email", (value, row) =>
{
    var email = value?.ToString();
    // Rýchle kontroly najprv
    if (string.IsNullOrEmpty(email)) return true;
    if (email.Length < 5 || email.Length > 100) return false;
    // Zložitejšie kontroly nakoniec
    return email.Contains("@") && email.Contains(".");
}, "Invalid email");
```

### UI freezing

**Riešenie:**
```csharp
// ✅ Použite ConfigureAwait(false) pre background operácie
await DataGridControl.ValidateAllRowsAsync().ConfigureAwait(false);

// ✅ Spustite na background thread
await Task.Run(async () =>
{
    var result = await DataGridControl.ExportToDataTableAsync();
    return result;
});
```

## 📋 Validačné problémy

### Validácie sa nespúšťajú

**Kontrola:**
```csharp
// ✅ Skontrolujte či sú pravidlá pridané
var rulesCount = validationService.GetTotalRuleCount();
Console.WriteLine($"Total validation rules: {rulesCount}");

// ✅ Skontrolujte throttling
if (!ThrottlingConfig.IsEnabled)
{
    Console.WriteLine("Throttling is disabled - validations should be immediate");
}
```

### Async validácie sa niekedy nezavolia

**Riešenie:**
```csharp
// ✅ Nastavte primeraný timeout
var asyncRule = new ValidationRule("Email", (_, _) => true, "Email check failed")
{
    IsAsync = true,
    AsyncValidationFunction = async (value, row, cancellationToken) =>
    {
        // Simulácia async operácie
        await Task.Delay(100, cancellationToken);
        return !value?.ToString()?.Contains("test") == true;
    },
    ValidationTimeout = TimeSpan.FromSeconds(5) // Zvýšte timeout
};
```

### Validačné chyby sa nezobrazujú

**Riešenie:**
```csharp
// ✅ Skontrolujte či sú error messages nastavené
var rule = new ValidationRule("Name", 
    (value, row) => !string.IsNullOrWhiteSpace(value?.ToString()),
    "Name is required") // DÔLEŽITÉ: Nastavte správnu error message
{
    RuleName = "Name_Required"
};
```

## 🔍 Diagnostické nástroje

### Debug logging

```csharp
// ✅ Zapnutie detailného loggovania
AdvancedWinUiDataGridControl.Configuration.SetDebugLogging(true);

// V kóde používajte debug výstupy
System.Diagnostics.Debug.WriteLine($"DataGrid initialized with {columns.Count} columns");
```

### Performance monitoring

```csharp
// ✅ Meranie výkonu validácií
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
await DataGridControl.ValidateAllRowsAsync();
stopwatch.Stop();
Console.WriteLine($"Validation took: {stopwatch.ElapsedMilliseconds}ms");
```

### Memory monitoring

```csharp
// ✅ Kontrola pamäte
var beforeMemory = GC.GetTotalMemory(false);
await DataGridControl.LoadDataAsync(largeDataSet);
var afterMemory = GC.GetTotalMemory(false);
Console.WriteLine($"Memory used: {(afterMemory - beforeMemory) / 1024 / 1024} MB");
```

### Event monitoring

```csharp
// ✅ Monitoring všetkých eventov
DataGridControl.ErrorOccurred += (s, e) => 
    Console.WriteLine($"Error: {e.Operation} - {e.Exception.Message}");
```

## 🔧 Utility script pre diagnostiku

```powershell
# Verification script
Write-Host "🔍 RpaWinUiComponents Diagnostika" -ForegroundColor Cyan

# Check .NET version
$dotnetVersion = dotnet --version
Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green

# Check installed packages
dotnet list package | Select-String "RpaWinUiComponents"

# Check Windows App SDK
Get-AppxPackage | Where-Object {$_.Name -like "*WindowsAppRuntime*"} | Select-Object Name, Version

# Memory check
Write-Host "💾 Available Memory: $([math]::Round((Get-WmiObject Win32_OperatingSystem).FreePhysicalMemory / 1MB, 2)) GB" -ForegroundColor White

Write-Host "✅ Diagnostika dokončená" -ForegroundColor Green
```

## 📞 Získanie pomoci

Ak problém pretrvá:

1. **Zapnite debug logging** a skontrolujte výstupy
2. **Skopírujte chybové hlášky** so stack trace
3. **Opíšte kroky na reprodukciu** problému
4. **Uveďte verzie** (.NET, WindowsAppSDK, RpaWinUiComponents)

**Kontakty:**
- 🐛 **GitHub Issues:** [Nahlásiť problém](https://github.com/your-repo/RpaWinUiComponents/issues)
- 📧 **Email:** support@rpasolutions.sk
- 📖 **Wiki:** [Dokumentácia](https://github.com/your-repo/RpaWinUiComponents/wiki)

---

**💡 Tip:** Väčšina problémov sa vyrieši správnou konfiguráciou DI a dodržaním poradia inicializácie.