#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationPrompt
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationPrompt
{
    <#
    .SYNOPSIS
        Displays a custom installation prompt with the toolkit branding and optional buttons.

    .DESCRIPTION
        Displays a custom installation prompt with the toolkit branding and optional buttons. Any combination of Left, Middle, or Right buttons can be displayed. The return value of the button clicked by the user is the button text specified. The prompt can also display a system icon and be configured to persist, minimize other windows, or timeout after a specified period.

    .PARAMETER RequestInput
        Show a text box for the user to provide an answer.

    .PARAMETER DefaultValue
        The default value to show in the text box.

    .PARAMETER Message
        The message text to be displayed on the prompt.

    .PARAMETER MessageAlignment
        Alignment of the message text.

    .PARAMETER ButtonLeftText
        Show a button on the left of the prompt with the specified text.

    .PARAMETER ButtonRightText
        Show a button on the right of the prompt with the specified text.

    .PARAMETER ButtonMiddleText
        Show a button in the middle of the prompt with the specified text.

    .PARAMETER Icon
        Show a system icon in the prompt.

    .PARAMETER WindowLocation
        The location of the dialog on the screen.

    .PARAMETER NoWait
        Presents the dialog in a separate, independent thread so that the main process isn't stalled waiting for a response.

    .PARAMETER PersistPrompt
        Specify whether to make the prompt persist in the center of the screen every couple of seconds, specified in the config.psd1 file. The user will have no option but to respond to the prompt.

    .PARAMETER MinimizeWindows
        Specifies whether to minimize other windows when displaying prompt.

    .PARAMETER NoExitOnTimeout
        Specifies whether to not exit the script if the UI times out.

    .PARAMETER NotTopMost
        Specifies whether the prompt shouldn't be topmost, above all other windows.

    .PARAMETER AllowMove
        Specifies that the user can move the dialog on the screen.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not generate any output.

    .EXAMPLE
        Show-ADTInstallationPrompt -Message 'Do you want to proceed with the installation?' -ButtonLeftText 'Yes' -ButtonRightText 'No'

    .EXAMPLE
        Show-ADTInstallationPrompt -Title 'Funny Prompt' -Message 'How are you feeling today?' -ButtonLeftText 'Good' -ButtonRightText 'Bad' -ButtonMiddleText 'Indifferent'

    .EXAMPLE
        Show-ADTInstallationPrompt -Message 'You can customize text to appear at the end of an install, or remove it completely for unattended installations.' -ButtonLeftText 'OK' -Icon Information -NoWait

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTInstallationPrompt
    #>

    [CmdletBinding(DefaultParameterSetName = 'ShowCustomDialog')]
    param
    (
        [Parameter(Mandatory = $true, ParameterSetName = 'ShowInputDialog')]
        [System.Management.Automation.SwitchParameter]$RequestInput,

        [Parameter(Mandatory = $false, ParameterSetName = 'ShowInputDialog')]
        [ValidateNotNullOrEmpty()]
        [System.String]$DefaultValue,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogMessageAlignment]$MessageAlignment = [PSADT.UserInterface.Dialogs.DialogMessageAlignment]::Center,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonRightText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonLeftText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$ButtonMiddleText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogSystemIcon]$Icon,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogPosition]$WindowLocation,

        [Parameter(Mandatory = $false, ParameterSetName = 'ShowCustomDialog')]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PersistPrompt,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$MinimizeWindows,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoExitOnTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$AllowMove
    )

    dynamicparam
    {
        # Initialize variables.
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        $adtConfig = Get-ADTConfig

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Title', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Title', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Title of the prompt.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('Subtitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Subtitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Subtitle of the prompt.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('Timeout', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Timeout', [System.TimeSpan], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; HelpMessage = 'Specifies how long to show the message prompt before aborting.' }
                    [System.Management.Automation.ValidateScriptAttribute]::new({
                            if ($_ -gt [System.TimeSpan]::FromSeconds($adtConfig.UI.DefaultTimeout))
                            {
                                $PSCmdlet.ThrowTerminatingError((New-ADTValidateScriptErrorRecord -ParameterName Timeout -ProvidedValue $_ -ExceptionMessage 'The installation UI dialog timeout cannot be longer than the timeout specified in the config.psd1 file.'))
                            }
                            return !!$_
                        })
                )
            ))

        # Return the populated dictionary.
        return $paramDictionary
    }

    begin
    {
        # Throw if the parent is ServiceUI and we're trying to use an input dialog.
        if ($PSCmdlet.ParameterSetName.Equals('ShowInputDialog') -and (Test-ADTParentProcessHasServiceUI))
        {
            $naerParams = @{
                Exception = [System.InvalidOperationException]::new('The input dialog is only permitted when ServiceUI is not used to start the toolkit.')
                Category = [System.Management.Automation.ErrorCategory]::InvalidOperation
                ErrorId = 'ServiceUiParentProcessFailure'
                RecommendedAction = "Please remove the use of ServiceUI within your environment and try again."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }

        # Throw a terminating error if at least one button isn't specified.
        if (!($PSBoundParameters.Keys -match '^Button'))
        {
            $naerParams = @{
                Exception = [System.ArgumentException]::new('At least one button must be specified when calling this function.')
                Category = [System.Management.Automation.ErrorCategory]::InvalidArgument
                ErrorId = 'MandatoryParameterMissing'
                TargetObject = $PSBoundParameters
                RecommendedAction = "Please review the supplied parameters used against $($MyInvocation.MyCommand.Name) and try again."
            }
            $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
        }

        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Set up DeploymentType.
        $DeploymentType = if ($adtSession)
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
            $PSBoundParameters.Add('Subtitle', (Get-ADTStringTable).InstallationPrompt.Subtitle.($DeploymentType.ToString()))
        }
        if (!$PSBoundParameters.ContainsKey('Timeout'))
        {
            $PSBoundParameters.Add('Timeout', [System.TimeSpan]::FromSeconds($adtConfig.UI.DefaultTimeout))
        }
    }

    process
    {
        try
        {
            try
            {
                # Bypass if in non-interactive mode.
                if ($adtSession -and $adtSession.IsNonInteractive())
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.DeployMode)]. Message: $Message"
                    return
                }

                # Bypass if no one's logged on to answer the dialog.
                if (!($runAsActiveUser = (Get-ADTEnvironmentTable).RunAsActiveUser))
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
                    return
                }

                # Build out hashtable of parameters needed to construct the dialog.
                $dialogOptions = @{
                    AppTitle = $PSBoundParameters.Title
                    Subtitle = $PSBoundParameters.Subtitle
                    AppIconImage = $adtConfig.Assets.Logo
                    AppBannerImage = $adtConfig.Assets.Banner
                    DialogTopMost = !$NotTopMost
                    MinimizeWindows = !!$MinimizeWindows
                    DialogExpiryDuration = $PSBoundParameters.Timeout
                    MessageText = $Message
                }
                if ($PSBoundParameters.ContainsKey('MessageAlignment'))
                {
                    if ($adtConfig.UI.DialogStyle -eq 'Fluent')
                    {
                        Write-ADTLogEntry -Message "The parameter [-MessageAlignment] is not supported with Fluent dialogs and has no effect." -Severity 2
                    }
                    $dialogOptions.MessageAlignment = $MessageAlignment
                }
                if ($PSBoundParameters.ContainsKey('DefaultValue'))
                {
                    $dialogOptions.InitialInputText = $DefaultValue
                }
                if ($ButtonRightText)
                {
                    $dialogOptions.Add('ButtonRightText', $ButtonRightText)
                }
                if ($ButtonLeftText)
                {
                    $dialogOptions.Add('ButtonLeftText', $ButtonLeftText)
                }
                if ($ButtonMiddleText)
                {
                    $dialogOptions.Add('ButtonMiddleText', $ButtonMiddleText)
                }
                if ($Icon)
                {
                    $dialogOptions.Add('Icon', $Icon)
                }
                if ($PSBoundParameters.ContainsKey('WindowLocation'))
                {
                    $dialogOptions.Add('DialogPosition', $WindowLocation)
                }
                if ($PSBoundParameters.ContainsKey('AllowMove'))
                {
                    $dialogOptions.Add('DialogAllowMove', !!$AllowMove)
                }
                if ($PersistPrompt)
                {
                    $dialogOptions.Add('DialogPersistInterval', [System.TimeSpan]::FromSeconds($adtConfig.UI.DefaultPromptPersistInterval))
                }
                if ($null -ne $adtConfig.UI.FluentAccentColor)
                {
                    $dialogOptions.Add('FluentAccentColor', $adtConfig.UI.FluentAccentColor)
                }

                # If the NoWait parameter is specified, launch a new PowerShell session to show the prompt asynchronously.
                if ($NoWait)
                {
                    Write-ADTLogEntry -Message "Displaying custom installation prompt asynchronously to [$($runAsActiveUser.NTAccount)] with message: [$Message]."
                    Show-ADTNoWaitDialog -User $runAsActiveUser -Type $PSCmdlet.ParameterSetName.Replace('Show', $null) -Style $adtConfig.UI.DialogStyle -Options $dialogOptions
                    return
                }

                # Close the Installation Progress dialog if running.
                if ($adtSession)
                {
                    Close-ADTInstallationProgress
                }

                # Instantiate a new ClientServerProcess object if one's not already present.
                if (!$Script:ADT.ClientServerProcess)
                {
                    Open-ADTClientServerProcess -User $runAsActiveUser
                }

                # Minimize all other windows
                if ($MinimizeWindows)
                {
                    Invoke-ADTMinimizeWindowsOperation -MinimizeAllWindows
                }

                # Call the underlying function to open the message prompt.
                Write-ADTLogEntry -Message "Displaying custom installation prompt with message: [$Message]."
                $result = $Script:ADT.ClientServerProcess.($PSCmdlet.ParameterSetName)($adtConfig.UI.DialogStyle, $dialogOptions)

                # Restore minimized windows.
                if ($MinimizeWindows)
                {
                    Invoke-ADTMinimizeWindowsOperation -RestoreAllWindows
                }

                # Process results.
                if ($result -eq 'Timeout')
                {
                    Write-ADTLogEntry -Message 'Installation action not taken within a reasonable amount of time.'
                    if (!$NoExitOnTimeout)
                    {
                        if (Test-ADTSessionActive)
                        {
                            Close-ADTSession -ExitCode $adtConfig.UI.DefaultExitCode
                        }
                    }
                    else
                    {
                        Write-ADTLogEntry -Message 'UI timed out but -NoExitOnTimeout specified. Continue...'
                    }
                }
                return $result
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
