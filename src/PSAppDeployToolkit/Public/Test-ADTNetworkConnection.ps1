#-----------------------------------------------------------------------------
#
# MARK: Test-ADTNetworkConnection
#
#-----------------------------------------------------------------------------

function Test-ADTNetworkConnection
{
    <#
    .SYNOPSIS
        Tests for an active local network connection, excluding wireless and virtual network adapters.

    .DESCRIPTION
        Tests for an active local network connection, excluding wireless and virtual network adapters, by querying the Win32_NetworkAdapter WMI class. This function checks if any physical network adapter is in the 'Up' status.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if a wired network connection is detected, otherwise returns $false.

    .EXAMPLE
        Test-ADTNetworkConnection

        Checks if there is an active wired network connection and returns true or false.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
    )

    begin
    {
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Checking if system is using a wired network connection...'
        try
        {
            try
            {
                if (& $Script:CommandTable.'Get-NetAdapter' -Physical | & { process { if ($_.Status.Equals('Up')) { return $_ } } } | & $Script:CommandTable.'Select-Object' -First 1)
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Wired network connection found.'
                    return $true
                }
                & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Wired network connection not found.'
                return $false
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
