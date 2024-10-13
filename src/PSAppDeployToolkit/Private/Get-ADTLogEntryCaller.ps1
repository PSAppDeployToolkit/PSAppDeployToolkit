#-----------------------------------------------------------------------------
#
# MARK: Get-ADTLogEntryCaller
#
#-----------------------------------------------------------------------------

function Get-ADTLogEntryCaller
{
    return (Get-PSCallStack | Select-Object -Skip 1 | & { process { if (![System.String]::IsNullOrWhiteSpace($_.Command) -and (($_.Command -notmatch '^(Write-(Log|ADTLogEntry)|<ScriptBlock>(<\w+>)?)$') -or (($_.Command -match '^(<ScriptBlock>(<\w+>)?)$') -and $_.Location.Equals('<No file>')))) { return $_ } } } | Select-Object -First 1)
}
