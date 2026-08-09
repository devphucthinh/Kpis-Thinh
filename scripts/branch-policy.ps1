function Assert-GitBranchPolicy {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ActiveBranch,
        [Parameter(Mandatory)][string[]]$BranchNames,
        [Parameter(Mandatory)][string]$WorkingBranch,
        [Parameter(Mandatory)][string[]]$ForbiddenBranchFragments
    )

    if ([string]::IsNullOrWhiteSpace($ActiveBranch)) {
        throw "Git policy requires an attached branch named '$WorkingBranch'."
    }

    if (-not $ActiveBranch.Equals($WorkingBranch, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Git policy must run on branch '$WorkingBranch'; current branch is '$ActiveBranch'."
    }

    foreach ($fragment in $ForbiddenBranchFragments) {
        $violations = @(
            $BranchNames |
                Where-Object { $_ -and $_.IndexOf($fragment, [StringComparison]::OrdinalIgnoreCase) -ge 0 }
        )

        if ($violations.Count -gt 0) {
            throw "Git policy found forbidden branch name fragment '$fragment' in: $($violations -join ', ')"
        }
    }
}
