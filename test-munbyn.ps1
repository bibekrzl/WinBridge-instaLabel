# Munbyn Printer Test Script
Write-Host "🖨️ Munbyn Printer Test Setup" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Check if SDK files exist
$sdkPath = "libs\munbyn\MunbynSDK.dll"
if (Test-Path $sdkPath) {
    Write-Host "✅ Munbyn SDK found at: $sdkPath" -ForegroundColor Green
} else {
    Write-Host "⚠️  Munbyn SDK not found at: $sdkPath" -ForegroundColor Yellow
    Write-Host "   Please place your Munbyn SDK files in: libs\munbyn\" -ForegroundColor White
    Write-Host "   Required files: MunbynSDK.dll, MunbynSDK.xml (if available)" -ForegroundColor White
}

Write-Host ""
Write-Host "🚀 Starting Print Server..." -ForegroundColor Green
Write-Host ""

# Start the web server
Write-Host "📱 Web Interface: http://localhost:5000" -ForegroundColor Cyan
Write-Host "🖨️ Munbyn Test Page: http://localhost:5000/munbyn-test.html" -ForegroundColor Cyan
Write-Host "📚 API Documentation: http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host ""
Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
Write-Host ""

# Change to web project directory and run
Set-Location "src\PrintServer.Web"
dotnet run 