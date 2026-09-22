function Resolve-FluenceRestartOutcome
{
    <#
    .SYNOPSIS
        Maps a restart prompt's dialog result to 'Restart', 'Later', or 'TimedOut'.
    .DESCRIPTION
        A Restart flag wins, then TimedOut, and everything else (the Restart later button, Esc, the
        title-bar X, or a result with no recognised flag) is 'Later', the safe outcome.
    .PARAMETER Result
        The Fluence.DialogResult (or raw hashtable) from the restart prompt's Show-FluenceDialog call.
    .OUTPUTS
        System.String
    .NOTES
        Does not require a host application; pure logic helper for Show-FluenceRestartPrompt.
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param
    (
        [Parameter(Mandatory = $true)]
        [object]$Result
    )

    $values = @{}
    if ($Result -is [System.Collections.IDictionary])
    {
        foreach ($key in $Result.Keys)
        {
            $values[$key] = $Result[$key]
        }
    }
    else
    {
        foreach ($property in $Result.PSObject.Properties)
        {
            $values[$property.Name] = $property.Value
        }
    }

    if ($values['Restart'] -eq $true)
    {
        return 'Restart'
    }
    if ($values['TimedOut'] -eq $true)
    {
        return 'TimedOut'
    }
    return 'Later'
}
