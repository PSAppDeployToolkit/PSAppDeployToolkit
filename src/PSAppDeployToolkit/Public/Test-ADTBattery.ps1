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
        Outputs an object containing the following properties:

        - ACPowerLineStatus
        - BatteryChargeStatus
        - BatteryLifePercent
        - BatterySaverEnabled
        - BatteryLifeRemaining
        - BatteryFullLifetime
        - IsUsingACPower
        - IsLaptop

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.Types.BatteryInfo

        Returns an object containing the following properties:

        - ACPowerLineStatus
        - BatteryChargeStatus
        - BatteryLifePercent
        - BatterySaverEnabled
        - BatteryLifeRemaining
        - BatteryFullLifetime
        - IsUsingACPower
        - IsLaptop

    .EXAMPLE
        Test-ADTBattery

        Checks if the local machine is running on AC power and returns true or false.

    .EXAMPLE
        (Test-ADTBattery -PassThru).IsLaptop

        Returns true if the current system is a laptop, otherwise false.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Test-ADTBattery
    #>

    [CmdletBinding()]
    [OutputType([PSADT.DeviceManagement.BatteryInfo])]
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
                # Determine if the system is using AC power.
                switch (($batteryInfo = [PSADT.DeviceManagement.BatteryInfo]::Get()).ACPowerLineStatus)
                {
                    ([PSADT.DeviceManagement.PowerLineStatus]::Online)
                    {
                        Write-ADTLogEntry -Message 'System is using AC power.'
                        break
                    }
                    ([PSADT.DeviceManagement.PowerLineStatus]::Offline)
                    {
                        Write-ADTLogEntry -Message 'System is using battery power.'
                        break
                    }
                    ([PSADT.DeviceManagement.PowerLineStatus]::Unknown)
                    {
                        if ($batteryInfo.IsBatteryInvalid())
                        {
                            Write-ADTLogEntry -Message "System power status is [$_] and battery charge status is [$($batteryInfo.BatteryChargeStatus)]. This is most likely due to a damaged battery so we will report system is using AC power."
                        }
                        else
                        {
                            Write-ADTLogEntry -Message "System power status is [$_] and battery charge status is [$($batteryInfo.BatteryChargeStatus)]. Therefore, we will report system is using battery power."
                        }
                        break
                    }
                }

                # Return the object if we're passing through, otherwise just whether we're on AC.
                if ($PassThru)
                {
                    return $batteryInfo
                }
                return $batteryInfo.IsUsingACPower
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
