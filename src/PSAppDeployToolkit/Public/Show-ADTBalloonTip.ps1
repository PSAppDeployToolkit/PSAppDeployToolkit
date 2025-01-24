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
        Displays a balloon tip notification in the system tray. This function can be used to show notifications to the user with customizable text, title, icon, and display duration.

        For Windows 10 and above, balloon tips automatically get translated by the system into toast notifications.

    .PARAMETER BalloonTipText
        Text of the balloon tip.

    .PARAMETER BalloonTipIcon
        Icon to be used. Options: 'Error', 'Info', 'None', 'Warning'. Default is: Info.

    .PARAMETER BalloonTipTime
        This parameter is obsolete and will be removed in PSAppDeployToolkit 4.2.0.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Show-ADTBalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'

        Displays a balloon tip with the text 'Installation Started' and the title 'Application Name'.

    .EXAMPLE
        Show-ADTBalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'

        Displays a balloon tip with the info icon, the text 'Installation Started', and the title 'Application Name'

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt
        Website: https://psappdeploytoolkit.com
        Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com
    #>

    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'BalloonTipIcon', Justification = "This parameter is used via the function's PSBoundParameters dictionary, which is not something PSScriptAnalyzer understands. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true, Position = 0)]
        [ValidateNotNullOrEmpty()]
        [System.String]$BalloonTipText,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Windows.Forms.ToolTipIcon]$BalloonTipIcon = 'Info',

        [Parameter(Mandatory = $false)]
        [System.Obsolete("This parameter will be removed in PSAppDeployToolkit 4.2.0.")]
        [ValidateNotNullOrEmpty()]
        [System.UInt32]$BalloonTipTime = 10000
    )

    dynamicparam
    {
        # Initialize the module first if needed.
        $adtSession = Initialize-ADTModuleIfUnitialized -Cmdlet $PSCmdlet

        # Define parameter dictionary for returning at the end.
        $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()

        # Add in parameters we need as mandatory when there's no active ADTSession.
        $paramDictionary.Add('BalloonTipTitle', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'BalloonTipTitle', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Title of the balloon tip.' }
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
        $adtConfig = Get-ADTConfig

        # Initalise the classic assets.
        Initialize-ADTClassicAssets

        # Set up defaults if not specified.
        if (!$PSBoundParameters.ContainsKey('BalloonTipTitle'))
        {
            $PSBoundParameters.Add('BalloonTipTitle', $adtSession.InstallTitle)
        }

        # Log the deprecation of -WaitSeconds to the log.
        if ($PSBoundParameters.ContainsKey('BalloonTipTime'))
        {
            Write-ADTLogEntry -Message "The parameter [BalloonTipTime] is obsolete and will be removed in PSAppDeployToolkit 4.2.0." -Severity 2
            $null = $PSBoundParameters.Remove('BalloonTipTime')
        }
    }

    process
    {
        # Don't allow toast notifications with fluent dialogs unless this function was explicitly requested by the caller.
        if (($adtConfig.UI.DialogStyle -eq 'Fluent') -and ((Get-PSCallStack)[1].Command -match '^(Show|Close)-ADTInstallationProgress$'))
        {
            return
        }

        try
        {
            try
            {
                # Skip balloon if in silent mode, disabled in the config or presentation is detected.
                if (!$adtConfig.UI.BalloonNotifications)
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Config Show Balloon Notifications: $($adtConfig.UI.BalloonNotifications)]. BalloonTipText: $BalloonTipText"
                    return
                }
                if ($adtSession -and $adtSession.IsSilent())
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.DeployMode)]. BalloonTipText: $BalloonTipText"
                    return
                }
                if (Test-ADTUserIsBusy)
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Presentation/Microphone in Use Detected: $true]. BalloonTipText: $BalloonTipText"
                    return
                }

                # Set up the balloon tip.
                Write-ADTLogEntry -Message "Displaying balloon tip notification with message [$BalloonTipText]."
                $nabtParams = Get-ADTBoundParametersAndDefaultValues -Invocation $MyInvocation
                $nabtParams.Add('Icon', $Script:Dialogs.Classic.Assets.Icon); $nabtParams.Add('Visible', $true)
                $notifyIcon = [System.Windows.Forms.NotifyIcon]$nabtParams

                # Add an event to manage disposal of the object before displaying.
                $null = Register-ObjectEvent -InputObject $notifyIcon -EventName BalloonTipShown -Action { $Sender.Dispose() }
                $notifyIcon.ShowBalloonTip($null)
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
