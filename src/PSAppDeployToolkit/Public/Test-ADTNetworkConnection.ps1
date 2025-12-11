#-----------------------------------------------------------------------------
#
# MARK: Test-ADTNetworkConnection
#
#-----------------------------------------------------------------------------

function Test-ADTNetworkConnection
{
    <#
    .SYNOPSIS
        Tests for an active local network connection; ethernet by default but can test for one or more connection types.

    .DESCRIPTION
        Tests for an active local network connection via Get-NetAdapter; ethernet by default but can test for one or more connection types. This function checks if any physical network adapter is in the 'Up' status.

    .PARAMETER InterfaceType
        Specifies one or more interface types to test. Defaults to `[PSADT.LibraryInterfaces.IF_TYPE]::IF_TYPE_ETHERNET_CSMACD` (Ethernet).

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
        [PSADT.LibraryInterfaces.IF_TYPE[]]$InterfaceType = [PSADT.LibraryInterfaces.IF_TYPE]::IF_TYPE_ETHERNET_CSMACD
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
                        Write-ADTLogEntry -Message "Active connection of type [$([PSADT.LibraryInterfaces.IF_TYPE]$adapter.InterfaceType)] found."
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
