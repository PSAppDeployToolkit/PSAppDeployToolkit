function Get-ADTMsiExitCodeMessage
{
    <#

    .SYNOPSIS
    Get message for MSI error code

    .DESCRIPTION
    Get message for MSI error code by reading it from msimsg.dll

    .PARAMETER MsiExitCode
    MSI error code

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the message for the MSI error code.

    .EXAMPLE
    Get-ADTMsiExitCodeMessage -MsiErrorCode 1618

    .NOTES
    This is an internal script function and should typically not be called directly.

    .LINK
    http://msdn.microsoft.com/en-us/library/aa368542(v=vs.85).aspx

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$MsiExitCode
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                if (![System.String]::IsNullOrWhiteSpace(($res = [PSADT.Msi]::GetMessageFromMsiExitCode($MsiExitCode).Trim())))
                {
                    return $res
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
