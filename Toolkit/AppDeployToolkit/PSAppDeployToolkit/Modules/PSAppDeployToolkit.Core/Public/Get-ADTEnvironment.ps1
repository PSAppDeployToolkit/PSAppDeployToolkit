function Get-ADTEnvironment
{
    # Return the environment database if initialised.
    if (!($adtData = Get-ADT).Environment -or !$adtData.Environment.Count)
    {
        throw [System.InvalidOperationException]::new("Please ensure that [Initialize-ADTModule] is called before using any PSAppDeployToolkit functions.")
    }
    return $adtData.Environment
}
