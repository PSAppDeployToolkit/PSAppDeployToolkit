#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Test-ADTBattery
{
    <#
    .SYNOPSIS
        Tests whether the local machine is running on AC power or not.

    .DESCRIPTION
        Tests whether the local machine is running on AC power and returns true/false. For detailed information, use the -PassThru option to get a hashtable containing various battery and power status properties.

    .PARAMETER PassThru
        Outputs a hashtable containing the following properties:
        - IsLaptop
        - IsUsingACPower
        - ACPowerLineStatus
        - BatteryChargeStatus
        - BatteryLifePercent
        - BatteryLifeRemaining
        - BatteryFullLifetime

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.BatteryInfo

        Returns a hashtable containing the following properties:
        - IsLaptop
        - IsUsingACPower
        - ACPowerLineStatus
        - BatteryChargeStatus
        - BatteryLifePercent
        - BatteryLifeRemaining
        - BatteryFullLifetime

    .EXAMPLE
        Test-ADTBattery

        Checks if the local machine is running on AC power and returns true or false.

    .EXAMPLE
        # Determine if the current system is a laptop or not.
        (Test-ADTBattery -PassThru).IsLaptop

        Returns true if the current system is a laptop, otherwise false.

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
    [OutputType([PSADT.Types.BatteryInfo])]
    param
    (
        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PassThru
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        Write-ADTLogEntry -Message 'Checking if system is using AC power or if it is running on battery...'
        try
        {
            try
            {
                # Get the system power status. Indicates whether the system is using AC power or if the status is unknown. Possible values:
                # Offline : The system is not using AC power.
                # Online  : The system is using AC power.
                # Unknown : The power status of the system is unknown.
                $powerStatus = @{ ACPowerLineStatus = [System.Windows.Forms.SystemInformation]::PowerStatus.PowerLineStatus }

                # Get the current battery charge status. Possible values: High, Low, Critical, Charging, NoSystemBattery, Unknown.
                $powerStatus.Add('BatteryChargeStatus', [System.Windows.Forms.SystemInformation]::PowerStatus.BatteryChargeStatus)
                $invalidBattery = ($powerStatus.BatteryChargeStatus -eq 'NoSystemBattery') -or ($powerStatus.BatteryChargeStatus -eq 'Unknown')

                # Get the approximate amount, from 0.00 to 1.0, of full battery charge remaining.
                # This property can report 1.0 when the battery is damaged and Windows can't detect a battery.
                # Therefore, this property is only indicative of battery charge remaining if 'BatteryChargeStatus' property is not reporting 'NoSystemBattery' or 'Unknown'.
                $powerStatus.Add('BatteryLifePercent', [System.Windows.Forms.SystemInformation]::PowerStatus.BatteryLifePercent * !$invalidBattery)

                # The reported approximate number of seconds of battery life remaining. It will report -1 if the remaining life is unknown because the system is on AC power.
                $powerStatus.Add('BatteryLifeRemaining', [System.Windows.Forms.SystemInformation]::PowerStatus.BatteryLifeRemaining)

                # Get the manufacturer reported full charge lifetime of the primary battery power source in seconds.
                # The reported number of seconds of battery life available when the battery is fully charged, or -1 if it is unknown.
                # This will only be reported if the battery supports reporting this information. You will most likely get -1, indicating unknown.
                $powerStatus.Add('BatteryFullLifetime', [System.Windows.Forms.SystemInformation]::PowerStatus.BatteryFullLifetime)

                # Determine if the system is using AC power.
                $powerStatus.Add('IsUsingACPower', $(switch ($powerStatus.ACPowerLineStatus)
                        {
                            Online
                            {
                                Write-ADTLogEntry -Message 'System is using AC power.'
                                $true
                            }
                            Offline
                            {
                                Write-ADTLogEntry -Message 'System is using battery power.'
                                $false
                            }
                            Unknown
                            {
                                if ($invalidBattery)
                                {
                                    Write-ADTLogEntry -Message "System power status is [$($powerStatus.ACPowerLineStatus)] and battery charge status is [$($powerStatus.BatteryChargeStatus)]. This is most likely due to a damaged battery so we will report system is using AC power."
                                    $true
                                }
                                else
                                {
                                    Write-ADTLogEntry -Message "System power status is [$($powerStatus.ACPowerLineStatus)] and battery charge status is [$($powerStatus.BatteryChargeStatus)]. Therefore, we will report system is using battery power."
                                    $false
                                }
                            }
                        }))

                # Determine if the system is a laptop.
                $powerStatus.Add('IsLaptop', !$invalidBattery -and ((& $Script:CommandTable.'Get-CimInstance' -ClassName Win32_SystemEnclosure).ChassisTypes -match '^(9|10|14)$'))

                # Return the object if we're passing through, otherwise just whether we're on AC.
                if ($PassThru)
                {
                    return [PSADT.Types.BatteryInfo]$powerStatus
                }
                return $powerStatus.IsUsingACPower
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
