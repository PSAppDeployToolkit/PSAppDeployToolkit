function Write-ADTDebugFooter
{
    Write-ADTLogEntry -Message 'Function End' -Source (Get-PSCallStack)[1].InvocationInfo.MyCommand.Name -DebugMessage
}
