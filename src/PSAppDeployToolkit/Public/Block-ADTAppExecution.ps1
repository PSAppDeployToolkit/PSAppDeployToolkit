#-----------------------------------------------------------------------------
#
# MARK: Block-ADTAppExecution
#
#-----------------------------------------------------------------------------

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
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Block-ADTAppExecution -ProcessName ('winword','excel')

        This example blocks the execution of Microsoft Word and Excel.

    .NOTES
        It is used when the -BlockExecution parameter is specified with the Show-ADTInstallationWelcome function to block applications.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
        License: https://opensource.org/license/lgpl-3-0

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
            $adtEnv = & $Script:CommandTable.'Get-ADTEnvironment'
            $adtSession = & $Script:CommandTable.'Get-ADTSession'
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        & $Script:CommandTable.'Initialize-ADTFunction' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $taskName = "$($adtEnv.appDeployToolkitName)_$($adtSession.GetPropertyValue('installName'))_BlockedApps" -replace $adtEnv.InvalidScheduledTaskNameCharsRegExPattern
    }

    process
    {
        # Bypass if no Admin rights.
        if (!$adtEnv.IsAdmin)
        {
            & $Script:CommandTable.'Write-ADTLogEntry' -Message "Bypassing Function [$($MyInvocation.MyCommand.Name)], because [User: $($adtEnv.ProcessNTAccount)] is not admin."
            return
        }

        try
        {
            try
            {
                # Clean up any previous state that might be lingering.
                if ($task = & $Script:CommandTable.'Get-ScheduledTask' -TaskName $taskName -ErrorAction Ignore)
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Scheduled task [$taskName] already exists, running [Unblock-ADTAppExecution] to clean up previous state."
                    & $Script:CommandTable.'Unblock-ADTAppExecution' -Tasks $task
                }

                # Create a scheduled task to run on startup to call this script and clean up blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
                & $Script:CommandTable.'Write-ADTLogEntry' -Message 'Creating scheduled task to cleanup blocked applications in case the installation is interrupted.'
                try
                {
                    $nstParams = @{
                        Principal = & $Script:CommandTable.'New-ScheduledTaskPrincipal' -Id Author -UserId S-1-5-18
                        Trigger = & $Script:CommandTable.'New-ScheduledTaskTrigger' -AtStartup
                        Action = & $Script:CommandTable.'New-ScheduledTaskAction' -Execute $adtEnv.envPSProcessPath -Argument "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -EncodedCommand $(& $Script:CommandTable.'Out-ADTPowerShellEncodedCommand' -Command "& {$((& $Script:CommandTable.'Unblock-ADTAppExecutionInternal').ScriptBlock)} -TaskName '$($taskName.Replace("'", "''"))'")"
                        Settings = & $Script:CommandTable.'New-ScheduledTaskSettingsSet' -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -DontStopOnIdleEnd -ExecutionTimeLimit ([System.TimeSpan]::FromHours(1))
                    }
                    $null = & $Script:CommandTable.'New-ScheduledTask' @nstParams | & $Script:CommandTable.'Register-ScheduledTask' -TaskName $taskName
                }
                catch
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Failed to create the scheduled task [$taskName]." -Severity 3
                    return
                }

                # Enumerate each process and set the debugger value to block application execution.
                foreach ($process in ($ProcessName -replace '$', '.exe'))
                {
                    & $Script:CommandTable.'Write-ADTLogEntry' -Message "Setting the Image File Execution Option registry key to block execution of [$process]."
                    & $Script:CommandTable.'Set-ADTRegistryKey' -Key (& $Script:CommandTable.'Join-Path' -Path 'Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options' -ChildPath $process) -Name Debugger -Value "$([System.IO.Path]::GetFileName($adtEnv.envPSProcessPath)) -ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command Import-Module -Name '$($Script:PSScriptRoot)\$($MyInvocation.MyCommand.Module.Name).psd1'; Show-ADTBlockedAppDialog -Title '$($adtSession.GetPropertyValue('InstallName').Replace("'","''"))'"
                }

                # Add callback to remove all blocked app executions during the shutdown of the final session.
                & $Script:CommandTable.'Add-ADTSessionFinishingCallback' -Callback $Script:CommandTable.'Unblock-ADTAppExecution'
            }
            catch
            {
                & $Script:CommandTable.'Write-Error' -ErrorRecord $_
            }
        }
        catch
        {
            & $Script:CommandTable.'Invoke-ADTFunctionErrorHandler' -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
        }
    }

    end
    {
        & $Script:CommandTable.'Complete-ADTFunction' -Cmdlet $PSCmdlet
    }
}
