# build-all.ps1 - Builds inline Music for local testing
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Building inline Music (Windows Native) " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$winProj = Join-Path $root "apps\windows\src\InlineMusic\InlineMusic.csproj"
dotnet build $winProj -c $Configuration

Write-Host "`nWindows build completed successfully." -ForegroundColor Green
