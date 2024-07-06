function Get-ADTSession
{
    # Return the most recent session in the database.
    if (!($adtData = Get-ADT).Sessions.Count)
    {
        throw [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.")
    }
    return $adtData.Sessions[-1]
}
