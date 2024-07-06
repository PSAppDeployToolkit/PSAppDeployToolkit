function Get-ADTStrings
{
    # Return the string database if initialised.
    if (!($adtData = Get-ADT).Strings -or !$adtData.Strings.Count)
    {
        throw [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any PSAppDeployToolkit functions.")
    }
    return $adtData.Strings
}
