#-----------------------------------------------------------------------------
#
# MARK: Invoke-ADTSCCMTask
#
#-----------------------------------------------------------------------------

function Invoke-ADTSCCMTask
{
    <#
    .SYNOPSIS
        Triggers SCCM to invoke the requested schedule task ID.

    .DESCRIPTION
        Triggers SCCM to invoke the requested schedule task ID. This function supports a variety of Schedule Id values as defined via https://learn.microsoft.com/en-us/intune/configmgr/develop/reference/core/clients/client-classes/triggerschedule-method-in-class-sms_client.

    .PARAMETER ScheduleId
        Name of the Schedule Id to trigger.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Invoke-ADTSCCMTask -ScheduleId 'SoftwareUpdatesScan'

        Triggers the 'SoftwareUpdatesScan' schedule task in SCCM.

    .EXAMPLE
        Invoke-ADTSCCMTask -ScheduleId 'HardwareInventory'

        Triggers the 'HardwareInventory' schedule task in SCCM.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Invoke-ADTSCCMTask
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [PSADT.ConfigMgr.TriggerScheduleId]$ScheduleId
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
                # Trigger SCCM task.
                Write-ADTLogEntry -Message "Triggering SCCM Task ID [$ScheduleId]."
                if (!($result = Invoke-CimMethod -Namespace ROOT\CCM -ClassName SMS_Client -MethodName TriggerSchedule -Arguments @{ sScheduleID = [System.Guid]::new([System.Byte[]](0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, ([System.Int32]$ScheduleId -band 0xFF00) -shr 8, [System.Int32]$ScheduleId -band 0xFF)).ToString('b') }))
                {
                    $naerParams = @{
                        Exception = [System.InvalidProgramException]::new("The TriggerSchedule method invocation returned no result.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'TriggerScheduleMethodNullResult'
                        TargetObject = $result
                        RecommendedAction = "Please confirm the status of the ccmexec client and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }
                if ($result.ReturnValue -ne 0)
                {
                    $naerParams = @{
                        Exception = [System.InvalidOperationException]::new("The TriggerSchedule method invocation returned an error code of [$($result.ReturnValue)].")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'TriggerScheduleMethodInvalidResult'
                        TargetObject = $result
                        RecommendedAction = "Please review the returned error value for the given ScheduleId and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to trigger SCCM Schedule Task ID [$ScheduleId]."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
