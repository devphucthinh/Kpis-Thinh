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
    -BranchNames @('main', 'release/1.0') `
    -WorkingBranch 'main' `
    -ForbiddenBranchFragments @('codex')

Assert-ThrowsLike -ExpectedPattern "*must run on branch 'main'*" -Action {
    Assert-GitBranchPolicy `
        -ActiveBranch 'feature/kpi' `
        -BranchNames @('main', 'feature/kpi') `
        -WorkingBranch 'main' `
        -ForbiddenBranchFragments @('codex')
}

Assert-ThrowsLike -ExpectedPattern '*forbidden branch name fragment*' -Action {
    Assert-GitBranchPolicy `
        -ActiveBranch 'main' `
        -BranchNames @('main', 'archive/Codex-old-work') `
        -WorkingBranch 'main' `
        -ForbiddenBranchFragments @('codex')
}

Write-Host 'Git branch policy tests passed.' -ForegroundColor Green
