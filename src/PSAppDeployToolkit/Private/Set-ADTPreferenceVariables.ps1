#-----------------------------------------------------------------------------
#
# MARK: Set-ADTPreferenceVariables
#
#-----------------------------------------------------------------------------

function Set-ADTPreferenceVariables
{
    <#
    .SYNOPSIS
        Sets preference variables within the called scope based on CommonParameter values within the callstack.

    .DESCRIPTION
        Script module functions do not automatically inherit their caller's variables, therefore we walk the callstack to get the closest bound CommonParameter value and use it within the called scope.

        This function is a helper function for any script module Advanced Function; by passing in the values of $ExecutionContext.SessionState, Set-ADTPreferenceVariables will set the caller's preference variables locally.

    .PARAMETER SessionState
        The $ExecutionContext.SessionState object from a script module Advanced Function. This is how the Set-ADTPreferenceVariables function sets variables in its callers' scope, even if that caller is in a different script module.

    .PARAMETER Scope
        A scope override, mostly so this can be called via Initialize-ADTFunction.

    .EXAMPLE
        Set-ADTPreferenceVariables -SessionState $ExecutionContext.SessionState

        Imports the default PowerShell preference variables from the caller into the local scope.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .NOTES
        An active ADT session is required to use this function.

        Original code inspired by: https://gallery.technet.microsoft.com/scriptcenter/Inherit-Preference-82343b9d

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This compatibility wrapper function cannot have its name changed for backwards compatiblity purposes.")]
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
