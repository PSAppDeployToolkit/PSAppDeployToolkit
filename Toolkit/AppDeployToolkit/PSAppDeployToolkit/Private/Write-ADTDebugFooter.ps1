function Write-ADTDebugFooter
{
    Write-ADTLogEntry -Message 'Function End' -Source (Get-Variable -Name MyInvocation -Scope 1 -ValueOnly).MyCommand.Name -DebugMessage
}
