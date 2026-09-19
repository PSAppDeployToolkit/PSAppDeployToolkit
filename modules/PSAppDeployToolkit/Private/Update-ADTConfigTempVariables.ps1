#-----------------------------------------------------------------------------
#
# MARK: Update-ADTConfigTempVariables
#
#-----------------------------------------------------------------------------

function Private:Update-ADTConfigTempVariables
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]$Config
    )

    # We use `[System.IO.Path]::GetTempPath()` to ensure SYSTEM contexts use the hardened path of `C:\Windows\SystemTemp`.
    $tempPath = [System.IO.Path]::GetTempPath().TrimEnd('\') -replace '([`$])', '`$1'
    foreach ($section in $($Config.GetEnumerator()))
    {
        # Re-process if this is a hashtable.
        if ($section.Value -is [System.Collections.Hashtable])
        {
            Update-ADTConfigTempVariables -Config $section.Value
            continue
        }

        # Splitting and rejoining keeps the path out of the replacement, where a dollar in it would be read
        # as a capture group. The backticks added above are for the expander that reads the value after this.
        # The colon is optional so a config naming the module's own $envTemp settles on the same answer.
        if (($section.Value -is [System.String]) -and ($section.Value -match '\$env:?Temp\b'))
        {
            $Config.($section.Key) = [System.Text.RegularExpressions.Regex]::Split($section.Value, '\$env:?Temp\b', 'IgnoreCase') -join $tempPath
        }
    }
}
