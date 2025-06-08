#-----------------------------------------------------------------------------
#
# MARK: Show-ADTDialogBox
#
#-----------------------------------------------------------------------------

function Show-ADTDialogBox
{
    <#
    .SYNOPSIS
        Display a custom dialog box with optional title, buttons, icon, and timeout.

    .DESCRIPTION
        Display a custom dialog box with optional title, buttons, icon, and timeout. The default button is "OK", the default Icon is "None", and the default Timeout is None.

        Show-ADTInstallationPrompt is recommended over this function as it provides more customization and uses consistent branding with the other UI components.

    .PARAMETER Text
        Text in the message dialog box.

    .PARAMETER Buttons
        The button(s) to display on the dialog box.

    .PARAMETER DefaultButton
        The Default button that is selected. Options: First, Second, Third.

    .PARAMETER Icon
        Icon to display on the dialog box. Options: None, Stop, Question, Exclamation, Information.

    .PARAMETER NoWait
        Presents the dialog in a separate, independent thread so that the main process isn't stalled waiting for a response.

    .PARAMETER ExitOnTimeout
        Specifies whether to not exit the script if the UI times out.

    .PARAMETER NotTopMost
        Specifies whether the message box shouldn't be a system modal message box that appears in a topmost window.

    .PARAMETER Force
        Specifies whether the message box should appear irrespective of an ongoing DeploymentSession's DeployMode.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        PSADT.UserInterface.DialogResults.DialogBoxResult

        Returns the text of the button that was clicked.

    .EXAMPLE
        Show-ADTDialogBox -Title 'Installation Notice' -Text 'Installation will take approximately 30 minutes. Do you wish to proceed?' -Buttons 'OKCancel' -DefaultButton 'Second' -Icon 'Exclamation' -Timeout 600 -Topmost $false

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2025 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTDialogBox
    #>

    [CmdletBinding()]
    [OutputType([PSADT.UserInterface.DialogResults.DialogBoxResult])]
    param
    (
        [Parameter(Mandatory = $true, Position = 0, HelpMessage = 'Enter a message for the dialog box.')]
        [ValidateNotNullOrEmpty()]
        [System.String]$Text,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogBoxButtons]$Buttons = [PSADT.UserInterface.Dialogs.DialogBoxButtons]::Ok,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogBoxDefaultButton]$DefaultButton = [PSADT.UserInterface.Dialogs.DialogBoxDefaultButton]::First,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [PSADT.UserInterface.Dialogs.DialogBoxIcon]$Icon = [PSADT.UserInterface.Dialogs.DialogBoxIcon]::None,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$ExitOnTimeout,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force
    )

    dynamicparam
    {
        # Initialize the module if there's no session and it hasn't been previously initialized.
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet
        $adtConfig = Get-ADTConfig

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Title', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Title', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Title of the message dialog box.' }
                    [System.Management.Automation.ValidateNotNullOrEmptyAttribute]::new()
                )
            ))
        $paramDictionary.Add('Timeout', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Timeout', [System.TimeSpan], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = $false; HelpMessage = 'Specifies how long to show the message prompt before aborting.' }
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
        # Initialize function.
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState

        # Set up defaults if not specified.
        $Title = if (!$PSBoundParameters.ContainsKey('Title'))
        {
            $adtSession.InstallTitle
        }
        else
        {
            $PSBoundParameters.Title
        }
        $Timeout = if (!$PSBoundParameters.ContainsKey('Timeout'))
        {
            [System.TimeSpan]::FromSeconds($adtConfig.UI.DefaultTimeout)
        }
        else
        {
            $PSBoundParameters.Timeout
        }
    }

    process
    {
        # Bypass if in silent mode.
        if ($adtSession -and $adtSession.IsNonInteractive() -and !$Force)
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.deployMode)]. Text: $Text"
            return
        }
        elseif (!($runAsActiveUser = (Get-ADTEnvironmentTable).RunAsActiveUser))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return
        }
        elseif ($Force)
        {
            Write-ADTLogEntry -Message "Forcibly displaying dialog box with message: $Text..."
        }
        else
        {
            Write-ADTLogEntry -Message "Displaying dialog box with message: $Text..."
        }

        try
        {
            try
            {
                # Instantiate dialog options as required.
                $dialogOptions = @{
                    AppTitle = $Title
                    MessageText = $Text
                    DialogButtons = $Buttons
                    DialogDefaultButton = $DefaultButton
                    DialogIcon = $Icon
                    DialogTopMost = !$NotTopMost
                    DialogExpiryDuration = $Timeout
                }

                # If the NoWait parameter is specified, launch a new PowerShell session to show the prompt asynchronously.
                if ($NoWait)
                {
                    Write-ADTLogEntry -Message "Displaying dialog box asynchronously to [$($runAsActiveUser.NTAccount)] with message: [$Text]."
                    Show-ADTNoWaitDialog -User $runAsActiveUser -Type DialogBox -Style $adtConfig.UI.DialogStyle -Options $dialogOptions
                    return
                }

                # Instantiate a new ClientServerProcess object if one's not already present.
                if (!$Script:ADT.ClientServerProcess)
                {
                    Open-ADTClientServerProcess -User $runAsActiveUser
                }

                # Call the underlying function to open the message prompt.
                Write-ADTLogEntry -Message "Displaying dialog box with message: [$Text]."
                $result = $Script:ADT.ClientServerProcess.ShowDialogBox($dialogOptions)

                # Process results.
                if ($result -eq [PSADT.UserInterface.DialogResults.DialogBoxResult]::Timeout)
                {
                    Write-ADTLogEntry -Message 'Dialog box not responded to within the configured amount of time.'
                    if ($ExitOnTimeout)
                    {
                        if (Test-ADTSessionActive)
                        {
                            Close-ADTSession -ExitCode $adtConfig.UI.DefaultExitCode
                        }
                    }
                    else
                    {
                        Write-ADTLogEntry -Message 'Dialog box timed out but -ExitOnTimeout not specified. Continue...'
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
