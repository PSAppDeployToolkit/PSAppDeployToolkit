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

        The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the configuration).

    .PARAMETER WindowLocation
        The location of the progress window. Default: center of the screen.

    .PARAMETER MessageAlignment
        The text alignment to use for the status message. Default: center.

    .PARAMETER NotTopMost
        Specifies whether the progress window shouldn't be topmost. Default: $false.

    .PARAMETER NoRelocation
        Specifies whether to not reposition the window upon updating the message. Default: $false.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Show-ADTInstallationProgress

        Uses the default status message from the XML configuration file.

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
        [Parameter(Mandatory = $false)]
        [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
        [System.String]$WindowLocation = 'Default',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
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
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'The title of the window to be displayed. The default is the derived value from $InstallTitle.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('WindowSubtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'WindowSubtitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; HelpMessage = 'The subtitle of the window to be displayed with a fluent progress window. The default is null.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('StatusMessage', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'StatusMessage', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'The status message to be displayed. The default status message is taken from the configuration file.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('StatusMessageDetail', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'StatusMessageDetail', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession -and ($adtConfig.UI.DialogStyle -eq 'Fluent'); HelpMessage = 'The status message detail to be displayed with a fluent progress window. The default status message is taken from the configuration file.' }
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
        if ($adtSession)
        {
            if (!$PSBoundParameters.ContainsKey('WindowTitle'))
            {
                $PSBoundParameters.Add('WindowTitle', $adtSession.GetPropertyValue('InstallTitle'))
            }
            if (!$PSBoundParameters.ContainsKey('StatusMessage'))
            {
                $PSBoundParameters.Add('StatusMessage', $adtStrings.Progress."Message$($adtSession.GetPropertyValue('DeploymentType'))")
            }
            if (!$PSBoundParameters.ContainsKey('StatusMessageDetail') -and ($adtConfig.UI.DialogStyle -eq 'Fluent'))
            {
                $PSBoundParameters.Add('StatusMessageDetail', $adtStrings.Progress."Message$($adtSession.GetPropertyValue('DeploymentType'))Detail")
            }
        }
    }

    process
    {
        # Return early in silent mode.
        if ($adtSession)
        {
            if ($adtSession.IsSilent())
            {
                Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.GetPropertyValue('DeployMode'))]. Status message: $($PSBoundParameters.StatusMessage)"
                return
            }

            # Notify user that the software installation has started.
            if (!(Test-ADTInstallationProgressRunning))
            {
                try
                {
                    Show-ADTBalloonTip -BalloonTipIcon Info -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $($adtStrings.BalloonText.Start)"
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
                & $Script:CommandTable."$($MyInvocation.MyCommand.Name)$($adtConfig.UI.DialogStyle)" @PSBoundParameters
                Add-ADTSessionFinishingCallback -Callback $Script:CommandTable.'Close-ADTInstallationProgress'
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
