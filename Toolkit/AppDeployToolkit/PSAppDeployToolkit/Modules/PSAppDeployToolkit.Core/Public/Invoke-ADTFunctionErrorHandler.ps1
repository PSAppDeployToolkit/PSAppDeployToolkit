function Invoke-ADTFunctionErrorHandler
{
    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]$Cmdlet,

        [Parameter(Mandatory = $true, ValueFromPipeline = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.ErrorRecord]$ErrorRecord
    )

    $ErrorActionPreference = $Cmdlet.SessionState.PSVariable.Get('OriginalErrorAction').Value
    $Cmdlet.WriteError($ErrorRecord)
}
