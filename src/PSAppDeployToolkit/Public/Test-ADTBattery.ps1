#-----------------------------------------------------------------------------
#
# MARK: Test-ADTBattery
#
#-----------------------------------------------------------------------------

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
                $acPowerLineStatus = [System.Windows.Forms.SystemInformation]::PowerStatus.PowerLineStatus

                # Get the current battery charge status. Possible values: High, Low, Critical, Charging, NoSystemBattery, Unknown.
                $batteryChargeStatus = [System.Windows.Forms.SystemInformation]::PowerStatus.BatteryChargeStatus
                $invalidBattery = ($batteryChargeStatus -eq 'NoSystemBattery') -or ($batteryChargeStatus -eq 'Unknown')

                # Get the approximate amount, from 0.00 to 1.0, of full battery charge remaining.
                # This property can report 1.0 when the battery is damaged and Windows can't detect a battery.
                # Therefore, this property is only indicative of battery charge remaining if 'BatteryChargeStatus' property is not reporting 'NoSystemBattery' or 'Unknown'.
                $batteryLifePercent = [System.Windows.Forms.SystemInformation]::PowerStatus.BatteryLifePercent * !$invalidBattery

                # The reported approximate number of seconds of battery life remaining. It will report -1 if the remaining life is unknown because the system is on AC power.
                $batteryLifeRemainingSeconds = [System.Windows.Forms.SystemInformation]::PowerStatus.BatteryLifeRemaining

                # Get the manufacturer reported full charge lifetime of the primary battery power source in seconds.
                # The reported number of seconds of battery life available when the battery is fully charged, or -1 if it is unknown.
                # This will only be reported if the battery supports reporting this information. You will most likely get -1, indicating unknown.
                $batteryFullLifetimeSeconds = [System.Windows.Forms.SystemInformation]::PowerStatus.BatteryFullLifetime

                # Determine if the system is using AC power.
                $isUsingAcPower = switch ($acPowerLineStatus)
                {
                    Online
                    {
                        Write-ADTLogEntry -Message 'System is using AC power.'
                        $true
                        break
                    }
                    Offline
                    {
                        Write-ADTLogEntry -Message 'System is using battery power.'
                        $false
                        break
                    }
                    Unknown
                    {
                        if ($invalidBattery)
                        {
                            Write-ADTLogEntry -Message "System power status is [$($acPowerLineStatus)] and battery charge status is [$batteryChargeStatus]. This is most likely due to a damaged battery so we will report system is using AC power."
                            $true
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "System power status is [$($acPowerLineStatus)] and battery charge status is [$batteryChargeStatus]. Therefore, we will report system is using battery power."
                            $false
                        }
                        break
                    }
                }

                # Determine if the system is a laptop.
                $isLaptop = !$invalidBattery -and ((Get-CimInstance -ClassName Win32_SystemEnclosure).ChassisTypes -match '^(9|10|14)$')

                # Return the object if we're passing through, otherwise just whether we're on AC.
                if ($PassThru)
                {
                    return [PSADT.Types.BatteryInfo]::new(
                        $acPowerLineStatus,
                        $batteryChargeStatus,
                        $batteryLifePercent,
                        $batteryLifeRemainingSeconds,
                        $batteryFullLifetimeSeconds,
                        $isUsingAcPower,
                        $isLaptop
                    )
                }
                return $isUsingAcPower
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
