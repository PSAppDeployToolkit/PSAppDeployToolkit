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
        [System.Int32]$SoftwareUpdatesScanWaitInSeconds = 180,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.TimeSpan]$WaitForPendingUpdatesTimeout = [System.TimeSpan]::FromMinutes(45)
    )

    begin
    {
        # Make this function continue on error.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorAction SilentlyContinue
    }

    process
    {
        try
        {
            try
            {
                # If SCCM 2007 Client or lower, exit function.
                if (($SCCMClientVersion = Get-ADTSCCMClientVersion).Major -le 4)
                {
                    $naerParams = @{
                        Exception = [System.Data.VersionNotFoundException]::new('SCCM 2007 or lower, which is incompatible with this function, was detected on this system.')
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'CcmExecVersionLowerThanMinimum'
                        TargetObject = $SCCMClientVersion
                        RecommendedAction = "Please review the installed CcmExec client and try again."
                    }
                    throw (New-ADTErrorRecord @naerParams)
                }

                # Trigger SCCM client scan for Software Updates.
                $StartTime = [System.DateTime]::Now
                Write-ADTLogEntry -Message 'Triggering SCCM client scan for Software Updates...'
                Invoke-ADTSCCMTask -ScheduleID 'SoftwareUpdatesScan'
                Write-ADTLogEntry -Message "The SCCM client scan for Software Updates has been triggered. The script is suspended for [$SoftwareUpdatesScanWaitInSeconds] seconds to let the update scan finish."
                Start-Sleep -Seconds $SoftwareUpdatesScanWaitInSeconds

                # Find the number of missing updates.
                try
                {
                    Write-ADTLogEntry -Message 'Getting the number of missing updates...'
                    [Microsoft.Management.Infrastructure.CimInstance[]]$CMMissingUpdates = Get-CimInstance -Namespace ROOT\CCM\ClientSDK -Query "SELECT * FROM CCM_SoftwareUpdate WHERE ComplianceState = '0'"
                }
                catch
                {
                    Write-ADTLogEntry -Message "Failed to find the number of missing software updates.`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 2
                    throw
                }

                # Install missing updates and wait for pending updates to finish installing.
                if (!$CMMissingUpdates.Count)
                {
                    Write-ADTLogEntry -Message 'There are no missing updates.'
                    return
                }

                # Install missing updates.
                Write-ADTLogEntry -Message "Installing missing updates. The number of missing updates is [$($CMMissingUpdates.Count)]."
                $null = Invoke-CimMethod -Namespace ROOT\CCM\ClientSDK -ClassName CCM_SoftwareUpdatesManager -MethodName InstallUpdates -Arguments @{ CCMUpdates = $CMMissingUpdates }

                # Wait for pending updates to finish installing or the timeout value to expire.
                do
                {
                    Start-Sleep -Seconds 60
                    [Microsoft.Management.Infrastructure.CimInstance[]]$CMInstallPendingUpdates = Get-CimInstance -Namespace ROOT\CCM\ClientSDK -Query 'SELECT * FROM CCM_SoftwareUpdate WHERE EvaluationState = 6 or EvaluationState = 7'
                    Write-ADTLogEntry -Message "The number of updates pending installation is [$($CMInstallPendingUpdates.Count)]."
                }
                while (($CMInstallPendingUpdates.Count -ne 0) -and ([System.DateTime]::Now - $StartTime) -lt $WaitForPendingUpdatesTimeout)
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
