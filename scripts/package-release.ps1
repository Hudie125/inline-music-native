# package-release.ps1 - Packages release binaries and computes SHA-256
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dist = Join-Path $root "build_artifacts\dist"

if (Test-Path $dist) {
    Remove-Item -Recurse -Force $dist
}
New-Item -ItemType Directory -Path $dist -Force | Out-Null

Write-Host "Publishing Windows binary..." -ForegroundColor Cyan
$winProj = Join-Path $root "apps\windows\src\InlineMusic\InlineMusic.csproj"
$winOut = Join-Path $dist "windows"
dotnet publish $winProj -c Release -r win-x64 --self-contained false -o $winOut

Write-Host "Creating Windows ZIP archive..." -ForegroundColor Cyan
$zipPath = Join-Path $dist "inline-music-windows-x64.zip"
Compress-Archive -Path "$winOut\*" -DestinationPath $zipPath

Write-Host "Calculating SHA-256 checksums..." -ForegroundColor Cyan
$hash = (Get-FileHash -Path $zipPath -Algorithm SHA256).Hash.ToLower()
$checksumFile = Join-Path $dist "SHA256SUMS.txt"
Set-Content -Path $checksumFile -Value "$hash  inline-music-windows-x64.zip"

Write-Host "`nRelease packaging completed!" -ForegroundColor Green
Write-Host "Archive: $zipPath"
Write-Host "SHA256:  $hash"
