#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

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

        # Cache the global callbacks.
        if (!($callbacks = (Get-ADTModuleData).Callbacks.$Type))
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new("Please ensure that [Initialize-ADTModule] is called before using any $($MyInvocation.MyCommand.Module.Name) functions.")
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'ADTCallbacksTableNotInitialised'
                TargetObject = $callbacks
                RecommendedAction = "Please ensure the module is initialised via [Initialize-ADTModule] and try again."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }

        # Perform any required action.
        $Callback | & {process {if (($Action.Equals('Add') -and !$callbacks.Contains($_)) -or $callbacks.Contains($_)) {$null = $callbacks.$Action($_)}}}
}
