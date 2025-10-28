$addinPath = "$env:APPDATA\Autodesk\Revit\Addins\2026"
$dllSource = "bin\x64\Release\net8.0-windows\FlexMapExporter.dll"
$manifestSource = "FlexMapExporter.addin"

Write-Host "Creating add-ins folder..." -ForegroundColor Yellow
New-Item -ItemType Directory -Path $addinPath -Force | Out-Null

Write-Host "Copying DLL..." -ForegroundColor Yellow
Copy-Item $dllSource $addinPath -Force

Write-Host "Copying manifest..." -ForegroundColor Yellow
Copy-Item $manifestSource $addinPath -Force

Write-Host "[OK] Deployment complete!" -ForegroundColor Green
Write-Host "Files deployed to: $addinPath" -ForegroundColor Gray
