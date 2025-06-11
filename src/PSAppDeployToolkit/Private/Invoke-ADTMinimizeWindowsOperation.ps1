#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTMinimizeWindowsOperation
#
#-----------------------------------------------------------------------------

function Private:Invoke-ADTMinimizeWindowsOperation
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'MinimizeAllWindows', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'RestoreAllWindows', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'MinimizeAllWindows')]
        [System.Management.Automation.SwitchParameter]$MinimizeAllWindows,

        [Parameter(Mandatory = $true, ParameterSetName = 'RestoreAllWindows')]
        [System.Management.Automation.SwitchParameter]$RestoreAllWindows
    )

    # Throw if there's no client/server process active.
    if (!$Script:ADT.ClientServerProcess)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("There is currently no client/server process active.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ClientServerProcessNull'
            TargetObject = $Script:ADT.ClientServerProcess
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }

    # Invoke the specified action.
    if (!$Script:ADT.ClientServerProcess.($PSCmdlet.ParameterSetName)())
    {
        $naerParams = @{
            Exception = [System.ApplicationException]::new("Failed to $($PSCmdlet.ParameterSetName.Split('All')[0].ToLower()) all windows for an unknown reason.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = "$($PSCmdlet.ParameterSetName)Error"
            RecommendedAction = "Please report this issue to the PSAppDeployToolkit development team."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
}
