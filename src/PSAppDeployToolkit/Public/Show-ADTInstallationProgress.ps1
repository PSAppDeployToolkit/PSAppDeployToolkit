#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationProgress
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationProgress
{
    <#
    .SYNOPSIS
        Displays a progress dialog in a separate thread with an updateable custom message.

    .DESCRIPTION
        Creates a WPF window in a separate thread to display a marquee style progress ellipse with a custom message that can be updated. The status message supports line breaks.

        The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the config.psd1 file).

    .PARAMETER WindowLocation
        The location of the progress window.

    .PARAMETER MessageAlignment
        The text alignment to use for the status message.

    .PARAMETER NotTopMost
        Specifies whether the progress window shouldn't be topmost.

    .PARAMETER NoRelocation
        Specifies whether to not reposition the window upon updating the message.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Show-ADTInstallationProgress

        Uses the default status message from the strings.psd1 file.

    .EXAMPLE
        Show-ADTInstallationProgress -StatusMessage 'Installation in Progress...'

        Displays a progress dialog with the status message 'Installation in Progress...'.

    .EXAMPLE
        Show-ADTInstallationProgress -StatusMessage "Installation in Progress...`nThe installation may take 20 minutes to complete."

        Displays a progress dialog with a multiline status message.

    .EXAMPLE
        Show-ADTInstallationProgress -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -NotTopMost

        Displays a progress dialog with the status message 'Installation in Progress...', positioned at the bottom right of the screen, and not set as topmost.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
        [PSDefaultValue(Help = 'Center')]
        [System.String]$WindowLocation,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'Center')]
        [System.Windows.TextAlignment]$MessageAlignment,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoRelocation
    )

    dynamicparam
    {
        # Initialize the module first if needed.
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        $adtConfig = Get-ADTConfig

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('WindowTitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'WindowTitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'The title of the window to be displayed. The default is the derived value from "$($adtSession.InstallTitle)".' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('WindowSubtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'WindowSubtitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession -and ($adtConfig.UI.DialogStyle -eq 'Fluent'); HelpMessage = 'The subtitle of the window to be displayed with a fluent progress window. The default is the derived value from "$($adtSession.DeploymentType)".' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('StatusMessage', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'StatusMessage', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'The status message to be displayed. The default status message is taken from the config.psd1 file.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('StatusMessageDetail', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'StatusMessageDetail', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession -and ($adtConfig.UI.DialogStyle -eq 'Fluent'); HelpMessage = 'The status message detail to be displayed with a fluent progress window. The default status message is taken from the config.psd1 file.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
        $adtStrings = Get-ADTStringTable
        $errRecord = $null

        # Set up defaults if not specified.
        if (!$PSBoundParameters.ContainsKey('WindowTitle'))
        {
            $PSBoundParameters.Add('WindowTitle', $adtSession.InstallTitle)
        }
        if (!$PSBoundParameters.ContainsKey('WindowSubtitle'))
        {
            $PSBoundParameters.Add('WindowSubtitle', $adtStrings.Progress.Subtitle.($adtSession.DeploymentType.ToString()))
        }
        if (!$PSBoundParameters.ContainsKey('StatusMessage'))
        {
            $PSBoundParameters.Add('StatusMessage', $adtStrings.Progress.Message.($adtSession.DeploymentType.ToString()))
        }
        if (!$PSBoundParameters.ContainsKey('StatusMessageDetail') -and ($adtConfig.UI.DialogStyle -eq 'Fluent'))
        {
            $PSBoundParameters.Add('StatusMessageDetail', $adtStrings.Progress.MessageDetail.($adtSession.DeploymentType.ToString()))
        }
    }

    process
    {
        # Determine if progress window is open before proceeding.
        $progressOpen = Test-ADTInstallationProgressRunning

        # Return early in silent mode.
        if ($adtSession)
        {
            if ($adtSession.IsSilent())
            {
                Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.DeployMode)]. Status message: $($PSBoundParameters.StatusMessage)"
                return
            }

            # Notify user that the software installation has started.
            if (!$progressOpen)
            {
                try
                {
                    Show-ADTBalloonTip -BalloonTipIcon Info -BalloonTipText $adtStrings.BalloonText.Start.($adtSession.DeploymentType.ToString())
                }
                catch
                {
                    $PSCmdlet.ThrowTerminatingError($_)
                }
            }
        }

        # Call the underlying function to open the progress window.
        try
        {
            try
            {
                # Perform the dialog action.
                if (!$progressOpen)
                {
                    Write-ADTLogEntry -Message "Creating the progress dialog in a separate thread with message: [$($PSBoundParameters.StatusMessage)]."
                }
                else
                {
                    Write-ADTLogEntry -Message "Updating the progress dialog with message: [$($PSBoundParameters.StatusMessage)]."
                }
                & $Script:CommandTable."$($MyInvocation.MyCommand.Name)$($adtConfig.UI.DialogStyle)" @PSBoundParameters

                # Add a callback to close it if we've opened for the first time.
                if (!(Test-ADTInstallationProgressRunning).Equals($progressOpen))
                {
                    Add-ADTSessionFinishingCallback -Callback $Script:CommandTable.'Close-ADTInstallationProgress'
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord ($errRecord = $_)
        }
        finally
        {
            if ($errRecord)
            {
                Close-ADTInstallationProgress
            }
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
