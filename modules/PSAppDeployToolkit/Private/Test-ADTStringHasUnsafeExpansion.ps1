#-----------------------------------------------------------------------------
#
# MARK: Test-ADTStringHasUnsafeExpansion
#
#-----------------------------------------------------------------------------

function Private:Test-ADTStringHasUnsafeExpansion
{
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [AllowEmptyString()]
        [System.String]$InputString
    )

    # Expansion also evaluates $( ) and resolves ${ } through the provider, so ${C:\file.txt} hands back that
    # file's contents. Config values can arrive from Group Policy, so both are refused before anything runs.
    for ($i = 0; $i -lt $InputString.Length; $i++)
    {
        # A backtick escapes whatever follows it, so nothing is expanded there and there is nothing to refuse.
        if ($InputString[$i] -eq '`')
        {
            $i++
            continue
        }
        if (($InputString[$i] -ne '$') -or (($i + 1) -ge $InputString.Length))
        {
            continue
        }
        if ($InputString[$i + 1] -eq '(')
        {
            return $true
        }
        if ($InputString[$i + 1] -eq '{')
        {
            # A brace that never closes expands as nothing of the sort, and PowerShell reports it for itself.
            if (($close = $InputString.IndexOf('}', $i + 2)) -lt 0)
            {
                continue
            }
            if ($InputString.Substring($i + 2, $close - $i - 2) -notmatch '^(?:env:|global:|script:|local:|private:)?\w+$')
            {
                return $true
            }
            $i = $close
        }
    }
    return $false
}
