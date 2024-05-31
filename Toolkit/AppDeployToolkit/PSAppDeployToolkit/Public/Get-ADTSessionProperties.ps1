function Get-ADTSessionProperties
{
    # Return the session's properties as a read-only dictionary.
    return (Get-ADTSession).Properties.AsReadOnly()
}
