function Get-ADTEnvironment
{
    # Return the environment database if initialised.
    if (!$Script:ADT.Environment -or !$Script:ADT.Environment.Count)
    {
        throw [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.")
    }
    return $Script:ADT.Environment
}
