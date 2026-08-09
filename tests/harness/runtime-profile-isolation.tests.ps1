Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$harness = Get-Content -LiteralPath (Join-Path $root 'scripts/harness.ps1') -Raw
$failures = [System.Collections.Generic.List[string]]::new()

if ($harness -notmatch "Name -eq 'Test'") {
    $failures.Add('The Test harness action must identify its environment scope.')
}
if ($harness -notmatch "ConnectionStrings__KpiRuntime") {
    $failures.Add('The Test harness action must isolate the runtime connection.')
}
if ($harness -notmatch "Kpi__PersistenceProfile") {
    $failures.Add('The Test harness action must force the explicit InMemoryTest profile.')
}
if ($harness -notmatch "InMemoryTest") {
    $failures.Add('The Test harness action must use InMemoryTest for Web contract tests.')
}

if ($failures.Count -gt 0) {
    throw ($failures -join [Environment]::NewLine)
}

Write-Host 'Runtime profile isolation checks passed.' -ForegroundColor Green
