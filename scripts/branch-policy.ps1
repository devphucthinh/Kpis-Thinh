function Assert-GitBranchPolicy {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ActiveBranch,
        [Parameter(Mandatory)][string[]]$BranchNames,
        [Parameter(Mandatory)][string[]]$AllowedBranches,
        [Parameter(Mandatory)][string[]]$ForbiddenBranchFragments
    )

    if ([string]::IsNullOrWhiteSpace($ActiveBranch)) {
        throw "Git policy requires an attached branch in: $($AllowedBranches -join ', ')."
    }

    $allowedMatch = @(
        $AllowedBranches |
            Where-Object { $ActiveBranch.Equals($_, [StringComparison]::OrdinalIgnoreCase) }
    )
    if ($allowedMatch.Count -eq 0) {
        throw "Git policy must run on one of the allowed branches ($($AllowedBranches -join ', ')); current branch is '$ActiveBranch'."
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
