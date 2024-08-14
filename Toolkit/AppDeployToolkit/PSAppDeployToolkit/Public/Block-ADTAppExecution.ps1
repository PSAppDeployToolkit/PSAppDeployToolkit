#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Block-ADTAppExecution
{
    <#

    .SYNOPSIS
    Block the execution of an application(s).

    .DESCRIPTION
    This function is called when you pass the -BlockExecution parameter to the Stop-RunningApplications function. It does the following:

    1.  Makes a copy of this script in a temporary directory on the local machine.
    2.  Checks for an existing scheduled task from previous failed installation attempt where apps were blocked and if found, calls the Unblock-ADTAppExecution function to restore the original IFEO registry keys.
            This is to prevent the function from overriding the backup of the original IFEO options.
    3.  Creates a scheduled task to restore the IFEO registry key values in case the script is terminated uncleanly by calling the local temporary copy of this script with the parameter -CleanupBlockedApps.
    4.  Modifies the "Image File Execution Options" registry key for the specified process(s) to call this script with the parameter -ShowBlockedAppDialog.
    5.  When the script is called with those parameters, it will display a custom message to the user to indicate that execution of the application has been blocked while the installation is in progress.
            The text of this message can be customized in the XML configuration file.

    .PARAMETER ProcessName
    Name of the process or processes separated by commas.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    Block-ADTAppExecution -ProcessName ('winword','excel')

    .NOTES
    It is used when the -BlockExecution parameter is specified with the Show-ADTInstallationWelcome function to block applications.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, HelpMessage = 'Specify process names, separated by commas.')]
        [ValidateNotNullOrEmpty()]
        [System.String[]]$ProcessName
    )

    begin
    {
        # Get everything we need before commencing.
        try
        {
            $adtEnv = Get-ADTEnvironment
            $adtConfig = Get-ADTConfig
            $adtSession = Get-ADTSession
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Define path for storing temporary data.
        $tempPath = $adtConfig.Toolkit.TempPath
        $taskName = "$($adtEnv.appDeployToolkitName)_$($adtSession.GetPropertyValue('installName'))_BlockedApps" -replace $adtEnv.InvalidScheduledTaskNameCharsRegExPattern
        $pwshArgs = "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command & $Script:CommandTable.'Import-Module' -Name '$((& $Script:CommandTable.'Get-Module' -Name "$($adtEnv.appDeployToolkitName)*").Name -replace '^',"$tempPath\" -join "', '")'"
    }

    process
    {
        # Bypass if no Admin rights.
        if (!$adtEnv.IsAdmin)
        {
            Write-ADTLogEntry -Message "Bypassing Function [$($MyInvocation.MyCommand.Name)], because [User: $($adtEnv.ProcessNTAccount)] is not admin."
            return
        }

        try
        {
            try
            {
                # Reset any previous instance of the temp folder.
                if ([System.IO.Directory]::Exists($tempPath))
                {
                    Remove-ADTFolder -Path $tempPath
                }
                try
                {
                    $null = [System.IO.Directory]::CreateDirectory($tempPath)
                }
                catch
                {
                    Write-ADTLogEntry -Message "Unable to create [$tempPath]. Possible attempt to gain elevated rights."
                }

                # Export the current state of the module for the scheduled task.
                & $Script:CommandTable.'Copy-Item' -Path $Script:PSScriptRoot* -Destination $tempPath -Exclude thumbs.db -Recurse -Force

                # Set contents to be readable for all users (BUILTIN\USERS).
                try
                {
                    Set-ADTItemPermission -Path $tempPath -User (ConvertTo-ADTNTAccountOrSID -SID S-1-5-32-545) -Permission Read -Inheritance ObjectInherit, ContainerInherit
                }
                catch
                {
                    Write-ADTLogEntry -Message "Failed to set read permissions on path [$tempPath]. The function might not be able to work correctly." -Severity 2
                }

                # Clean up any previous state that might be lingering.
                if ($task = & $Script:CommandTable.'Get-ScheduledTask' -TaskName $taskName -ErrorAction Ignore)
                {
                    Write-ADTLogEntry -Message "Scheduled task [$taskName] already exists, running [Unblock-ADTAppExecution] to clean up previous state."
                    Unblock-ADTAppExecution -Tasks $task
                }

                # Create a scheduled task to run on startup to call this script and clean up blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
                Write-ADTLogEntry -Message 'Creating scheduled task to cleanup blocked applications in case the installation is interrupted.'
                try
                {
                    $nstParams = @{
                        Principal = & $Script:CommandTable.'New-ScheduledTaskPrincipal' -Id Author -UserId S-1-5-18
                        Trigger = & $Script:CommandTable.'New-ScheduledTaskTrigger' -AtStartup
                        Action = & $Script:CommandTable.'New-ScheduledTaskAction' -Execute $adtEnv.envPSProcessPath -Argument "$pwshArgs; Unblock-ADTAppExecution"
                        Settings = & $Script:CommandTable.'New-ScheduledTaskSettingsSet' -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -DontStopOnIdleEnd -ExecutionTimeLimit ([System.TimeSpan]::FromHours(1))
                    }
                    $null = & $Script:CommandTable.'New-ScheduledTask' @nstParams | & $Script:CommandTable.'Register-ScheduledTask' -TaskName $taskName
                }
                catch
                {
                    Write-ADTLogEntry -Message "Failed to create the scheduled task [$taskName]." -Severity 3
                    return
                }

                # Enumerate each process and set the debugger value to block application execution.
                foreach ($process in $ProcessName -replace '$', '.exe')
                {
                    Write-ADTLogEntry -Message "Setting the Image File Execution Option registry key to block execution of [$process]."
                    Set-ADTRegistryKey -Key (& $Script:CommandTable.'Join-Path' -Path 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options' -ChildPath $process) -Name Debugger -Value "$([System.IO.Path]::GetFileName($adtEnv.envPSProcessPath)) $pwshArgs; Show-ADTBlockedAppDialog -Title '$($adtSession.GetPropertyValue('installName'))'"
                }
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
