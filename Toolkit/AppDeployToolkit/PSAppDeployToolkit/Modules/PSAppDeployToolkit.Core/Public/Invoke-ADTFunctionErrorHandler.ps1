function Invoke-ADTFunctionErrorHandler
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.SessionState]$SessionState,

        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    begin {
        $ErrorActionPreference = if ($SessionState.Equals($ExecutionContext.SessionState))
        {
            Get-Variable -Name OriginalErrorAction -Scope 1 -ValueOnly
        }
        else
        {
            $SessionState.PSVariable.Get('OriginalErrorAction').Value
        }
    }
    
    process {
        $Cmdlet.WriteError($ErrorRecord)
    }
}
