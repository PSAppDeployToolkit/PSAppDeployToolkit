#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Invoke-ADTSessionCallbackOperation
{
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
        $Callback.Where($(if ($Action -eq 'Add') {{!$callbacks.Contains($_)}} else {{$callbacks.Contains($_)}})).ForEach({$callbacks.$Action($_)})
}
