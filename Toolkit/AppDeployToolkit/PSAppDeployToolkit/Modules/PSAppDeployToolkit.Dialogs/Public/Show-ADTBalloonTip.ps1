function Show-ADTBalloonTip
{
    <#

    .SYNOPSIS
    Displays a balloon tip notification in the system tray.

    .DESCRIPTION
    Displays a balloon tip notification in the system tray.

    .PARAMETER BalloonTipText
    Text of the balloon tip.

    .PARAMETER BalloonTipTitle
    Title of the balloon tip.

    .PARAMETER BalloonTipIcon
    Icon to be used. Options: 'Error', 'Info', 'None', 'Warning'. Default is: Info.

    .PARAMETER BalloonTipTime
    Time in milliseconds to display the balloon tip. Default: 10000.

    .PARAMETER NoWait
    Create the balloontip asynchronously. Default: $false

    .INPUTS
    None. You cannot pipe objects to this function.

    .OUTPUTS
    System.String. Returns the version of the specified file.

    .EXAMPLE
    Show-ADTBalloonTip -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name'

    .EXAMPLE
    Show-ADTBalloonTip -BalloonTipIcon 'Info' -BalloonTipText 'Installation Started' -BalloonTipTitle 'Application Name' -BalloonTipTime 1000

    .NOTES
    For Windows 10 OS and above a Toast notification is displayed in place of a balloon tip if toast notifications are enabled in the XML config file.

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
    )

    dynamicparam
    {
        # Initialise the module first if needed.
        if (!($adtSession = if (Test-ADTSessionActive) {Get-ADTSession}) -and !(Test-ADTModuleInitialised))
        {
            try
            {
                Initialize-ADTModule
            }
            catch
            {
                $PSCmdlet.ThrowTerminatingError($_)
            }
        }
        $adtConfig = Get-ADTConfig

        # Build out the necessary parameters.
        try
        {
            Convert-ADTCommandParamsToDynamicParams -Command ($Command = Get-ADTDialogFunction)
        }
        catch
        {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    begin
    {
        Initialize-ADTFunction -Cmdlet $PSCmdlet -SessionState $ExecutionContext.SessionState
    }

    process
    {
        try
        {
            try
            {
                # Skip balloon if in silent mode, disabled in the config or presentation is detected.
                if (($adtSession -and $adtSession.IsSilent()) -or !$adtConfig.UI.BalloonNotifications)
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [$(if ($adtSession) {"Mode:$($adtSession.GetPropertyValue('DeployMode')), "})Config Show Balloon Notifications:$($adtConfig.UI.BalloonNotifications)]. BalloonTipText: $BalloonTipText"
                    return
                }
                if (Test-ADTPowerPoint)
                {
                    Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [$(if ($adtSession) {"Mode:$($adtSession.GetPropertyValue('DeployMode')), "})Presentation Detected:$true]. BalloonTipText: $BalloonTipText"
                    return
                }

                # Call the underlying function to show the balloon tip.
                & $Command @PSBoundParameters
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