#-----------------------------------------------------------------------------
#
# MARK: Get-ADTLogEntryCaller
#
#-----------------------------------------------------------------------------

function Get-ADTLogEntryCaller
{
    return (& $Script:CommandTable.'Get-PSCallStack' | & $Script:CommandTable.'Select-Object' -Skip 1 | & { process { if (![System.String]::IsNullOrWhiteSpace($_.Command) -and (($_.Command -notmatch '^(Write-(Log|ADTLogEntry)|<ScriptBlock>(<\w+>)?)$') -or (($_.Command -match '^(<ScriptBlock>(<\w+>)?)$') -and $_.Location.Equals('<No file>')))) { return $_ } } } | & $Script:CommandTable.'Select-Object' -First 1)
}
