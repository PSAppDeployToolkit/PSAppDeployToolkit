#-----------------------------------------------------------------------------
#
# MARK: Test-ADTNetworkConnection
#
#-----------------------------------------------------------------------------

function Test-ADTNetworkConnection
{
    <#
    .SYNOPSIS
        Tests for an active local network connection; ethernet/Wi-Fi by default but can test for a number of other connection types.

    .DESCRIPTION
        Tests for an active local network connection via Get-NetAdapter; ethernet/Wi-Fi by default but can test for a number of other connection types. This function checks if any of the nominated interface types is in the 'Up' status.

    .PARAMETER InterfaceType
        Specifies one or more interface types to test. Defaults to `Ethernet` and `Wireless80211` (Wi-Fi).

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

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTNetworkConnection
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Net.NetworkInformation.NetworkInterfaceType[]]$InterfaceType = ([System.Net.NetworkInformation.NetworkInterfaceType]::Ethernet, [System.Net.NetworkInformation.NetworkInterfaceType]::Wireless80211)
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        $connectionTypes = [System.String]::Join(', ', $InterfaceType)
        Write-ADTLogEntry -Message "Checking if system has an active connection of type [$connectionTypes]..."
        try
        {
            try
            {
                [System.UInt32[]]$interfaceTypes = $InterfaceType.value__
                foreach ($adapter in (Get-NetAdapter -Physical))
                {
                    if ($adapter.Status.Equals('Up') -and $interfaceTypes.Contains($adapter.InterfaceType))
                    {
                        Write-ADTLogEntry -Message "Active connection of type [$([System.Net.NetworkInformation.NetworkInterfaceType]$adapter.InterfaceType)] found."
                        return $true
                    }
                }
                Write-ADTLogEntry -Message "Active connection of type [$connectionTypes] not found."
                return $false
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
