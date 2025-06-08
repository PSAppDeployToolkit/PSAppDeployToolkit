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

        1) Makes a copy of this script in a temporary directory on the local machine.
        2) Checks for an existing scheduled task from previous failed installation attempt where apps were blocked and if found, calls the Unblock-ADTAppExecution function to restore the original IFEO registry keys. This is to prevent the function from overriding the backup of the original IFEO options.
        3) Creates a scheduled task to restore the IFEO registry key values in case the script is terminated uncleanly by calling `Unblock-ADTAppExecution` the local temporary copy of this module.
        4) Modifies the "Image File Execution Options" registry key for the specified process(s) to call `Show-ADTInstallationPrompt` with the appropriate messaging via this module.
        5) When the script is called with those parameters, it will display a custom message to the user to indicate that execution of the application has been blocked while the installation is in progress. The text of this message can be customized in the strings.psd1 file.

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
        An active ADT session is required to use this function.

        It is used when the -BlockExecution parameter is specified with the Show-ADTInstallationWelcome function to block applications.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Block-ADTAppExecution
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
            $adtSession = Get-ADTSession
            $adtEnv = Get-ADTEnvironmentTable
            $adtConfig = Get-ADTConfig
            $adtStrings = Get-ADTStringTable
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $taskName = "$($adtEnv.appDeployToolkitName)_$($adtSession.InstallName)_BlockedApps" -replace $adtEnv.InvalidScheduledTaskNameCharsRegExPattern

        # Announce function's deprecation to all callers.
        Write-ADTLogEntry -Message "The block execution technology is now deprecated and will be removed in PSAppDeployToolkit 4.2.0. Please see [https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/issues/1416] for more information." -Severity 2
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
                # Clean up any previous state that might be lingering.
                if ($task = Get-ScheduledTask -TaskName $taskName -ErrorAction Ignore)
                {
                    Write-ADTLogEntry -Message "Scheduled task [$taskName] already exists, running [Unblock-ADTAppExecution] to clean up previous state."
                    Unblock-ADTAppExecution -Tasks $task
                }

                # Create a scheduled task to run on startup to call this script and clean up blocked applications in case the installation is interrupted, e.g. user shuts down during installation"
                Write-ADTLogEntry -Message 'Creating scheduled task to cleanup blocked applications in case the installation is interrupted.'
                try
                {
                    $nstParams = @{
                        Principal = New-ScheduledTaskPrincipal -Id Author -UserId S-1-5-18
                        Trigger = New-ScheduledTaskTrigger -AtStartup
                        Action = New-ScheduledTaskAction -Execute $adtEnv.envPSProcessPath -Argument "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -EncodedCommand $(Out-ADTPowerShellEncodedCommand -Command "& {$($Script:CommandTable.'Unblock-ADTAppExecutionInternal'.ScriptBlock)} -TaskName '$($taskName.Replace("'", "''"))'")"
                        Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -DontStopOnIdleEnd -ExecutionTimeLimit ([System.TimeSpan]::FromHours(1))
                    }
                    $null = New-ScheduledTask @nstParams | Register-ScheduledTask -TaskName $taskName
                }
                catch
                {
                    Write-ADTLogEntry -Message "Failed to create the scheduled task [$taskName]." -Severity 3
                    return
                }

                # Configure the appropriate permissions for the client/server process.
                Set-ADTClientServerProcessPermissions

                # Build out hashtable of parameters needed to construct the dialog.
                $dialogOptions = @{
                    AppTitle = $adtSession.InstallTitle
                    Subtitle = $adtStrings.BlockExecutionText.Subtitle.($DeploymentType.ToString())
                    AppIconImage = $adtConfig.Assets.Logo
                    AppBannerImage = $adtConfig.Assets.Banner
                    DialogTopMost = $true
                    MinimizeWindows = $false
                    DialogExpiryDuration = [System.TimeSpan]::FromSeconds($adtConfig.UI.DefaultTimeout)
                    MessageText = $adtStrings.BlockExecutionText.Message.($DeploymentType.ToString())
                    Icon = [PSADT.UserInterface.Dialogs.DialogSystemIcon]::Warning
                    ButtonRightText = 'OK'
                }
                if ($null -ne $adtConfig.UI.FluentAccentColor)
                {
                    $dialogOptions.Add('FluentAccentColor', $adtConfig.UI.FluentAccentColor)
                }

                # Store the BlockExection command in the registry due to IFEO length issues when > 255 chars.
                $blockExecRegPath = Convert-ADTRegistryPath -Key (Join-Path -Path $adtConfig.Toolkit.RegPath -ChildPath $adtEnv.appDeployToolkitName)
                $blockExecCommand = "& '$($Script:PSScriptRoot)\lib\PSADT.ClientServer.Client.exe' /SingleDialog -DialogType $([PSADT.UserInterface.Dialogs.DialogType]::CustomDialog) -DialogStyle $($adtConfig.UI.DialogStyle) -DialogOptions $([PSADT.Utilities.SerializationUtilities]::SerializeToString([PSADT.UserInterface.DialogOptions.CustomDialogOptions]$dialogOptions, [PSADT.UserInterface.DialogOptions.CustomDialogOptions]))"
                $blockExecDbgPath = "conhost.exe --headless $([System.IO.Path]::GetFileName($adtEnv.envPSProcessPath)) -NonInteractive -NoProfile -Command & ([scriptblock]::Create([Microsoft.Win32.Registry]::GetValue('$($blockExecRegPath -replace '^Microsoft\.PowerShell\.Core\\Registry::')', 'BlockExecutionCommand', `$null))); #"
                Set-ADTRegistryKey -Key $blockExecRegPath -Name BlockExecutionCommand -Value $blockExecCommand

                # Enumerate each process and set the debugger value to block application execution.
                foreach ($process in $ProcessName)
                {
                    Write-ADTLogEntry -Message "Setting the Image File Execution Option registry key to block execution of [$process]."
                    if ([System.IO.Path]::IsPathRooted($process))
                    {
                        $basePath = "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\$($process -replace '^.+\\')"
                        [Microsoft.Win32.Registry]::SetValue("$basePath\MyFilter", 'Debugger', $blockExecDbgPath, [Microsoft.Win32.RegistryValueKind]::String)
                        [Microsoft.Win32.Registry]::SetValue("$basePath\MyFilter", 'FilterFullPath', $process, [Microsoft.Win32.RegistryValueKind]::String)
                        [Microsoft.Win32.Registry]::SetValue($basePath, 'UseFilter', $true, [Microsoft.Win32.RegistryValueKind]::DWord)
                    }
                    else
                    {
                        [Microsoft.Win32.Registry]::SetValue("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\$process.exe", 'Debugger', $blockExecDbgPath, [Microsoft.Win32.RegistryValueKind]::String)
                    }
                }

                # Add callback to remove all blocked app executions during the shutdown of the final session.
                Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Unblock-ADTAppExecution'
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
