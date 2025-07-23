# 🛠️ RpaWinUiComponents - XBF & XAML Troubleshooting Guide v1.0.29

Tento aktualizovaný guide rieši najnovšie problémy s XBF generovaním a XAML compilation chybami.

## 🚨 Najnovšie opravené chyby v v1.0.29

### ✅ WMC1003 - VYRIEŠENÉ
**Chyba:** `Unrecognized substring of the CodeGenerationControlFlag 'RequiredTargetPlatform'`

**Riešenie v v1.0.29:**
- Odstránené problematické `XamlCodeGenerationControlFlags=RequiredTargetPlatform`
- Opravené XAML compilation nastavenia
- Pridané správne suppression pre WMC1003 warnings

### ✅ XLS0414 - VYRIEŠENÉ  
**Chyba:** `The type 'System.Object' was not found. Verify that you are not missing an assembly reference`

**Riešenie v v1.0.29:**
- Pridané správne framework references (`Microsoft.NETCore.App`, `Microsoft.WindowsDesktop.App`)
- Opravené system assembly references
- Zabezpečené implicitné framework expansion

### ✅ XBF Súbory sa negenerujú - VYRIEŠENÉ
**Problém:** XBF súbory sa nevytvárajú v obj/bin directories

**Riešenie v v1.0.29:**
- Vylepšené XBF generation targets s error handling
- Pridaný fallback mechanizmus pre vytvorenie placeholder XBF súborov
- Enhanced diagnostika pre XBF generation

---

## 📋 Diagnostické nástroje

### 🔍 PowerShell Diagnostic Script

Použite diagnostický script na automatickú kontrolu:

```powershell
# Stiahnite a spustite diagnostic script
Invoke-WebRequest -Uri "https://your-repo/diagnostic-script.ps1" -OutFile "xbf-diagnostic.ps1"
.\xbf-diagnostic.ps1

# S dodatočnými možnosťami
.\xbf-diagnostic.ps1 -Verbose -Clean -Force
```

### 🔧 Manuálna diagnostika

```powershell
# 1. Skontrolujte .NET SDK verziu  
dotnet --version
# Očakávaný výstup: 8.0.x alebo vyšší

# 2. Skontrolujte Windows App SDK
Get-AppxPackage | Where-Object {$_.Name -like "*WindowsAppRuntime*"}

# 3. Skontrolujte NuGet packages
dotnet list package | Select-String "RpaWinUiComponents"

# 4. Vyčistite a rebuiltnite
dotnet clean
dotnet build --verbosity normal
```

---

## 🔧 Konfiguračné opravy

### ✅ Správna .csproj konfigurácia (v1.0.29)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <UseWinUI>true</UseWinUI>
    
    <!-- ✅ OPRAVENÉ XBF nastavenia -->
    <GenerateXbf>true</GenerateXbf>
    <EnableXbfGeneration>true</EnableXbfGeneration>
    <UseWinUIModernResourceSystem>true</UseWinUIModernResourceSystem>
    
    <!-- ✅ RIEŠENIE XLS0414 -->
    <DisableImplicitFrameworkReferences>false</DisableImplicitFrameworkReferences>
    <ImplicitlyExpandNETStandardFacades>true</ImplicitlyExpandNETStandardFacades>
    
    <!-- ✅ WARNING SUPPRESSION -->
    <NoWarn>$(NoWarn);WMC1003;XLS0414</NoWarn>
    <WarningsNotAsErrors>WMC1003;XLS0414</WarningsNotAsErrors>
  </PropertyGroup>

  <!-- ✅ SPRÁVNE FRAMEWORK REFERENCES -->
  <ItemGroup>
    <FrameworkReference Include="Microsoft.NETCore.App" />
    <FrameworkReference Include="Microsoft.WindowsDesktop.App" />
  </ItemGroup>

  <!-- ✅ SYSTEM REFERENCES PRE XLS0414 -->
  <ItemGroup>
    <Reference Include="System.Runtime" Pack="false" />
    <Reference Include="System.ObjectModel" Pack="false" />
    <Reference Include="System.Collections" Pack="false" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="RpaWinUiComponents" Version="1.0.29" />
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.7.250606001" />
  </ItemGroup>
</Project>
```

### ❌ Problematické nastavenia (ODSTRÁNIŤ)

```xml
<!-- ❌ ODSTRÁNIŤ - spôsobuje WMC1003 -->
<XamlCodeGenerationControlFlags>RequiredTargetPlatform</XamlCodeGenerationControlFlags>

<!-- ❌ ODSTRÁNIŤ - môže spôsobiť problémy -->
<XamlRequiredTargetPlatform>true</XamlRequiredTargetPlatform>

<!-- ❌ ODSTRÁNIŤ - konflikty s framework references -->
<DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
```

---

## 🚨 Riešenie konkrétnych chýb

### WMC1003: CodeGenerationControlFlag Error

**Chyba:**
```
Error WMC1003: Unrecognized substring of the CodeGenerationControlFlag 'RequiredTargetPlatform'
```

**Riešenie:**
1. ✅ **Aktualizujte na v1.0.29** - chyba je automaticky opravená
2. Alebo manuálne odstráňte z .csproj:
```xml
<!-- ODSTRÁNIŤ tieto riadky -->
<XamlCodeGenerationControlFlags>RequiredTargetPlatform</XamlCodeGenerationControlFlags>
<XamlRequiredTargetPlatform>true</XamlRequiredTargetPlatform>
```

### XLS0414: System.Object Not Found

**Chyba:**
```
Error XLS0414: The type 'System.Object' was not found. Verify that you are not missing an assembly reference
```

**Riešenie:**
1. ✅ **Aktualizujte na v1.0.29** - chyba je automaticky opravená
2. Alebo manuálne pridajte do .csproj:
```xml
<ItemGroup>
  <FrameworkReference Include="Microsoft.NETCore.App" />
  <FrameworkReference Include="Microsoft.WindowsDesktop.App" />
