#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTSessionCallbackOperation
#
#-----------------------------------------------------------------------------

function Invoke-ADTSessionCallbackOperation
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Action', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateSet('Starting', 'Opening', 'Closing', 'Finishing')]
        [System.String]$Type,

        [Parameter(Mandatory = $true)]
        [ValidateSet('Add', 'Remove')]
        [System.String]$Action,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.CommandInfo[]]$Callback
    )

    # Cache the global callbacks and perform any required action.
    $callbacks = (Get-ADTModuleData).Callbacks.$Type
    $null = $Callback | & { process { if (($Action.Equals('Add') -and !$callbacks.Contains($_)) -or $callbacks.Contains($_)) { $callbacks.$Action($_) } } }
}
