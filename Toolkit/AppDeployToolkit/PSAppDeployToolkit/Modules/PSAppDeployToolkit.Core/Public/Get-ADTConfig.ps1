function Get-ADTConfig
{
    # Return the config database if initialised.
    if (!$Script:ADT.Config -or !$Script:ADT.Config.Count)
    {
        throw [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.")
    }
    return $Script:ADT.Config
}
