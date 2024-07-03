function Complete-ADTFunction
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet
    )

    Write-ADTLogEntry -Message 'Function End' -Source $Cmdlet.MyInvocation.MyCommand.Name -DebugMessage
}
