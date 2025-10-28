$addinPath = "$env:APPDATA\Autodesk\Revit\Addins\2026"
$manifestPath = Join-Path $addinPath "FlexMapExporter.addin"

Write-Host "Checking deployed manifest..." -ForegroundColor Yellow
Write-Host ""

if (Test-Path $manifestPath) {
    $content = Get-Content $manifestPath -Raw
    
    if ($content -match "AddInId") {
        Write-Host "[OK] AddInId node is present!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Manifest content:" -ForegroundColor Cyan
        Get-Content $manifestPath | ForEach-Object { Write-Host $_ }
    } else {
        Write-Host "[ERROR] AddInId node is missing!" -ForegroundColor Red
    }
} else {
    Write-Host "[ERROR] Manifest file not found!" -ForegroundColor Red
}
