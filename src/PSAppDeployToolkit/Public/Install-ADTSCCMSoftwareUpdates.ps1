#-----------------------------------------------------------------------------
#
# MARK: Install-ADTSCCMSoftwareUpdates
#
#-----------------------------------------------------------------------------

function Install-ADTSCCMSoftwareUpdates
{
    <#
    .SYNOPSIS
        Scans for outstanding SCCM updates to be installed and installs the pending updates.

    .DESCRIPTION
        Scans for outstanding SCCM updates to be installed and installs the pending updates.

        Only compatible with SCCM 2012 Client or higher. This function can take several minutes to run.

    .PARAMETER SoftwareUpdatesScanWaitInSeconds
        The amount of time to wait in seconds for the software updates scan to complete.

    .PARAMETER WaitForPendingUpdatesTimeout
        The amount of time to wait for missing and pending updates to install before exiting the function.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any objects.

    .EXAMPLE
        Install-ADTSCCMSoftwareUpdates

        Scans for outstanding SCCM updates and installs the pending updates with default wait times.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Install-ADTSCCMSoftwareUpdates
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Int32]]$SoftwareUpdatesScanWaitInSeconds = 180,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$WaitForPendingUpdatesTimeout = [System.TimeSpan]::FromMinutes(45)
    )

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $StartTime = [System.DateTime]::Now
    }

    process
    {
        # Trigger SCCM client scan for Software Updates.
        try
        {
            Write-ADTLogEntry -Message 'Triggering SCCM client scan for Software Updates...'; Invoke-ADTSCCMTask -ScheduleID ([PSADT.ConfigMgr.TriggerScheduleId]::SoftwareUpdatesScan)
            Write-ADTLogEntry -Message "Suspending this thread for [$SoftwareUpdatesScanWaitInSeconds] seconds to let the update scan finish."
            [System.Threading.Thread]::Sleep($SoftwareUpdatesScanWaitInSeconds * 1000)
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }

        # Find the number of missing updates.
        Write-ADTLogEntry -Message 'Getting the number of missing updates...'
        try
        {
            try
            {
                [Microsoft.Management.Infrastructure.CimInstance[]]$CMMissingUpdates = Get-CimInstance -Namespace ROOT\CCM\ClientSDK -Query "SELECT * FROM CCM_SoftwareUpdate WHERE ComplianceState = '0'"
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to find the number of missing software updates."
        }

        # Return early if there's no missing updates to install.
        if (!$CMMissingUpdates -or !$CMMissingUpdates.Count)
        {
            Write-ADTLogEntry -Message 'There are no missing updates.'
            return
        }

        try
        {
            try
            {
                # Install missing updates.
                Write-ADTLogEntry -Message "Installing missing updates. The number of missing updates is [$($CMMissingUpdates.Count)]."
                if (!($result = Invoke-CimMethod -Namespace ROOT\CCM\ClientSDK -ClassName CCM_SoftwareUpdatesManager -MethodName InstallUpdates -Arguments @{ CCMUpdates = $CMMissingUpdates }))
                {
                    $naerParams = @{
                        Exception = [System.InvalidProgramException]::new("The InstallUpdates method invocation returned no result.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'InstallUpdatesMethodNullResult'
                        TargetObject = $result
                        RecommendedAction = "Please confirm the status of the ccmexec client and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }
                if ($result.ReturnValue -ne 0)
                {
                    $naerParams = @{
                        Exception = [System.InvalidOperationException]::new("The InstallUpdates method invocation returned an error code of [$($result.ReturnValue)].")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'InstallUpdatesMethodInvalidResult'
                        TargetObject = $result
                        RecommendedAction = "Please review the returned error value for the InstallUpdates method and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Wait for pending updates to finish installing or the timeout value to expire.
                do
                {
                    Start-Sleep -Seconds 60; [Microsoft.Management.Infrastructure.CimInstance[]]$CMInstallPendingUpdates = Get-CimInstance -Namespace ROOT\CCM\ClientSDK -Query 'SELECT * FROM CCM_SoftwareUpdate WHERE EvaluationState = 6 or EvaluationState = 7'
                    Write-ADTLogEntry -Message "The number of updates pending installation is [$(if ($CMInstallPendingUpdates) { $CMInstallPendingUpdates.Count } else { 0 })]."
                }
                while ($CMInstallPendingUpdates -and $CMInstallPendingUpdates.Count -and ([System.DateTime]::Now - $StartTime) -lt $WaitForPendingUpdatesTimeout)
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_ -LogMessage "Failed to trigger installation of missing software updates."
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
