#-----------------------------------------------------------------------------
#
# MARK: Get-ADTLogEntryCaller
#
#-----------------------------------------------------------------------------

function Get-ADTLogEntryCaller
{
    return (& $Script:CommandTable.'Get-PSCallStack' | & $Script:CommandTable.'Select-Object' -Skip 1 | & { process { if (![System.String]::IsNullOrWhiteSpace($_.Command) -and ($_.Command -notmatch '^(Write-(Log|ADTLogEntry)|<ScriptBlock>(<\w+>)?)$')) { return $_ } } } | & $Script:CommandTable.'Select-Object' -First 1)
}
