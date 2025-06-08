#-----------------------------------------------------------------------------
#
# MARK: Test-ADTServiceExists
#
#-----------------------------------------------------------------------------

function Test-ADTServiceExists
{
    <#
    .SYNOPSIS
        Check to see if a service exists.

    .DESCRIPTION
        Check to see if a service exists. The UseCIM switch can be used in conjunction with PassThru to return WMI objects for PSADT v3.x compatibility, however, this method fails in Windows Sandbox.

    .PARAMETER Name
        Specify the name of the service.

        Note: Service name can be found by executing "Get-Service | Format-Table -AutoSize -Wrap" or by using the properties screen of a service in services.msc.

    .PARAMETER UseCIM
        Use CIM/WMI to check for the service. This is useful for compatibility with PSADT v3.x.

    .PARAMETER PassThru
        Return the WMI service object. To see all the properties use: Test-ADTServiceExists -Name 'spooler' -PassThru | Get-Member

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        System.Boolean

        Returns $true if the service exists, otherwise returns $false.

    .EXAMPLE
        Test-ADTServiceExists -Name 'wuauserv'

        Checks if the service 'wuauserv' exists.

    .EXAMPLE
        Test-ADTServiceExists -Name testservice -UseCIM -PassThru | Invoke-CimMethod -MethodName Delete

        Checks if a service exists and then deletes it by using the -PassThru parameter.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTServiceExists
    #>

    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [Alias('UseWMI')]
        [System.Management.Automation.SwitchParameter]$UseCIM,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
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
                # Access via CIM/WMI if specifically asked.
                if ($UseCIM)
                {
                    # If nothing is returned from Win32_Service, check Win32_BaseService.
                    if (!($ServiceObject = Get-CimInstance -ClassName Win32_Service -Filter "Name = '$Name'"))
                    {
                        $ServiceObject = Get-CimInstance -ClassName Win32_BaseService -Filter "Name = '$Name'"
                    }
                }
                else
                {
                    # If the result is empty, it means the provided service is invalid.
                    $ServiceObject = Get-Service -Name $Name -ErrorAction Ignore
                }

                # Return early if null.
                if (!$ServiceObject)
                {
                    Write-ADTLogEntry -Message "Service [$Name] does not exist."
                    return $false
                }
                Write-ADTLogEntry -Message "Service [$Name] exists."

                # Return the CIM object if passing through.
                if ($PassThru)
                {
                    return $ServiceObject
                }
                return $true
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed check to see if service [$Name] exists."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
