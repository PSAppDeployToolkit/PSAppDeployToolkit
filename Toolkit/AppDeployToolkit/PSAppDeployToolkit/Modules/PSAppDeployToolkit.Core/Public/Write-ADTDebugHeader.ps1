function Write-ADTDebugHeader
{
    Write-ADTLogEntry -Message 'Function Start' -Source ($caller = (Get-PSCallStack)[1].InvocationInfo).MyCommand.Name -DebugMessage
    if ($CmdletBoundParameters = $caller.BoundParameters | Format-Table -Property @{ Label = 'Parameter'; Expression = { "[-$($_.Key)]" } }, @{ Label = 'Value'; Expression = { $_.Value }; Alignment = 'Left' }, @{ Label = 'Type'; Expression = { $_.Value.GetType().Name }; Alignment = 'Left' } -AutoSize -Wrap | Out-String)
    {
        Write-ADTLogEntry -Message "Function invoked with bound parameter(s):`n$CmdletBoundParameters" -Source $caller.MyCommand.Name -DebugMessage
    }
    else
    {
        Write-ADTLogEntry -Message 'Function invoked without any bound parameters.' -Source $caller.MyCommand.Name -DebugMessage
    }
}
