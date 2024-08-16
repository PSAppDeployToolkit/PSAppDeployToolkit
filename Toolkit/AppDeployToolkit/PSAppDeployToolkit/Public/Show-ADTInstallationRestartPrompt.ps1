#---------------------------------------------------------------------------
#
#
#
#---------------------------------------------------------------------------

function Show-ADTInstallationRestartPrompt
{
    <#

    .SYNOPSIS
    Displays a restart prompt with a countdown to a forced restart.

    .DESCRIPTION
    Displays a restart prompt with a countdown to a forced restart.

    .PARAMETER Title
    Title of the prompt. Default: the application installation name.

    .PARAMETER CountdownSeconds
    Specifies the number of seconds to countdown before the system restart. Default: 60

    .PARAMETER CountdownNoHideSeconds
    Specifies the number of seconds to display the restart prompt without allowing the window to be hidden. Default: 30

    .PARAMETER SilentRestart
    Specifies whether the restart should be triggered when Deploy mode is silent or very silent. Default: $false

    .PARAMETER NoCountdown
    Specifies not to show a countdown.

    The UI will restore/reposition itself persistently based on the interval value specified in the config file.

    .PARAMETER SilentCountdownSeconds
    Specifies number of seconds to countdown for the restart when the toolkit is running in silent mode and NoSilentRestart is $false. Default: 5

    .PARAMETER NotTopMost
    Specifies whether the windows is the topmost window. Default: $false.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the version of the specified file.

    .EXAMPLE
    Show-ADTInstallationRestartPrompt -CountdownSeconds 600 -CountdownNoHideSeconds 60

    .EXAMPLE
    Show-ADTInstallationRestartPrompt -NoCountdown

    .EXAMPLE
    Show-ADTInstallationRestartPrompt -Countdownseconds 300 -NoSilentRestart $false -SilentCountdownSeconds 10

    .NOTES
    Be mindful of the countdown you specify for the reboot as code directly after this function might NOT be able to execute - that includes logging.

    .NOTES
    This function can be called without an active ADT session.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$CountdownSeconds = 60,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$CountdownNoHideSeconds = 30,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Int32]$SilentCountdownSeconds = 5,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$SilentRestart,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoCountdown,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost
    )

    dynamicparam
    {
        # Initialise variables.
        $adtSession = Initialize-ADTDialogFunction -Cmdlet $PSCmdlet

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Title', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Title', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Initialise function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Set up defaults if not specified.
        if (!$PSBoundParameters.ContainsKey('Title'))
        {
            $PSBoundParameters.Add('Title', $adtSession.GetPropertyValue('InstallTitle'))
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
                        Write-ADTLogEntry -Message "Triggering restart silently, because the deploy mode is set to [$($adtSession.GetPropertyValue('DeployMode'))] and [NoSilentRestart] is disabled. Timeout is set to [$SilentCountdownSeconds] seconds."
                        & $Script:CommandTable.'Start-Process' -FilePath (Get-ADTPowerShellProcessPath) -ArgumentList "-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command & $Script:CommandTable.'Start-Sleep' -Seconds $SilentCountdownSeconds; & $Script:CommandTable.'Restart-Computer' -Force" -WindowStyle Hidden -ErrorAction Ignore
                    }
                    else
                    {
                        Write-ADTLogEntry -Message "Skipping restart, because the deploy mode is set to [$($adtSession.GetPropertyValue('DeployMode'))] and [SilentRestart] is false."
                    }
                    return
                }

                # Check if we are already displaying a restart prompt.
                $restartPromptTitle = (Get-ADTStringTable).RestartPrompt.Title
                if (& $Script:CommandTable.'Get-Process' | & { process { if ($_.MainWindowTitle -match $restartPromptTitle) { return $_ } } })
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
                    & $Script:CommandTable.'Start-Process' -FilePath (Get-ADTPowerShellProcessPath) -ArgumentList "-ExecutionPolicy Bypass -NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command Import-Module -Name '$Script:PSScriptRoot'; `$null = $($MyInvocation.MyCommand.Name) $($PSBoundParameters | Resolve-ADTBoundParameters -Exclude SilentRestart, SilentCountdownSeconds)" -WindowStyle Hidden -ErrorAction Ignore
                    return
                }

                # Call the underlying function to open the restart prompt.
                Show-ADTInstallationRestartPromptClassic @PSBoundParameters
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
