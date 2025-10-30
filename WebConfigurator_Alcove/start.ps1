# Door Configurator - Quick Start Script
# Starts a local web server and opens the configurator in your browser

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Alcove Stall Configurator - Launch" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Python is available
$pythonCmd = Get-Command python -ErrorAction SilentlyContinue

if ($pythonCmd) {
    Write-Host "[OK] Found Python" -ForegroundColor Green
    Write-Host ""
    Write-Host "`nStarting web server on http://localhost:8001..." -ForegroundColor Green
    Write-Host "Press Ctrl+C to stop the server`n" -ForegroundColor Yellow
    
    # Wait a moment then open browser
    Start-Sleep -Seconds 1
    Start-Process "http://localhost:8001"
    
    # Start server (this will block)
    python -m http.server 8001
} else {
    Write-Host "[ERROR] Python not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Python or use one of these alternatives:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. Install Python from: https://www.python.org/downloads/" -ForegroundColor White
    Write-Host "2. Use VS Code Live Server extension" -ForegroundColor White
    Write-Host "3. Use Node.js: npm install -g http-server && http-server -p 8000" -ForegroundColor White
    Write-Host ""
}