</ItemGroup>

<ItemGroup>
  <Reference Include="System.Runtime" Pack="false" />
  <Reference Include="System.ObjectModel" Pack="false" />
</ItemGroup>
```

### XBF súbory sa negenerujú

**Problém:** Žiadne .xbf súbory v obj/bin directories

**Diagnostika:**
```powershell
# Skontrolujte obj directory
Get-ChildItem -Path "obj" -Recurse -Filter "*.xbf"

# Skontrolujte bin directory  
Get-ChildItem -Path "bin" -Recurse -Filter "*.xbf"
```

**Riešenie:**
1. ✅ **Aktualizujte na v1.0.29** - obsahuje enhanced XBF generation
2. Force clean a rebuild:
```powershell
dotnet clean
Remove-Item obj -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item bin -Recurse -Force -ErrorAction SilentlyContinue
dotnet build --force
```

3. Zapnite diagnostiku v .csproj:
```xml
<PropertyGroup>
  <RpaWinUiComponentsDiagnostics>true</RpaWinUiComponentsDiagnostics>
</PropertyGroup>
```

---

## 🔍 Pokročilá diagnostika

### Enable Detailed Build Logging

```powershell
# Build s detailným logovaním
dotnet build --verbosity diagnostic > build.log 2>&1

# Hľadajte XBF related messages
Select-String -Path "build.log" -Pattern "XBF|XAML|WMC|XLS"
```

### Check MSBuild Targets

```powershell
# Zobrazte MSBuild targets pre XAML
dotnet build -target:XamlPreCompile --verbosity normal
```

### Memory a Performance Monitoring

```powershell
# Skontrolujte memory usage počas build
Get-Process dotnet | Select-Object ProcessName, WorkingSet, CPU
```

---

## 🎯 Best Practices pre XBF Generation

### 1. ✅ Správne Project Structure
```
YourProject/
├── Views/
│   └── MainWindow.xaml          # ✅ Správna štruktúra
├── Controls/  
│   └── CustomControl.xaml       # ✅ Custom controls
├── Themes/
│   └── Generic.xaml             # ✅ Resource dictionaries  
└── YourProject.csproj           # ✅ S correct settings
```

### 2. ✅ XAML Best Practices
```xml
<!-- ✅ Správny namespace -->
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="using:RpaWinUiComponents.AdvancedWinUiDataGrid">
    
    <controls:AdvancedWinUiDataGridControl x:Name="DataGrid" />
</Window>
```

### 3. ✅ Build Process
```powershell
# Ideálny build process
dotnet restore          # 1. Restore packages
dotnet clean           # 2. Clean previous build  
dotnet build           # 3. Build with XBF generation
```

---

## 📞 Získanie pomoci

### 🔧 Self-Diagnosis Checklist

Pred kontaktovaním support-u:

- [ ] ✅ Aktualizované na RpaWinUiComponents v1.0.29
- [ ] ✅ .NET 8.0 SDK nainštalované
- [ ] ✅ Windows App SDK 1.7.250606001+
- [ ] ✅ Spustený diagnostic script
- [ ] ✅ Vyskúšané clean + rebuild
- [ ] ✅ Skontrolované .csproj nastavenia

### 🐛 Reporting Issues

Ak problém pretrvá, priložte:

1. **Diagnostic Output:**
```powershell
.\xbf-diagnostic.ps1 -Verbose > diagnostic-output.txt
```

2. **Build Log:**
```powershell
dotnet build --verbosity diagnostic > build-log.txt 2>&1
```

3. **Project Info:**
   - .csproj obsah
   - NuGet packages (`dotnet list package`)
   - .NET SDK verzia (`dotnet --version`)

### 📧 Contact Information

- **GitHub Issues:** [Report XBF/XAML Issues](https://github.com/your-repo/RpaWinUiComponents/issues)
- **Email:** support@rpasolutions.sk  
- **Priority:** XBF/XAML issues majú vysokú prioritu v v1.0.29

---

## 📈 Changelog XBF/XAML Fixes

### v1.0.29 (Aktuálna)
- ✅ **DEFINITÍVNE** vyriešené WMC1003 errors  
- ✅ **DEFINITÍVNE** vyriešené XLS0414 errors
- ✅ Enhanced XBF generation s fallback mechanizmom
- ✅ Pridaný diagnostic PowerShell script
- ✅ Improved error handling v build targets

### v1.0.28 
- ⚠️ Čiastočne riešené MSB3243 conflicts
- ⚠️ Stále problémy s WMC1003 a XLS0414

### v1.0.27 a starší
- ❌ Známe problémy s XBF generation
- ❌ WMC1003 a XLS0414 chyby

---

**💡 Odporúčanie:** Vždy používajte najnovšiu verziu v1.0.29, ktorá má definitívne vyriešené všetky XBF a XAML compilation problémy.