#-----------------------------------------------------------------------------
#
# MARK: Expand-ADTVariablesInHashtable
#
#-----------------------------------------------------------------------------

function Private:Expand-ADTVariablesInHashtable
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'SessionState', Justification = "This parameter is used within filters that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Hashtable,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState
    )

    begin
    {
        # Internal filter to refuse any value the expander must not be handed.
        filter Confirm-ADTHashtableExpansionIsSafe
        {
            foreach ($section in $($_.GetEnumerator()))
            {
                # Re-process if this is a hashtable.
                if ($section.Value -is [System.Collections.Hashtable])
                {
                    $section.Value | & $MyInvocation.MyCommand; continue
                }

                # Config values can come from Group Policy, so what the expander will act on is tested first.
                if (($section.Value -is [System.String]) -and (Test-ADTStringHasUnsafeExpansion -InputString $section.Value))
                {
                    $naerParams = @{
                        Exception = [System.InvalidOperationException]::new("The value for [$($section.Key)] holds a subexpression or a braced provider path. A value may name variables such as `$env:ProgramData, but it may not evaluate code or read through a provider.")
                        Category = [System.Management.Automation.ErrorCategory]::SecurityError
                        ErrorId = 'UnsafeStringExpansionValue'
                        TargetObject = $section.Value
                        RecommendedAction = "Please remove the subexpression or provider path from this value and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }
            }
        }

        # Internal filter to expand each value in place.
        filter Update-ADTHashtableExpandedValues
        {
            foreach ($section in $($_.GetEnumerator()))
            {
                # Re-process if this is a hashtable.
                if ($section.Value -is [System.Collections.Hashtable])
                {
                    $section.Value | & $MyInvocation.MyCommand; continue
                }

                # Written back against the table, as the enumerator's entry is a copy of it.
                if ($section.Value -is [System.String])
                {
                    $_.($section.Key) = $SessionState.InvokeCommand.ExpandString($section.Value)
                }
            }
        }
    }

    process
    {
        # The table is mutated in place, so the whole graph is tested before any of it is expanded. Refusing
        # part way through would leave the caller holding a table with some of its values already replaced.
        $Hashtable | Confirm-ADTHashtableExpansionIsSafe
        $Hashtable | Update-ADTHashtableExpandedValues
    }
}
