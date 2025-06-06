#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTMinimizeWindowsOperation
#
#-----------------------------------------------------------------------------

function Private:Invoke-ADTMinimizeWindowsOperation
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'MinimizeAllWindows')]
        [System.Management.Automation.SwitchParameter]$MinimizeAllWindows,

        [Parameter(Mandatory = $true, ParameterSetName = 'RestoreAllWindows')]
        [System.Management.Automation.SwitchParameter]$RestoreAllWindows
    )

    # Instantiate a new DisplayServer object if one's not already present.
    if (!$Script:ADT.DisplayServer)
    {
        Open-ADTDisplayServer
    }

    # Invoke the specified action.
    if (!$Script:ADT.DisplayServer.($PSCmdlet.ParameterSetName)())
    {
        $naerParams = @{
            Exception = [System.ApplicationException]::new("Failed to $($PSCmdlet.ParameterSetName.Split('All')[0].ToLower()) all windows for an unknown reason.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = "$($PSCmdlet.ParameterSetName)Error"
            RecommendedAction = "Please report this issue to the PSAppDeployToolkit development team."
        }
        throw (New-ADTErrorRecord @naerParams)
    }
}
