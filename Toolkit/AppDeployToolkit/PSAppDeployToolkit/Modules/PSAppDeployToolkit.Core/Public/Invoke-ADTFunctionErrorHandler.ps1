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
        [System.Management.Automation.ErrorRecord]$ErrorRecord,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Prefix
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
        if ($ErrorRecord.CategoryInfo.Activity.Equals('Write-Error'))
        {
            $ErrorRecord.CategoryInfo.Activity = $Cmdlet.MyInvocation.MyCommand.Name
        }
        if ($Prefix)
        {
            if (!$ErrorActionPreference.Equals([System.Management.Automation.ActionPreference]::Stop))
            {
                $Prefix += "`n$(Resolve-ADTError -ErrorRecord $ErrorRecord)"
            }
            Write-ADTLogEntry -Message $Prefix -Source $Cmdlet.MyInvocation.MyCommand.Name -Severity 3
        }
        $Cmdlet.WriteError($ErrorRecord)
    }
}
