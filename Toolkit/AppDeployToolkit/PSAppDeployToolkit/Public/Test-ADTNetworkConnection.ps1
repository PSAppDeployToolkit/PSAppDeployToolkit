#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Test-ADTNetworkConnection
{
    <#

    .SYNOPSIS
    Tests for an active local network connection, excluding wireless and virtual network adapters.

    .DESCRIPTION
    Tests for an active local network connection, excluding wireless and virtual network adapters, by querying the Win32_NetworkAdapter WMI class.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.Boolean. Returns $true if a wired network connection is detected, otherwise returns $false.

    .EXAMPLE
    Test-ADTNetworkConnection

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message 'Checking if system is using a wired network connection...'
        try
        {
            try
            {
                if (& $Script:CommandTable.'Get-NetAdapter' -Physical | & $Script:CommandTable.'Where-Object' {$_.Status.Equals('Up')})
                {
                    Write-ADTLogEntry -Message 'Wired network connection found.'
                    return $true
                }
                Write-ADTLogEntry -Message 'Wired network connection not found.'
                return $false
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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
