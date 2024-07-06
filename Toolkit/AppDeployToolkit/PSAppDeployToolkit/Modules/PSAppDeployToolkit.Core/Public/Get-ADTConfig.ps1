function Get-ADTConfig
{
    # Return the config database if initialised.
    if (!($adtData = Get-ADT).Config -or !$adtData.Config.Count)
    {
        throw [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.")
    }
    return $adtData.Config
}
