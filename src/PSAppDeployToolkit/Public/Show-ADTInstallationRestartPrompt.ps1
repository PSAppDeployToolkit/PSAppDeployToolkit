#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationRestartPrompt
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationRestartPrompt
{
    <#
    .SYNOPSIS
        Displays a restart prompt with a countdown to a forced restart.

    .DESCRIPTION
        Displays a restart prompt with a countdown to a forced restart. The prompt can be customized with a title, countdown duration, and whether it should be topmost. It also supports silent mode where the restart can be triggered without user interaction.

    .PARAMETER CountdownSeconds
        Specifies the number of seconds to display the restart prompt.

    .PARAMETER CountdownNoHideSeconds
        Specifies the number of seconds to display the restart prompt without allowing the window to be hidden.

    .PARAMETER SilentCountdownSeconds
        Specifies number of seconds to countdown for the restart when the toolkit is running in silent mode and `-SilentRestart` isn't specified.

    .PARAMETER SilentRestart
        Specifies whether the restart should be triggered when DeployMode is silent or very silent.

    .PARAMETER NoCountdown
        Specifies whether the user should receive a prompt to immediately restart their workstation.

    .PARAMETER CustomMessageText
    Specify the custom message text to display in the dialog (Fluent UI only).

    .PARAMETER NotTopMost
        Specifies whether the prompt shouldn't be topmost, above all other windows.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Show-ADTInstallationRestartPrompt -NoCountdown

        Displays a restart prompt without a countdown.

    .EXAMPLE
        Show-ADTInstallationRestartPrompt -CountdownSeconds 300

        Displays a restart prompt with a 300-second countdown.

    .EXAMPLE
        Show-ADTInstallationRestartPrompt -CountdownSeconds 600 -CountdownNoHideSeconds 60

        Displays a restart prompt with a 600-second countdown and triggers a silent restart with a 60-second countdown in silent mode.

    .NOTES
        Be mindful of the countdown you specify for the reboot as code directly after this function might NOT be able to execute - that includes logging.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationRestartPrompt
    #>

    [CmdletBinding(DefaultParameterSetName = 'Countdown')]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'NoCountdown')]
        [System.Management.Automation.SwitchParameter]$NoCountdown,

        [Parameter(Mandatory = $false, ParameterSetName = 'Countdown')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$CountdownSeconds = 60,

        [Parameter(Mandatory = $false, ParameterSetName = 'Countdown')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$CountdownNoHideSeconds = 30,

        [Parameter(Mandatory = $true, ParameterSetName = 'SilentRestart')]
        [System.Management.Automation.SwitchParameter]$SilentRestart,

        [Parameter(Mandatory = $false, ParameterSetName = 'SilentRestart')]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$SilentCountdownSeconds = 5,

        [Parameter(Mandatory = $false)]
        [System.String]$CustomMessageText,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost
    )

    dynamicparam
    {
        # Initialize variables.
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        $adtStrings = Get-ADTStringTable

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Title', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Title', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Title of the prompt.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                    ($defaultValue = [System.Management.Automation.PSDefaultValueAttribute]::new())
                    $defaultValue.Help = "(Get-ADTSession).InstallTitle"
                )
            ))
        $paramDictionary.Add('Subtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Subtitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Subtitle of the prompt.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                    ($defaultValue = [System.Management.Automation.PSDefaultValueAttribute]::new())
                    $defaultValue.Help = "(Get-ADTStringTable).Prompt.Subtitle.((Get-ADTSession).DeploymentType.ToString())"
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $adtConfig = Get-ADTConfig

        # Set up DeploymentType.
        $PSBoundParameters.DeploymentType = if ($adtSession)
        {
            $adtSession.DeploymentType.ToString()
        }
        else
        {
            'Install'
        }

        # Set up remainder if not specified.
        if (!$PSBoundParameters.ContainsKey('Title'))
        {
            $PSBoundParameters.Add('Title', $adtSession.InstallTitle)
        }
        if (!$PSBoundParameters.ContainsKey('Subtitle'))
        {
            $PSBoundParameters.Add('Subtitle', $adtStrings.RestartPrompt.Subtitle.($PSBoundParameters.DeploymentType))
        }
        if (!$PSBoundParameters.ContainsKey('CountdownSeconds'))
        {
            $PSBoundParameters.Add('CountdownSeconds', $CountdownSeconds)
        }
        if (!$PSBoundParameters.ContainsKey('CountdownNoHideSeconds'))
        {
            $PSBoundParameters.Add('CountdownNoHideSeconds', $CountdownNoHideSeconds)
        }
        if (!$PSBoundParameters.ContainsKey('CustomMessageText'))
        {
            $PSBoundParameters.Add('CustomMessageText', $CustomMessageText)
        }
    }

    process
    {
        try
        {
            try
            {
                # If in non-interactive mode.
                if ($adtSession -and $adtSession.IsSilent())
                {
                    if ($SilentRestart)
                    {
                        Write-ADTLogEntry -Message "Triggering restart silently, because the deploy mode is set to [$($adtSession.DeployMode)] and [-SilentRestart] has been specified. Timeout is set to [$SilentCountdownSeconds] seconds."
                        Start-Process -FilePath (Get-ADTPowerShellProcessPath) -ArgumentList "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command Start-Sleep -Seconds $SilentCountdownSeconds; Restart-Computer -Force" -WindowStyle Hidden -ErrorAction Ignore
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Skipping restart, because the deploy mode is set to [$($adtSession.DeployMode)] and [-SilentRestart] was not specified."
                    }
                    return
                }

                # Check if we are already displaying a restart prompt.
                if (Get-Process | & { process { if ($_.MainWindowTitle -match $adtStrings.RestartPrompt.Title) { return $_ } } } | Select-Object -First 1)
                {
                    Write-ADTLogEntry -Message "$($MyInvocation.MyCommand.Name) was invoked, but an existing restart prompt was detected. Cancelling restart prompt." -Severity 2
                    return
                }

                # If the script has been dot-source invoked by the deploy app script, display the restart prompt asynchronously.
                if ($adtSession)
                {
                    if ($NoCountdown)
                    {
                        Write-ADTLogEntry -Message "Invoking $($MyInvocation.MyCommand.Name) asynchronously with no countdown..."
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Invoking $($MyInvocation.MyCommand.Name) asynchronously with a [$CountdownSeconds] second countdown..."
                    }

                    # Start another powershell instance silently with function parameters from this function.
                    Start-Process -FilePath (Get-ADTPowerShellProcessPath) -ArgumentList "$(if (!(Test-ADTModuleIsReleaseBuild)) { "-ExecutionPolicy Bypass " })-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command & (Import-Module -FullyQualifiedName @{ ModuleName = '$("$($Script:PSScriptRoot)\$($MyInvocation.MyCommand.Module.Name).psd1".Replace("'", "''"))'; Guid = '$($MyInvocation.MyCommand.Module.Guid)'; ModuleVersion = '$($MyInvocation.MyCommand.Module.Version)' } -PassThru) { & `$CommandTable.'Initialize-ADTModule' -ScriptDirectory '$([System.String]::Join("', '", $Script:ADT.Directories.Script.Replace("'", "''")))'; `$null = & `$CommandTable.'$($MyInvocation.MyCommand.Name)$($adtConfig.UI.DialogStyle)' $([PSADT.Utilities.PowerShellUtilities]::ConvertDictToPowerShellArgs($PSBoundParameters, ('SilentRestart', 'SilentCountdownSeconds')).Replace('"', '\"')) }" -WindowStyle Hidden -ErrorAction Ignore
                    return
                }

                # Call the underlying function to open the restart prompt.
                return & $Script:CommandTable."$($MyInvocation.MyCommand.Name)$($adtConfig.UI.DialogStyle)" @PSBoundParameters
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
