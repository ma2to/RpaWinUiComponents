# Cleanup script pre odstránenie duplicitných súborov z Demo projektu
Write-Host "🧹 Čistenie duplicitných súborov z Demo projektu..." -ForegroundColor Cyan

# Aktuálny adresár by mal byť root solution
if (-not (Test-Path "RpaWinUiComponents.sln")) {
    Write-Host "❌ Spustite script z root adresára solution (kde je .sln súbor)" -ForegroundColor Red
    exit 1
}

# 1. Odstránenie duplicitných XAML súborov z Demo projektu
Write-Host "`n🗑️ Odstráňujú sa duplicitné súbory..." -ForegroundColor Yellow

$duplicateFolder = "RpaWinUiComponents.Demo\AdvancedWinUiDataGrid"
if (Test-Path $duplicateFolder) {
    Write-Host "Odstráňuje sa: $duplicateFolder" -ForegroundColor White
    Remove-Item -Recurse -Force $duplicateFolder
    Write-Host "✅ Duplicitný folder odstránený" -ForegroundColor Green
} else {
    Write-Host "✅ Duplicitný folder už neexistuje" -ForegroundColor Green
}

# 2. Vyčistenie build artefaktov
Write-Host "`n🧽 Vyčisťovanie build artefaktov..." -ForegroundColor Yellow

$foldersToClean = @(
    "RpaWinUiComponents.Demo\bin",
    "RpaWinUiComponents.Demo\obj",
    "RpaWinUiComponents\bin", 
    "RpaWinUiComponents\obj",
    "bin",
    "obj",
    "packages"
)

foreach ($folder in $foldersToClean) {
    if (Test-Path $folder) {
        Write-Host "Čistí sa: $folder" -ForegroundColor White
        Remove-Item -Recurse -Force $folder
    }
}

Write-Host "✅ Build artefakty vyčistené" -ForegroundColor Green

# 3. Vyčistenie NuGet cache
Write-Host "`n📦 Vyčisťovanie NuGet cache..." -ForegroundColor Yellow
dotnet nuget locals all --clear
Write-Host "✅ NuGet cache vyčistený" -ForegroundColor Green

# 4. Kontrola súborov
Write-Host "`n🔍 Kontrola súborov..." -ForegroundColor Yellow

$expectedFiles = @(
    "RpaWinUiComponents.Demo\MainWindow.xaml",
    "RpaWinUiComponents.Demo\MainWindow.xaml.cs", 
    "RpaWinUiComponents.Demo\App.xaml",
    "RpaWinUiComponents.Demo\App.xaml.cs",
    "RpaWinUiComponents.Demo\RpaWinUiComponents.Demo.csproj"
)

$unexpectedFiles = @(
    "RpaWinUiComponents.Demo\AdvancedWinUiDataGrid\Views\AdvancedDataGridControl.xaml",
    "RpaWinUiComponents.Demo\AdvancedWinUiDataGrid\Themes\Generic.xaml"
)

Write-Host "`n✅ Očakávané súbory:" -ForegroundColor Green
foreach ($file in $expectedFiles) {
    if (Test-Path $file) {
        Write-Host "  ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "  ❌ $file CHÝBA" -ForegroundColor Red
    }
}

Write-Host "`n🚫 Súbory ktoré by NEMALI existovať:" -ForegroundColor Yellow
foreach ($file in $unexpectedFiles) {
    if (Test-Path $file) {
        Write-Host "  ❌ $file STÁLE EXISTUJE" -ForegroundColor Red
    } else {
        Write-Host "  ✅ $file neexistuje (správne)" -ForegroundColor Green
    }
}

Write-Host "`n🎉 Cleanup dokončený!" -ForegroundColor Cyan
Write-Host "`n📋 Ďalšie kroky:" -ForegroundColor Yellow
Write-Host "1. Spustite: cd RpaWinUiComponents && .\Build-Package.ps1 -Version '1.0.8'" -ForegroundColor White
Write-Host "2. Aktualizujte NuGet referenicu v Demo projekte" -ForegroundColor White
Write-Host "3. Spustite: dotnet restore && dotnet build" -ForegroundColor White
Write-Host "4. Testujte Demo aplikáciu" -ForegroundColor White