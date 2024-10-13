#-----------------------------------------------------------------------------
#
# MARK: Get-ADTSCCMClientVersion
#
#-----------------------------------------------------------------------------

function Get-ADTSCCMClientVersion
{
    # Make sure SCCM client is installed and running.
    Write-ADTLogEntry -Message 'Checking to see if SCCM Client service [ccmexec] is installed and running.'
    if (!(Test-ADTServiceExists -Name ccmexec))
    {
        $naerParams = @{
            Exception = [System.ApplicationException]::new('SCCM Client Service [ccmexec] does not exist. The SCCM Client may not be installed.')
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'CcmExecServiceMissing'
            RecommendedAction = "Please check the availability of this service and try again."
        }
        throw (New-ADTErrorRecord @naerParams)
    }
    if (($svc = Get-Service -Name ccmexec).Status -ne 'Running')
    {
        $naerParams = @{
            Exception = [System.ApplicationException]::new("SCCM Client Service [ccmexec] exists but it is not in a 'Running' state.")
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'CcmExecServiceNotRunning'
            TargetObject = $svc
            RecommendedAction = "Please check the status of this service and try again."
        }
        throw (New-ADTErrorRecord @naerParams)
    }

    # Determine the SCCM Client Version.
    try
    {
        [System.Version]$SCCMClientVersion = Get-CimInstance -Namespace ROOT\CCM -ClassName CCM_InstalledComponent | & { process { if ($_.Name -eq 'SmsClient') { $_.Version } } }
    }
    catch
    {
        Write-ADTLogEntry -Message "Failed to query the system for the SCCM client version number.`n$(Resolve-ADTErrorRecord -ErrorRecord $_)" -Severity 2
        throw
    }
    if (!$SCCMClientVersion)
    {
        $naerParams = @{
            Exception = [System.Data.NoNullAllowedException]::new('The query for the SmsClient version returned a null result.')
            Category = [System.Management.Automation.ErrorCategory]::InvalidResult
            ErrorId = 'CcmExecVersionNullOrEmpty'
            RecommendedAction = "Please check the installed version and try again."
        }
        throw (New-ADTErrorRecord @naerParams)
    }
    Write-ADTLogEntry -Message "Installed SCCM Client Version Number [$SCCMClientVersion]."
    return $SCCMClientVersion
}