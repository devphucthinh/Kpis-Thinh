Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$guide = Get-Content (Join-Path $root 'HUONG_DAN_TICH_HOP_KPI.txt') -Raw
$required = @('harness.cmd bootstrap', 'KpiManagement.slnx', 'PostgreSQL', 'kpi_lab_test', '/api/v1/formulas/validate', '/api/v1/formulas/capabilities', 'supportedOperations', 'formula.ast', 'ActorContext', 'Development', 'separation-of-duty', '/KpiPeriods/Details/{id}', '/KpiEvaluations/History', '/Audit', 'KpiFullFlowTests.cs', 'data-theme-toggle', 'formula-suggestions-panel', 'formula-syntax-helper', 'ROUND(value, decimals)', 'run-kpi.bat postgres')
$missing = @($required | Where-Object { $guide -notmatch [regex]::Escape($_) })
if ($missing.Count -gt 0) { throw "Integration guide is missing: $($missing -join ', ')" }
Write-Host 'Integration guide checks passed.' -ForegroundColor Green
