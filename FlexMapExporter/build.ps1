# FlexMapExporter Build Script
# Builds and deploys the Revit add-in

param(
    [string]$Configuration = "Release"
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Flex-Map Exporter Build Script" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# Check for .NET SDK
Write-Host "Checking for .NET 8 SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: .NET SDK not found!" -ForegroundColor Red
    Write-Host "Please install .NET 8 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
Write-Host ""

# Check for Revit 2026
Write-Host "Checking for Revit 2026..." -ForegroundColor Yellow
$revitPath = "C:\Program Files\Autodesk\Revit 2026"
if (-not (Test-Path $revitPath)) {
    Write-Host "WARNING: Revit 2026 not found at default location" -ForegroundColor Yellow
    Write-Host "Path: $revitPath" -ForegroundColor Yellow
    Write-Host "You may need to update the RevitAPIPath in the .csproj file" -ForegroundColor Yellow
} else {
    Write-Host "[OK] Found Revit 2026" -ForegroundColor Green
}
Write-Host ""

# Clean previous build
Write-Host "Cleaning previous build..." -ForegroundColor Yellow
dotnet clean --configuration $Configuration --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Clean failed" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Clean complete" -ForegroundColor Green
Write-Host ""

# Restore NuGet packages
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Restore failed" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Packages restored" -ForegroundColor Green
Write-Host ""

# Build project
Write-Host "Building project ($Configuration)..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    exit 1
}
Write-Host "[OK] Build successful" -ForegroundColor Green
Write-Host ""

# Check deployment
$addinPath = "$env:APPDATA\Autodesk\Revit\Addins\2026"
Write-Host "Checking deployment..." -ForegroundColor Yellow
Write-Host "Target folder: $addinPath" -ForegroundColor Gray

if (Test-Path "$addinPath\FlexMapExporter.dll") {
    Write-Host "[OK] FlexMapExporter.dll deployed" -ForegroundColor Green
} else {
    Write-Host "WARNING: DLL not found in add-ins folder" -ForegroundColor Yellow
}

if (Test-Path "$addinPath\FlexMapExporter.addin") {
    Write-Host "[OK] FlexMapExporter.addin manifest deployed" -ForegroundColor Green
} else {
    Write-Host "WARNING: Manifest not found in add-ins folder" -ForegroundColor Yellow
}
Write-Host ""

# Summary
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "  Build Complete!" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Start Revit 2026" -ForegroundColor White
Write-Host "2. Open a family document (.rfa)" -ForegroundColor White
Write-Host "3. Go to Add-Ins tab -> Flex-Map Exporter" -ForegroundColor White
Write-Host ""
Write-Host "Add-in location: $addinPath" -ForegroundColor Gray
Write-Host ""
