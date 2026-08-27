#-----------------------------------------------------------------------------
#
# MARK: Show-ADTNotifyIcon
#
#-----------------------------------------------------------------------------

function Show-ADTNotifyIcon
{
    <#
    .SYNOPSIS
        Displays a notification icon in the system tray.

    .DESCRIPTION
        The `Show-ADTNotifyIcon` function displays a notification icon in the system tray.

        For Windows 10 and above, balloon tips from notification icons automatically get translated by the system into toast notifications.

    .PARAMETER Force
        Creates the notification icon irrespective of whether running silently or not.

    .INPUTS
        None

        You cannot pipe objects to this function.

    .OUTPUTS
        None

        This function does not return any output.

    .EXAMPLE
        Show-ADTNotifyIcon

        Creates a notification icon with the configured icon and session title.

    .NOTES
        An active ADT session is NOT required to use this function.

        Tags: psadt<br />
        Website: https://psappdeploytoolkit.com<br />
        Copyright: (C) 2026 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).<br />
        License: https://opensource.org/license/lgpl-3-0

    .LINK
        https://psappdeploytoolkit.com/docs/reference/functions/Show-ADTNotifyIcon

    .LINK
        https://github.com/PSAppDeployToolkit/PSAppDeployToolkit/blob/main/src/PSAppDeployToolkit/Public/Show-ADTNotifyIcon.ps1
    #>

    [CmdletBinding()]
    param
    (
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
        $paramDictionary.Add('ToolTipText', [System.Management.Automation.RuntimeDefinedParameter]::new(
                'ToolTipText', [System.String], $(
                    [System.Management.Automation.ParameterAttribute]@{ Mandatory = !$adtSession; HelpMessage = 'Tool tip text for the notification icon.' }
                    [PSAppDeployToolkit.Attributes.ValidateNotNullOrWhiteSpaceAttribute]::new()
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

        # Set up defaults if not specified.
        $ToolTipText = if (!$PSBoundParameters.ContainsKey('ToolTipText'))
        {
            $adtSession.InstallTitle
        }
        else
        {
            $PSBoundParameters.ToolTipText
        }
        if ($ToolTipText.Length -gt 63)
        {
            $ToolTipText = "$($ToolTipText.Substring(0, 60))..."
        }
    }

    process
    {
        # Skip the notification icon if we're in the ESP or there's no logged on user.
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
                Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.DeployMode)]."
                return
            }
            $forced = $true
        }

        # Call the underlying function to open/update the notification icon.
        try
        {
            try
            {
                if (!$notifyIconOpen)
                {
                    # Establish options object and open the notification icon.
                    Write-ADTLogEntry -Message "$(("Displaying", "Forcibly displaying")[$forced]) notification icon with text [$ToolTipText]."
                    Invoke-ADTClientServerOperation -ShowNotifyIcon -User $runAsActiveUser -Options (New-ADTDialogOptionsObject -Type ([PSADT.UserInterface.DialogOptions.NotifyIconOptions]) -Data @{
                            AppTitle = $adtConfig.Toolkit.CompanyName
                            AppIconImage = $adtConfig.Assets.Logo
                            AppTaskbarIconImage = $adtConfig.Assets.TaskbarIcon
                            MessageText = $ToolTipText
                        })
                    Add-ADTModuleCallback -Hookpoint OnFinish -Callback $Script:CommandTable.'Close-ADTNotifyIcon'
                }
                else
                {
                    Write-ADTLogEntry -Message "Updating notification icon with text [$ToolTipText]."
                    Invoke-ADTClientServerOperation -UpdateNotifyIcon -User $runAsActiveUser -MessageText $ToolTipText
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
