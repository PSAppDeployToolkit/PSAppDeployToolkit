function Get-ADTStrings
{
    # Return the string database if initialised.
    if (!$Script:ADT.Strings -or !$Script:ADT.Strings.Count)
    {
        throw [System.InvalidOperationException]::new("Please ensure that [Open-ADTSession] is called before using any $($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name) functions.")
    }
    return $Script:ADT.Strings
}
