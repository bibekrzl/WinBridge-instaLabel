# Print Server Build Script
# This script builds and runs the Print Server application

param(
    [switch]$Build,
    [switch]$Run,
    [switch]$Publish,
    [switch]$Clean,
    [string]$Configuration = "Debug"
)

Write-Host "🖨️ Print Server Build Script" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Function to check if .NET is installed
function Test-DotNet {
    try {
        $dotnetVersion = dotnet --version
        Write-Host "✅ .NET SDK found: $dotnetVersion" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host "❌ .NET SDK not found. Please install .NET 7.0 or later." -ForegroundColor Red
        return $false
    }
}

# Function to clean build artifacts
function Clean-Build {
    Write-Host "🧹 Cleaning build artifacts..." -ForegroundColor Yellow
    dotnet clean
    Remove-Item -Path "bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "obj" -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✅ Clean completed" -ForegroundColor Green
}

# Function to build the solution
function Build-Solution {
    Write-Host "🔨 Building solution..." -ForegroundColor Yellow
    dotnet build -c $Configuration
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build completed successfully" -ForegroundColor Green
    } else {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit 1
    }
}

# Function to run the web server
function Start-WebServer {
    Write-Host "🌐 Starting web server..." -ForegroundColor Yellow
    Write-Host "📱 Web interface will be available at: http://localhost:5000" -ForegroundColor Cyan
    Write-Host "📚 API documentation: http://localhost:5000/swagger" -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop the server" -ForegroundColor Yellow
    
    Set-Location "src/PrintServer.Web"
    dotnet run
}

# Function to run the tray application
function Start-TrayApp {
    Write-Host "🖥️ Starting tray application..." -ForegroundColor Yellow
    Write-Host "Look for the Print Server icon in your system tray" -ForegroundColor Cyan
    
    Set-Location "src/PrintServer.Tray"
    dotnet run
}

# Function to publish the application
function Publish-Application {
    Write-Host "📦 Publishing application..." -ForegroundColor Yellow
    
    # Create publish directory
    $publishDir = "publish"
    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $publishDir | Out-Null
    
    # Publish Web API
    Write-Host "📦 Publishing Web API..." -ForegroundColor Yellow
    dotnet publish "src/PrintServer.Web" -c Release -o "$publishDir/web"
    
    # Publish Tray App
    Write-Host "📦 Publishing Tray App..." -ForegroundColor Yellow
    dotnet publish "src/PrintServer.Tray" -c Release -r win-x64 --self-contained -o "$publishDir/tray"
    
    # Create executable version
    Write-Host "📦 Creating executable..." -ForegroundColor Yellow
    dotnet publish "src/PrintServer.Tray" -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o "$publishDir/executable"
    
    Write-Host "✅ Publishing completed" -ForegroundColor Green
    Write-Host "📁 Published files are in the '$publishDir' directory" -ForegroundColor Cyan
}

# Main execution
if (-not (Test-DotNet)) {
    exit 1
}

# Handle parameters
if ($Clean) {
    Clean-Build
}

if ($Build) {
    Build-Solution
}

if ($Publish) {
    Build-Solution
    Publish-Application
}

if ($Run) {
    Build-Solution
    
    Write-Host "🚀 Starting Print Server..." -ForegroundColor Green
    Write-Host "Choose an option:" -ForegroundColor Cyan
    Write-Host "1. Web Server (API + Web Interface)" -ForegroundColor White
    Write-Host "2. Tray Application (Windows Tray)" -ForegroundColor White
    Write-Host "3. Both (Web Server in background + Tray App)" -ForegroundColor White
    
    $choice = Read-Host "Enter your choice (1-3)"
    
    switch ($choice) {
        "1" {
            Start-WebServer
        }
        "2" {
            Start-TrayApp
        }
        "3" {
            Write-Host "🔄 Starting both applications..." -ForegroundColor Yellow
            
            # Start web server in background
            $webJob = Start-Job -ScriptBlock {
                Set-Location $using:PWD
                Set-Location "src/PrintServer.Web"
                dotnet run
            }
            
            # Start tray app
            Start-TrayApp
            
            # Clean up background job
            Stop-Job $webJob
            Remove-Job $webJob
        }
        default {
            Write-Host "❌ Invalid choice. Please run the script again." -ForegroundColor Red
        }
    }
}

# If no parameters provided, show help
if (-not ($Build -or $Run -or $Publish -or $Clean)) {
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1 -Build          # Build the solution" -ForegroundColor White
    Write-Host "  .\build.ps1 -Run            # Build and run the application" -ForegroundColor White
    Write-Host "  .\build.ps1 -Publish        # Build and publish the application" -ForegroundColor White
    Write-Host "  .\build.ps1 -Clean          # Clean build artifacts" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1 -Build -Run     # Build and then run" -ForegroundColor White
    Write-Host "  .\build.ps1 -Clean -Build   # Clean, then build" -ForegroundColor White
} 