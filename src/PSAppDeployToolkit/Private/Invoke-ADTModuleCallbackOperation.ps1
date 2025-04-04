#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTModuleCallbackOperation
#
#-----------------------------------------------------------------------------

function Invoke-ADTModuleCallbackOperation
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Action', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.Module.CallbackType]$Hookpoint,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Add', 'Remove')]
        [System.String]$Action,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.CommandInfo[]]$Callback
    )

    # Cache the global callbacks and perform any required action.
    $callbacks = $Script:ADT.Callbacks.$Hookpoint
    $null = $Callback | & { process { if ($Action.Equals('Remove') -or !$callbacks.Contains($_)) { $callbacks.$Action($_) } } }
}
