Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$configPath = Join-Path $root '.harness/harness.json'
$harnessPath = Join-Path $root 'scripts/harness.ps1'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$harness = Get-Content -LiteralPath $harnessPath -Raw
$failures = [System.Collections.Generic.List[string]]::new()

if (-not ($config.steps.PSObject.Properties.Name -contains 'migrate')) {
    $failures.Add('Harness config must declare steps.migrate.')
}

if ($harness -notmatch "ValidateSet\([^)]*'migrate'") {
    $failures.Add('scripts/harness.ps1 must accept the migrate action.')
}

if ($harness -notmatch "'migrate'\s*\{\s*Invoke-HarnessSteps") {
    $failures.Add('scripts/harness.ps1 must dispatch migrate through configured argument-array steps.')
}

if ($harness -match "'check'\s*\{(?s:.*?)config\.steps\.migrate") {
    $failures.Add('check must not invoke the migration steps.')
}

$migrationCommands = @($config.steps.PSObject.Properties | Where-Object { $_.Name -match 'migrat' })
if ($migrationCommands.Count -ne 1) {
    $failures.Add('Exactly one user-facing migration action must be declared.')
}

if ($failures.Count -gt 0) {
    throw ($failures -join [Environment]::NewLine)
}

Write-Host 'Migration command contract checks passed.' -ForegroundColor Green
