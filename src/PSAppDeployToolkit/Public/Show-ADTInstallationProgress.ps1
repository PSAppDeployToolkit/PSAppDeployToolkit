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

    .PARAMETER StatusMessage
        The status message to be displayed. The default status message is taken from the config.psd1 file.

    .PARAMETER StatusMessageDetail
        The status message detail to be displayed with a fluent progress window. The default status message is taken from the config.psd1 file.

    .PARAMETER StatusBarPercentage
        The percentage to display on the status bar. If null or not supplied, the status bar will continuously scroll.

    .PARAMETER MessageAlignment
        The text alignment to use for the status message.

    .PARAMETER WindowLocation
        The location of the progress window.

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
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationProgress
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessage,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessageDetail,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Double]$StatusBarPercentage,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'Center')]
        [PSADT.UserInterface.Dialogs.DialogMessageAlignment]$MessageAlignment,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
        [PSDefaultValue(Help = 'Center')]
        [System.String]$WindowLocation,

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
        $paramDictionary.Add('Title', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Title', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'The title of the window to be displayed. The default is the derived value from "$($adtSession.InstallTitle)".' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('Subtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Subtitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession -and ($adtConfig.UI.DialogStyle -eq 'Fluent'); HelpMessage = 'The subtitle of the window to be displayed with a fluent progress window. The default is the derived value from "$($adtSession.DeploymentType)".' }
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

        # Set up DeploymentType.
        [System.String]$deploymentType = if ($adtSession)
        {
            $adtSession.DeploymentType
        }
        else
        {
            [PSADT.Module.DeploymentType]::Install
        }

        # Set up defaults if not specified.
        if (!$PSBoundParameters.ContainsKey('Title'))
        {
            $PSBoundParameters.Add('Title', $adtSession.InstallTitle)
        }
        if (!$PSBoundParameters.ContainsKey('Subtitle'))
        {
            $PSBoundParameters.Add('Subtitle', $adtStrings.ProgressPrompt.Subtitle.$deploymentType)
        }
        if (!$PSBoundParameters.ContainsKey('StatusMessage'))
        {
            $PSBoundParameters.Add('StatusMessage', $adtStrings.ProgressPrompt.Message.$deploymentType)
        }
        if (!$PSBoundParameters.ContainsKey('StatusMessageDetail'))
        {
            $PSBoundParameters.Add('StatusMessageDetail', $adtStrings.ProgressPrompt.MessageDetail.$deploymentType)
        }
    }

    process
    {
        # Determine if progress window is open before proceeding.
        $progressOpen = [PSADT.UserInterface.DialogManager]::ProgressDialogOpen()

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
                    Show-ADTBalloonTip -BalloonTipIcon Info -BalloonTipText $adtStrings.BalloonTip.Start.$deploymentType
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
                    # Create the new progress dialog.
                    $dialogOptions = @{
                        AppTitle = $PSBoundParameters.Title
                        Subtitle = $PSBoundParameters.Subtitle
                        AppIconImage = $adtConfig.Assets.Logo
                        AppBannerImage = $adtConfig.Assets.Banner
                        DialogAllowMove = $true
                        DialogTopMost = !$NotTopMost
                        MinimizeWindows = $false
                        ProgressMessageText = $PSBoundParameters.StatusMessage
                        ProgressDetailMessageText = $PSBoundParameters.StatusMessageDetail
                    }
                    if ($PSBoundParameters.ContainsKey('MessageAlignment'))
                    {
                        if ($adtConfig.UI.DialogStyle -eq 'Fluent')
                        {
                            Write-ADTLogEntry -Message "The parameter [-MessageAlignment] is not supported with Fluent dialogs and has no effect." -Severity 2
                        }
                        $dialogOptions.MessageAlignment = $MessageAlignment
                    }
                    if ($PSBoundParameters.ContainsKey('StatusBarPercentage'))
                    {
                        $dialogOptions.Add('ProgressPercentage', $StatusBarPercentage)
                    }
                    if ($null -ne $adtConfig.UI.FluentAccentColor)
                    {
                        $dialogOptions.Add('FluentAccentColor', $adtConfig.UI.FluentAccentColor)
                    }
                    Write-ADTLogEntry -Message "Creating the progress dialog in a separate thread with message: [$($PSBoundParameters.StatusMessage)]."
                    [PSADT.UserInterface.DialogManager]::ShowProgressDialog($adtConfig.UI.DialogStyle, $dialogOptions)

                    # Add a callback to close it as we've opened for the first time.
                    Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTInstallationProgress'
                }
                else
                {
                    Write-ADTLogEntry -Message "Updating the progress dialog with message: [$($PSBoundParameters.StatusMessage)]."
                    [PSADT.UserInterface.DialogManager]::UpdateProgressDialog($PSBoundParameters.StatusMessage, $PSBoundParameters.StatusMessageDetail)
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
