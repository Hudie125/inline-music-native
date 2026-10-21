# test-all.ps1 - Runs all automated tests
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Running inline Music Unit Tests        " -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

$testProj = Join-Path $root "apps\windows\tests\InlineMusic.Tests\InlineMusic.Tests.csproj"
dotnet test $testProj --verbosity normal

Write-Host "`nAll test suites executed." -ForegroundColor Green
