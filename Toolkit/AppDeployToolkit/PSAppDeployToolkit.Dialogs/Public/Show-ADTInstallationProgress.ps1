#---------------------------------------------------------------------------
#
# 
#
#---------------------------------------------------------------------------

function Show-ADTInstallationProgress
{
    <#

    .SYNOPSIS
    Displays a progress dialog in a separate thread with an updateable custom message.

    .DESCRIPTION
    Create a WPF window in a separate thread to display a marquee style progress ellipse with a custom message that can be updated. The status message supports line breaks.

    The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the configuration).

    .PARAMETER WindowTitle
    The title of the window to be displayed. The default is the derived value from $InstallTitle.

    .PARAMETER WindowSubtitle
    The subtitle of the window to be displayed with a fluent progress window. The default is null.

    .PARAMETER StatusMessage
    The status message to be displayed. The default status message is taken from the configuration file.

    .PARAMETER StatusMessageDetail
    The status message detail to be displayed with a fluent progress window. The default status message is taken from the configuration file.

    .PARAMETER WindowLocation
    The location of the progress window. Default: center of the screen.

    .PARAMETER NotTopMost
    Specifies whether the progress window shouldn't be topmost. Default: $false.

    .PARAMETER Quiet
    Specifies whether to not log the success of updating the progress message. Default: $false.

    .PARAMETER NoRelocation
    Specifies whether to not reposition the window upon updating the message. Default: $false.

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    None. This function does not generate any output.

    .EXAMPLE
    # Use the default status message from the XML configuration file.
    Show-ADTInstallationProgress

    .EXAMPLE
    Show-ADTInstallationProgress -StatusMessage 'Installation in Progress...'

    .EXAMPLE
    Show-ADTInstallationProgress -StatusMessage "Installation in Progress...`nThe installation may take 20 minutes to complete."

    .EXAMPLE
    Show-ADTInstallationProgress -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -TopMost $false

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
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Quiet,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoRelocation
    )

    dynamicparam
    {
        # Initialise the module first if needed.
        $adtSession = Initialize-ADTDialogFunction -Cmdlet $PSCmdlet
        $adtStrings = Get-ADTStrings
        $fluentUi = (Get-ADTConfig).UI.DialogStyle -eq 'Fluent'

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('WindowTitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
            'WindowTitle', [System.String], $(
                [System.Management.Automation.ParameterAttribute]@{Mandatory = !$adtSession}
                [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
            )
        ))
        $paramDictionary.Add('WindowSubtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
            'WindowSubtitle', [System.String], $(
                [System.Management.Automation.ParameterAttribute]@{Mandatory = $false}
                [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
            )
        ))
        $paramDictionary.Add('StatusMessage', [System.Management.Automation.RuntimeDefinedParameter]::new(
            'StatusMessage', [System.String], $(
                [System.Management.Automation.ParameterAttribute]@{Mandatory = !$adtSession}
                [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
            )
        ))
        $paramDictionary.Add('StatusMessageDetail', [System.Management.Automation.RuntimeDefinedParameter]::new(
            'StatusMessageDetail', [System.String], $(
                [System.Management.Automation.ParameterAttribute]@{Mandatory = !$adtSession -and $fluentUi}
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
        if (!$PSBoundParameters.ContainsKey('WindowTitle'))
        {
            $PSBoundParameters.Add('WindowTitle', $adtSession.GetPropertyValue('InstallTitle'))
        }
        if (!$PSBoundParameters.ContainsKey('StatusMessage'))
        {
            $PSBoundParameters.Add('StatusMessage', $adtStrings.Progress."Message$($adtSession.GetPropertyValue('DeploymentType'))")
        }
        if (!$PSBoundParameters.ContainsKey('StatusMessageDetail') -and $fluentUi)
        {
            $PSBoundParameters.Add('StatusMessageDetail', $adtStrings.Progress."Message$($adtSession.GetPropertyValue('DeploymentType'))Detail")
        }
        $WindowTitle = $PSBoundParameters.WindowTitle
        $StatusMessage = $PSBoundParameters.StatusMessage
        $StatusMessageDetail = if ($PSBoundParameters.ContainsKey('StatusMessageDetail')) {$PSBoundParameters.StatusMessageDetail}

        # Remove fluent dialog parameters if specified.
        if (!$fluentUi)
        {
            $null = $PSBoundParameters.Keys.GetEnumerator().Where({$_ -match '^(WindowSubtitle|StatusMessageDetail)$'}).ForEach({
                Write-ADTLogEntry -Message "The parameter [$($_)] is only supported by fluent dialogs and has been removed for you." -Severity 2
                $PSBoundParameters.Remove($_)
            })
        }
    }

    process
    {
        # Return early in silent mode.
        if ($adtSession)
        {
            if ($adtSession.IsSilent())
            {
                Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.GetPropertyValue('DeployMode'))]. Status message: $($PSBoundParameters.StatusMessage)" -DebugMessage:$Quiet
                return
            }

            # Notify user that the software installation has started.
            try
            {
                Show-ADTBalloonTip -BalloonTipIcon Info -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $($adtStrings.BalloonText.Start)"
            }
            catch
            {
                $PSCmdlet.ThrowTerminatingError($_)
            }
        }

        # Call the underlying function to open the progress window.
        try
        {
            try
            {
                # Archive off the curent running state first.
                $start = Test-ADTInstallationProgressRunning
                & (Get-ADTDialogFunction) @PSBoundParameters

                # If we've opened the window for the first time, add a closing callback.
                if (!(Test-ADTInstallationProgressRunning).Equals($start))
                {
                    Add-ADTSessionFinishingCallback -Callback $MyInvocation.MyCommand.Module.ExportedCommands.'Close-ADTInstallationProgress'
                }
            }
            catch
            {
                Write-Error -ErrorRecord $_
            }
        }
        catch
        {
            Invoke-ADTFunctionErrorHandler -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState -ErrorRecord $_
            Close-ADTInstallationProgress
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
