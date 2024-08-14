#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Get-ADTConfig
{
    [CmdletBinding()]
    param
    (
    )

    # Return the config database if initialised.
    if (!($adtData = Get-ADTModuleData).Config -or !$adtData.Config.Count)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("Please ensure that [Initialize-ADTModule] is called before using any PSAppDeployToolkit functions.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTConfigNotLoaded'
            TargetObject = $adtData.Config
            RecommendedAction = "Please ensure the module is initialised via [Initialize-ADTModule] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $adtData.Config
}
