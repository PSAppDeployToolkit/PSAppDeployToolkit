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

    .LINK
    https://psappdeploytoolkit.com

    #>

    begin {
        Write-ADTDebugHeader
    }

    process {
        Write-ADTLogEntry -Message 'Checking if system is using a wired network connection...'
        if (Get-NetAdapter -Physical | Where-Object {$_.Status.Equals('Up')})
        {
            Write-ADTLogEntry -Message 'Wired network connection found.'
            return $true
        }
        Write-ADTLogEntry -Message 'Wired network connection not found.'
        return $false
    }

    end {
        Write-ADTDebugFooter
    }
}
