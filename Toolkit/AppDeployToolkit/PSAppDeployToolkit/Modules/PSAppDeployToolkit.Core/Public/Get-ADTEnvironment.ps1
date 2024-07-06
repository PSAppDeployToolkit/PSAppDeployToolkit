function Get-ADTEnvironment
{
    [CmdletBinding()]
    param (
    )

    # Return the environment database if initialised.
    if (!($adtData = Get-ADTModuleData).Environment -or !$adtData.Environment.Count)
    {
        $naerParams = @{
            Exception = [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
            ErrorId = 'ADTEnvironmentDatabaseEmpty'
            TargetObject = $adtData.Environment
            RecommendedAction = "Please ensure the module is initialised via [Open-ADTSession] and try again."
        }
        $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
    }
    return $adtData.Environment
}
