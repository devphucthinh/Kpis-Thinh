[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('bootstrap', 'format', 'lint', 'test', 'migrate', 'check', 'status')]
    [string]$Action = 'check'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$ConfigPath = Join-Path $RepositoryRoot '.harness/harness.json'
$BranchPolicyPath = Join-Path $RepositoryRoot 'scripts/branch-policy.ps1'

# The Windows installer commonly registers dotnet under Program Files without
# updating the current non-login shell. Make the canonical harness discover it
# while still allowing PATH or CI-provisioned SDKs to win.
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    $knownDotnet = Join-Path ${env:ProgramFiles} 'dotnet'
    if (Test-Path (Join-Path $knownDotnet 'dotnet.exe')) {
        $env:Path = "$knownDotnet$([IO.Path]::PathSeparator)$env:Path"
    }
}

. $BranchPolicyPath

function Write-Section {
    param([Parameter(Mandatory)][string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Get-HarnessConfig {
    if (-not (Test-Path -LiteralPath $ConfigPath -PathType Leaf)) {
        throw "Harness config not found: $ConfigPath"
    }

    $config = Get-Content -LiteralPath $ConfigPath -Raw | ConvertFrom-Json
    if ($config.version -ne 1) {
        throw "Unsupported harness version '$($config.version)'. Expected version 1."
    }

    foreach ($name in @('bootstrap', 'format', 'lint', 'test', 'migrate')) {
        if (-not ($config.steps.PSObject.Properties.Name -contains $name)) {
            throw "Harness config is missing steps.$name."
        }
    }

    if (-not ($config.PSObject.Properties.Name -contains 'gitPolicy')) {
        throw 'Harness config is missing gitPolicy.'
    }

    return $config
}

function Test-RepositoryContract {
    param([Parameter(Mandatory)]$Config)

    Write-Section 'Repository contract'
    $missing = @()
    foreach ($relativePath in $Config.requiredFiles) {
        $candidate = Join-Path $RepositoryRoot $relativePath
        if (-not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            $missing += $relativePath
        }
    }

    if ($missing.Count -gt 0) {
        throw "Required files are missing: $($missing -join ', ')"
    }

    $trackedEnvFiles = @()
    $gitMetadataPath = Join-Path $RepositoryRoot '.git'
    if ((Get-Command git -ErrorAction SilentlyContinue) -and (Test-Path -LiteralPath $gitMetadataPath)) {
        $trackedEnvFiles = @(
            @(& git -C $RepositoryRoot ls-files -- '.env' '.env.*') |
                Where-Object { $_ -and $_ -notmatch '\.example$' }
        )
    }

    if ($trackedEnvFiles.Count -gt 0) {
        throw "Potential secret-bearing env files are tracked: $($trackedEnvFiles -join ', ')"
    }

    $gitCommand = Get-Command git -ErrorAction SilentlyContinue
    if (-not $gitCommand) {
        throw 'Git is required to enforce the repository branch policy.'
    }

    $isGitRepository = @(& git -C $RepositoryRoot rev-parse --is-inside-work-tree 2>$null)
    if ($LASTEXITCODE -ne 0 -or $isGitRepository.Count -eq 0 -or $isGitRepository[0] -ne 'true') {
        throw "Repository root is not a Git working tree: $RepositoryRoot"
    }

    $activeBranch = (@(& git -C $RepositoryRoot branch --show-current) -join '').Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to read the active Git branch.'
    }

    $branchNames = @(
        @(& git -C $RepositoryRoot for-each-ref '--format=%(refname:short)' refs/heads refs/remotes) |
            Where-Object { $_ -and $_ -notmatch '/HEAD$' }
    )
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to read local and remote-tracking Git branches.'
    }

    Assert-GitBranchPolicy `
        -ActiveBranch $activeBranch `
        -BranchNames $branchNames `
        -WorkingBranch $Config.gitPolicy.workingBranch `
        -ForbiddenBranchFragments @($Config.gitPolicy.forbiddenBranchFragments)

    Write-Host 'Repository contract passed.' -ForegroundColor Green
}

function Invoke-HarnessSteps {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)]$Steps
    )

    Write-Section $Name
    if (@($Steps).Count -eq 0) {
        Write-Host "No $($Name.ToLowerInvariant()) steps configured."
        return
    }

    foreach ($step in $Steps) {
        if ([string]::IsNullOrWhiteSpace($step.name) -or [string]::IsNullOrWhiteSpace($step.command)) {
            throw "$Name contains a step without a name or command."
        }

        $executable = Get-Command $step.command -ErrorAction SilentlyContinue
        if (-not $executable) {
            throw "Command '$($step.command)' for step '$($step.name)' was not found on PATH."
        }

        $workingDirectory = $RepositoryRoot
        if (-not ($step.PSObject.Properties.Name -contains 'args')) {
            throw "Step '$($step.name)' is missing its args array."
        }

        if ($step.PSObject.Properties.Name -contains 'workingDirectory') {
            $workingDirectory = (Resolve-Path (Join-Path $RepositoryRoot $step.workingDirectory)).Path
            $rootPrefix = $RepositoryRoot.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
            $isRoot = $workingDirectory.Equals($RepositoryRoot, [StringComparison]::OrdinalIgnoreCase)
            $isDescendant = $workingDirectory.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)
            if (-not ($isRoot -or $isDescendant)) {
                throw "Step '$($step.name)' resolves outside the repository."
            }
        }

        Write-Host "--> $($step.name)" -ForegroundColor DarkCyan
        Push-Location $workingDirectory
        try {
            & $step.command @($step.args)
            if ($LASTEXITCODE -ne 0) {
                throw "Step '$($step.name)' failed with exit code $LASTEXITCODE."
            }
        }
        finally {
            Pop-Location
        }
    }
}

