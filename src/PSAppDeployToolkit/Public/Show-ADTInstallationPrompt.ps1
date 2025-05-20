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

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$Message,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Left', 'Center', 'Right')]
        [System.String]$MessageAlignment = 'Center',

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
        [ValidateSet('Application', 'Asterisk', 'Error', 'Exclamation', 'Hand', 'Information', 'Question', 'Shield', 'Warning', 'WinLogo')]
        [System.String]$Icon,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$PersistPrompt,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$MinimizeWindows,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoExitOnTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost
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
        $paramDictionary.Add('Timeout', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Timeout', [System.UInt32], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; HelpMessage = 'Specifies how long, in seconds, to show the message prompt before aborting.' }
                    [System.Management.Automation.ValidateScriptAttribute]::new({
                            if ($_ -gt $adtConfig.UI.DefaultTimeout)
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
            $adtSession.DeploymentType.ToString()
        }
        else
        {
            'Install'
        }

        # Set up defaults if not specified.
        if (!$PSBoundParameters.ContainsKey('Title'))
        {
            $PSBoundParameters.Add('Title', $adtSession.InstallTitle)
        }
        if (!$PSBoundParameters.ContainsKey('Subtitle'))
        {
            $PSBoundParameters.Add('Subtitle', (Get-ADTStringTable).Prompt.Subtitle.$DeploymentType)
        }
        if (!$PSBoundParameters.ContainsKey('Timeout'))
        {
            $PSBoundParameters.Add('Timeout', $adtConfig.UI.DefaultTimeout)
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

                # Resolve the bound parameters to a string.
                $paramsString = [PSADT.Utilities.PowerShellUtilities]::ConvertDictToPowerShellArgs($PSBoundParameters)

                # If the NoWait parameter is specified, launch a new PowerShell session to show the prompt asynchronously.
                if ($NoWait)
                {
                    Write-ADTLogEntry -Message "Displaying custom installation prompt asynchronously with the parameters: [$($paramsString.Replace("''", "'"))]."
                    Start-Process -FilePath (Get-ADTPowerShellProcessPath) -ArgumentList "$(if (!(Test-ADTModuleIsReleaseBuild)) { "-ExecutionPolicy Bypass " })-NonInteractive -NoProfile -NoLogo -WindowStyle Hidden -Command & (Import-Module -FullyQualifiedName @{ ModuleName = '$("$($Script:PSScriptRoot)\$($MyInvocation.MyCommand.Module.Name).psd1".Replace("'", "''"))'; Guid = '$($MyInvocation.MyCommand.Module.Guid)'; ModuleVersion = '$($MyInvocation.MyCommand.Module.Version)' } -PassThru) { & `$CommandTable.'Initialize-ADTModule' -ScriptDirectory '$([System.String]::Join("', '", $Script:ADT.Directories.Script.Replace("'", "''")))'; `$null = & `$CommandTable.'$($MyInvocation.MyCommand.Name)$($adtConfig.UI.DialogStyle)' $($paramsString.Replace('"', '\"')) }" -WindowStyle Hidden -ErrorAction Ignore
                    return
                }

                # Close the Installation Progress dialog if running.
                if ($adtSession)
                {
                    Close-ADTInstallationProgress
                }

                # Call the underlying function to open the message prompt.
                Write-ADTLogEntry -Message "Displaying custom installation prompt with the parameters: [$($paramsString.Replace("''", "'"))]."
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
