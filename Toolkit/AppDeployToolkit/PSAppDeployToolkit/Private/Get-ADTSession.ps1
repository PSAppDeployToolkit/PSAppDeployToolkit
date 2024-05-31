function Get-ADTSession
{
    # Return the most recent session in the database.
    if (!$Script:ADT.Sessions.Count)
    {
        throw [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any $($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name) functions.")
    }
    return $Script:ADT.Sessions[-1]
}
