#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Test-ADTServiceExists
{
    <#

    .SYNOPSIS
    Check to see if a service exists.

    .DESCRIPTION
    Check to see if a service exists. UseCIM switch can be used in conjunction with PassThru to return WMI objects for PSADT v3.x compatibility, however this method fails in Windows Sandbox.

    .PARAMETER Name
    Specify the name of the service.

    Note: Service name can be found by executing "Get-Service | Format-Table -AutoSize -Wrap" or by using the properties screen of a service in services.msc.

    .PARAMETER PassThru
    Return the WMI service object. To see all the properties use: Test-ADTServiceExists -Name 'spooler' -PassThru | Get-Member

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not return any objects.

    .EXAMPLE
    Test-ADTServiceExists -Name 'wuauserv'

    .EXAMPLE
    Test-ADTServiceExists -Name 'testservice' -PassThru | Where-Object { $_ } | ForEach-Object { $_.Delete() }

    Check if a service exists and then delete it by using the -PassThru parameter.

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
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
                    if (!($ServiceObject = & $Script:CommandTable.'Get-CimInstance' -ClassName Win32_Service -Filter "Name = '$Name'"))
                    {
                        $ServiceObject = & $Script:CommandTable.'Get-CimInstance' -ClassName Win32_BaseService -Filter "Name = '$Name'"
                    }
                }
                else
                {
                    # If the result is empty, it means the provided service is invalid.
                    $ServiceObject = & $Script:CommandTable.'Get-Service' -Name $Name -ErrorAction Ignore
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
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
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
