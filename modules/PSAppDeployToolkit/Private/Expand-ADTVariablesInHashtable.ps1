#-----------------------------------------------------------------------------
#
# MARK: Expand-ADTVariablesInHashtable
#
#-----------------------------------------------------------------------------

function Private:Expand-ADTVariablesInHashtable
{
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

    process
    {
        # Go recursive if we've received a hashtable, otherwise just update the values.
        foreach ($section in $($Hashtable.GetEnumerator()))
        {
            if ($section.Value -is [System.String])
            {
                # Config values can come from Group Policy, so what the expander will act on is tested first.
                if (Test-ADTStringHasUnsafeExpansion -InputString $section.Value)
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
                $Hashtable.($section.Key) = $SessionState.InvokeCommand.ExpandString($section.Value)
            }
            elseif ($section.Value -is [System.Collections.Hashtable])
            {
                & $MyInvocation.MyCommand -Hashtable $section.Value -SessionState $SessionState
            }
        }
    }
}
