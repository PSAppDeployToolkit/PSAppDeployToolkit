function Test-ADTServiceExists
{
    <#

    .SYNOPSIS
    Check to see if a service exists.

    .DESCRIPTION
    Check to see if a service exists (using WMI method because Get-Service will generate ErrorRecord if service doesn't exist).

    .PARAMETER Name
    Specify the name of the service.

    Note: Service name can be found by executing "Get-Service | Format-Table -AutoSize -Wrap" or by using the properties screen of a service in services.msc.

    .PARAMETER ComputerName
    Specify the name of the computer. Default is: the local computer.

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

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Name,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ComputerName = $env:ComputerName,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process {
        # If nothing is returned from Win32_Service, check Win32_BaseService.
        if (!($ServiceObject = Get-CimInstance -ComputerName $ComputerName -ClassName Win32_Service -Filter "Name = '$Name'"))
        {
            $ServiceObject = Get-CimInstance -ComputerName $ComputerName -ClassName Win32_BaseService -Filter "Name = '$Name'"
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

    end {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
