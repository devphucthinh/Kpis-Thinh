[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
. (Join-Path $RepositoryRoot 'scripts/branch-policy.ps1')

function Assert-ThrowsLike {
    param(
        [Parameter(Mandatory)][scriptblock]$Action,
        [Parameter(Mandatory)][string]$ExpectedPattern
    )

    try {
        & $Action
    }
    catch {
        if ($_.Exception.Message -notlike $ExpectedPattern) {
            throw "Expected error like '$ExpectedPattern' but received '$($_.Exception.Message)'."
        }
        return
    }

    throw "Expected an error like '$ExpectedPattern', but the action succeeded."
}

Assert-GitBranchPolicy `
    -ActiveBranch 'main' `
    -BranchNames @('main', 'feature/bsc-kpi-reference-implementation', 'release/1.0') `
    -AllowedBranches @('main', 'feature/bsc-kpi-reference-implementation') `
    -ForbiddenBranchFragments @('codex')

Assert-GitBranchPolicy `
    -ActiveBranch 'feature/bsc-kpi-reference-implementation' `
    -BranchNames @('main', 'feature/bsc-kpi-reference-implementation') `
    -AllowedBranches @('main', 'feature/bsc-kpi-reference-implementation') `
    -ForbiddenBranchFragments @('codex')

Assert-ThrowsLike -ExpectedPattern '*must run on one of the allowed branches*' -Action {
    Assert-GitBranchPolicy `
        -ActiveBranch 'feature/kpi' `
        -BranchNames @('main', 'feature/kpi') `
        -AllowedBranches @('main', 'feature/bsc-kpi-reference-implementation') `
        -ForbiddenBranchFragments @('codex')
}

Assert-ThrowsLike -ExpectedPattern '*forbidden branch name fragment*' -Action {
    Assert-GitBranchPolicy `
        -ActiveBranch 'main' `
        -BranchNames @('main', 'archive/Codex-old-work') `
        -AllowedBranches @('main', 'feature/bsc-kpi-reference-implementation') `
        -ForbiddenBranchFragments @('codex')
}

Write-Host 'Git branch policy tests passed.' -ForegroundColor Green
