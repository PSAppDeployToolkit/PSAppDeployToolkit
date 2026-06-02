#-----------------------------------------------------------------------------
#
# MARK: Show-ADTBalloonTip
#
#-----------------------------------------------------------------------------

function Show-ADTBalloonTip
{
    <#
    .SYNOPSIS
        Displays a balloon tip notification in the system tray.

    .DESCRIPTION
        The `Show-ADTBalloonTip` function displays a balloon tip notification in the system tray. This function can be used to show notifications to the user with customizable text, title, icon, and display duration.

        For Windows 10 and above, balloon tips automatically get translated by the system into toast notifications.

    .PARAMETER Text
        Text of the balloon tip.

    .PARAMETER Icon
        Icon to be used. Valid values for this parameter are: `Error`, `Info`, `None`, `Warning`.

    .PARAMETER Timeout
        This parameter has had no effect since Windows Vista and will be removed in PSAppDeployToolkit 4.3.0.

    .PARAMETER NoWait
        This parameter has had no effect since Windows Vista and will be removed in PSAppDeployToolkit 4.3.0.

    .PARAMETER Force
        Creates the balloon tip irrespective of whether running silently or not.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Show-ADTBalloonTip -Text 'Installation Started' -Title 'Application Name'

        Displays a balloon tip with the text 'Installation Started' and the title 'Application Name'.

    .EXAMPLE
        Show-ADTBalloonTip -Icon 'Info' -Text 'Installation Started' -Title 'Application Name'

        Displays a balloon tip with the info icon, the text 'Installation Started', and the title 'Application Name'

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTBalloonTip
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'Icon', Justification = "This parameter is used via the function's PSBoundParameters dictionary, which is not something PSScriptAnalyzer understands. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpace()]
        [Alias('BalloonTipText')]
        [System.String]$Text,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [Alias('BalloonTipIcon')]
        [System.Windows.Forms.ToolTipIcon]$Icon = [System.Windows.Forms.ToolTipIcon]::Info,

        [Parameter(Mandatory = $false)]
        [System.Obsolete('This parameter will be removed in PSAppDeployToolkit 4.3.0.')]
        [PSAppDeployToolkit.Attributes.ValidateGreaterThanZero()]
        [Alias('BalloonTipTime')]
        [System.UInt32]$Timeout,

        [Parameter(Mandatory = $false)]
        [System.Obsolete('This parameter will be removed in PSAppDeployToolkit 4.3.0.')]
        [System.Management.Automation.SwitchParameter]$NoWait,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Force
    )

    dynamicparam
    {
        # Initialize the module first if needed.
        $adtSession = Initialize-ADTModuleIfUninitialized -Cmdlet $PSCmdlet -PassThruActiveSession

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('Title', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'Title', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Title of the balloon tip.' }
                    [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpaceAttribute]::new()
                    [System.Management.Automation.AliasAttribute]::new('BalloonTipTitle')
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
        $forced = $false

        # Log the deprecation of -WaitSeconds to the log.
        if ($Timeout -gt 0)
        {
            Write-ADTLogEntry -Message "The parameter [BalloonTipTime] has had no effect since Windows Vista and will be removed in PSAppDeployToolkit 4.3.0." -Severity 2
        }
        if ($NoWait)
        {
            Write-ADTLogEntry -Message "The parameter [NoWait] has had no effect since Windows Vista and will be removed in PSAppDeployToolkit 4.3.0." -Severity 2
        }

        # Set up defaults if not specified.
        $Title = if (!$PSBoundParameters.ContainsKey('Title'))
        {
            $adtSession.InstallTitle
        }
        else
        {
            $PSBoundParameters.Title
        }
    }

    process
    {
        # Skip balloon if disabled in the config, we're in the ESP, or there's no logged on user.
        if (!$adtConfig.UI.BalloonNotifications)
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Config Show Balloon Notifications: $($adtConfig.UI.BalloonNotifications)]. Text: $Text"
            return
        }
        if (Test-ADTEspActive -InformationAction SilentlyContinue)
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is an active Enrollment Status Page (ESP) on the system."
            return
        }
        if (!($runAsActiveUser = Get-ADTClientServerUser -AllowSystemFallback))
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) as there is no active user logged onto the system."
            return
        }

        # Determine if a notification icon is open before testing the session.
        # We'll allow updating of a notification icon in silent mode without
        # the -Force parameter if an existing notification icon is already open
        if (!($notifyIconOpen = Test-ADTNotifyIconOpen -RunAsActiveUser $runAsActiveUser) -and $adtSession -and $adtSession.IsSilent())
        {
            if (!$Force)
            {
                Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.DeployMode)]. Text: $Text"
                return
            }
            $forced = $true
        }

        # Call the underlying function to show a balloon tip.
        try
        {
            try
            {
                # Set up/update the notification icon before proceeding.
                Show-ADTNotifyIcon -ToolTipText "$($Title) - $($Text)" -Force:$Force

                # Establish options object and display the balloon tip.
                Write-ADTLogEntry -Message "$(("Displaying", "Forcibly displaying")[$forced]) balloon tip notification with message [$Text]."
                Invoke-ADTClientServerOperation -ShowBalloonTip -User $runAsActiveUser -Options (New-ADTDialogOptionsObject -Type ([PSADT.UserInterface.DialogOptions.BalloonTipOptions]) -Data @{
                        Title = $Title
                        Text = $Text
                        Icon = $Icon
                    })

                # If we're here without a session, close out notification icon.
                if (!$adtSession -and !$notifyIconOpen)
                {
                    Close-ADTNotifyIcon
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
        }
    }

    end
    {
        Complete-ADTFunction -Cmdlet $PSCmdlet
    }
}
