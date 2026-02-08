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
        The status message to be displayed. The default status message is taken from the imported strings.psd1 file.

    .PARAMETER StatusMessageDetail
        The status message detail to be displayed with a fluent progress window. The default status message is taken from the imported strings.psd1 file.

    .PARAMETER StatusBarPercentage
        The percentage to display on the status bar. If null or not supplied, the status bar will continuously scroll.

    .PARAMETER MessageAlignment
        The text alignment to use for the status message.

    .PARAMETER WindowLocation
        The location of the dialog on the screen.

    .PARAMETER NotTopMost
        Specifies whether the progress window shouldn't be topmost.

    .PARAMETER AllowMove
        Specifies that the user can move the dialog on the screen.

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
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationProgress
    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessage = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessageDetail = [System.Management.Automation.Language.NullString]::Value,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Nullable[System.Double]]$StatusBarPercentage,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSDefaultValue(Help = 'Center')]
        [PSADT.UserInterface.DialogMessageAlignment]$MessageAlignment,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.DialogPosition]$WindowLocation,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$AllowMove
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
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = "The title of the window to be displayed. Optionally used to override the active DeploymentSession's `InstallTitle` value." }
                    [System.Management.Automation.AliasAttribute]::new('WindowTitle')
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('Subtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Subtitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession -and ($adtConfig.UI.DialogStyle -eq 'Fluent'); HelpMessage = "The subtitle of the window to be displayed with a fluent progress window. Optionally used to override the subtitle defined in the `strings.psd1` file." }
                    [System.Management.Automation.AliasAttribute]::new('WindowSubtitle')
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
        $errRecord = $null

        # Initialise the string table.
        $sessionState = if ($adtSession)
        {
            $adtSession.SessionState
        }
        if ($null -eq $sessionState)
        {
            $sessionState = $PSCmdlet.SessionState
        }
        $adtStrings = Get-ADTStringTable -SessionState $SessionState

        # Set up DeploymentType.
        [System.String]$deploymentType = if ($adtSession)
        {
            $adtSession.DeploymentType
        }
        else
        {
            [PSAppDeployToolkit.Foundation.DeploymentType]::Install
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
        # Return early in silent mode.
        if ($adtSession -and $adtSession.IsSilent())
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.DeployMode)]. Status message: $($PSBoundParameters.StatusMessage)"
            return
        }

        # Bypass if no one's logged on to answer the dialog.
        if (!($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return
        }

        # Determine if progress window is open before proceeding.
        $progressOpen = Invoke-ADTClientServerOperation -ProgressDialogOpen -User $runAsActiveUser

        # Notify user that the software installation has started.
        if ($adtSession -and !$progressOpen)
        {
            try
            {
                Show-ADTBalloonTip -BalloonTipIcon Info -BalloonTipText $adtStrings.BalloonTip.Start.$deploymentType -NoWait
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
                # Perform the dialog action.
                $null = if (!$progressOpen)
                {
                    # Create the necessary options.
                    $dialogOptions = @{
                        AppTitle = $PSBoundParameters.Title
                        Subtitle = $PSBoundParameters.Subtitle
                        AppIconImage = $adtConfig.Assets.Logo
                        AppIconDarkImage = $adtConfig.Assets.LogoDark
                        AppBannerImage = $adtConfig.Assets.Banner
                        AppTaskbarIconImage = $adtConfig.Assets.TaskbarIcon
                        DialogTopMost = !$NotTopMost
                        Language = $Script:ADT.Language
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
                    if ($PSBoundParameters.ContainsKey('WindowLocation'))
                    {
                        $dialogOptions.Add('DialogPosition', $WindowLocation)
                    }
                    if ($PSBoundParameters.ContainsKey('AllowMove'))
                    {
                        $dialogOptions.Add('DialogAllowMove', !!$AllowMove)
                    }
                    if ($null -ne $adtConfig.UI.FluentAccentColor)
                    {
                        $dialogOptions.Add('FluentAccentColor', $adtConfig.UI.FluentAccentColor)
                    }
                    [PSADT.UserInterface.DialogOptions.ProgressDialogOptions]$dialogOptions = $dialogOptions

                    # Create the new progress dialog.
                    Write-ADTLogEntry -Message "Creating the progress dialog in a separate thread with $([System.String]::Join(', ', ('StatusMessage', 'StatusMessageDetail', 'StatusBarPercentage').ForEach({ if ($PSBoundParameters.ContainsKey($_)) { "[$($_): $($PSBoundParameters.$_)]" } })))."
                    Invoke-ADTClientServerOperation -ShowProgressDialog -User $runAsActiveUser -DialogStyle $adtConfig.UI.DialogStyle -Options $dialogOptions
                    Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTInstallationProgress'
                }
                else
                {
                    # Update the dialog as required.
                    Write-ADTLogEntry -Message "Updating the progress dialog with $([System.String]::Join(', ', ('StatusMessage', 'StatusMessageDetail', 'StatusBarPercentage').ForEach({ if ($PSBoundParameters.ContainsKey($_)) { "[$($_): $($PSBoundParameters.$_)]" } })))."
                    $iacsoParams = @{
                        UpdateProgressDialog = $true
                        User = $runAsActiveUser
                    }
                    if ($PSBoundParameters.ContainsKey('StatusMessage'))
                    {
                        $iacsoParams.Add('ProgressMessage', $PSBoundParameters.StatusMessage)
                    }
                    if ($PSBoundParameters.ContainsKey('StatusMessageDetail'))
                    {
                        $iacsoParams.Add('ProgressDetailMessage', $PSBoundParameters.StatusMessageDetail)
                    }
                    if ($PSBoundParameters.ContainsKey('StatusBarPercentage'))
                    {
                        $iacsoParams.Add('ProgressPercentage', $StatusBarPercentage)
                    }
                    if ($PSBoundParameters.ContainsKey('MessageAlignment'))
                    {
                        $iacsoParams.Add('MessageAlignment', $MessageAlignment)
                    }
                    Invoke-ADTClientServerOperation @iacsoParams
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