function Show-HarnessStatus {
    param([Parameter(Mandatory)]$Config)

    Write-Section 'Harness status'
    Write-Host "Repository: $RepositoryRoot"
    Write-Host "Config version: $($Config.version)"
    Write-Host "Working branch: $($Config.gitPolicy.workingBranch)"
    Write-Host "Forbidden branch fragments: $(@($Config.gitPolicy.forbiddenBranchFragments) -join ', ')"

    foreach ($name in @('bootstrap', 'format', 'lint', 'test', 'migrate')) {
        $count = @($Config.steps.$name).Count
        Write-Host ("{0,-10} {1} step(s)" -f $name, $count)
    }

    Write-Host "`nTools"
    foreach ($tool in @('git', 'pwsh', 'node', 'pnpm', 'python', 'uv', 'docker', 'codex')) {
        $found = Get-Command $tool -ErrorAction SilentlyContinue
        $state = if ($found) { $found.Source } else { 'not found' }
        Write-Host ("{0,-10} {1}" -f $tool, $state)
    }
}

$config = Get-HarnessConfig

switch ($Action) {
    'bootstrap' { Invoke-HarnessSteps -Name 'Bootstrap' -Steps $config.steps.bootstrap }
    'format' { Invoke-HarnessSteps -Name 'Format' -Steps $config.steps.format }
    'lint' { Invoke-HarnessSteps -Name 'Lint' -Steps $config.steps.lint }
    'test' { Invoke-HarnessSteps -Name 'Test' -Steps $config.steps.test }
    'migrate' { Invoke-HarnessSteps -Name 'Migrate' -Steps $config.steps.migrate }
    'status' { Show-HarnessStatus -Config $config }
    'check' {
        Test-RepositoryContract -Config $config
        Invoke-HarnessSteps -Name 'Bootstrap' -Steps $config.steps.bootstrap
        Invoke-HarnessSteps -Name 'Lint' -Steps $config.steps.lint
        Invoke-HarnessSteps -Name 'Test' -Steps $config.steps.test
        Write-Host "`nAll harness checks passed." -ForegroundColor Green
    }
}
