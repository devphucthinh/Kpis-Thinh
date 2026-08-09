[CmdletBinding()]
param()
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$browserRoot = Join-Path $env:USERPROFILE 'AppData/Local/ms-playwright'
$existing = @(Get-ChildItem -LiteralPath $browserRoot -Directory -ErrorAction SilentlyContinue | Where-Object Name -like 'chromium-*')
if ($existing.Count -gt 0) { Write-Host 'Playwright Chromium is already provisioned.'; exit 0 }
$playwright = Join-Path $root 'tests/Kpi.Web.EndToEndTests/bin/Release/net10.0/playwright.ps1'
if (-not (Test-Path -LiteralPath $playwright)) { throw "Playwright driver script not found after build: $playwright" }
& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $playwright install chromium
if ($LASTEXITCODE -ne 0) { throw "Playwright browser provisioning failed with exit code $LASTEXITCODE." }
