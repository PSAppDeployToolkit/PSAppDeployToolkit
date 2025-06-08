#-----------------------------------------------------------------------------
#
# MARK: Set-ADTPreferenceVariables
#
#-----------------------------------------------------------------------------

function Private:Set-ADTPreferenceVariables
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$Scope = 1
    )

    # Get the callstack so we can enumerate bound parameters of our callers.
    $stackParams = (Get-PSCallStack).InvocationInfo.BoundParameters.GetEnumerator().GetEnumerator()

    # Loop through each common parameter and get the first bound value.
    foreach ($pref in $Script:PreferenceVariableTable.GetEnumerator())
    {
        # Return early if we have nothing.
        if (!($param = $stackParams | & { process { if ($_.Key.Equals($pref.Key)) { return @{ Name = $pref.Value; Value = $_.Value } } } } | Select-Object -First 1))
        {
            continue
        }

        # If we've hit a switch, default it to an ActionPreference of Continue.
        if ($param.Value -is [System.Management.Automation.SwitchParameter])
        {
            if (!$param.Value)
            {
                continue
            }
            $param.Value = [System.Management.Automation.ActionPreference]::Continue
        }

        # When we're within the same module, just go up a scope level to set the value.
        # If the caller in an external scope, we set this within their SessionState.
        if ($SessionState.Equals($ExecutionContext.SessionState))
        {
            Set-Variable @param -Scope $Scope -Force -Confirm:$false -WhatIf:$false
        }
        else
        {
            $SessionState.PSVariable.Set($param.Value, $param.Value)
        }
    }
}
