function Show-ADTInstallationProgressFluent
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
    The subtitle of the window to be displayed. The default is null.

    .PARAMETER StatusMessage
    The status message to be displayed. The default status message is taken from the configuration file.

    .PARAMETER StatusMessageDetail
    The status message detail to be displayed. The default status message is taken from the configuration file.

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
    # Use the default status message from the configuration file.
    Show-ADTInstallationProgressFluent

    .EXAMPLE
    Show-ADTInstallationProgressFluent -StatusMessage 'Installation in Progress...'

    .EXAMPLE
    Show-ADTInstallationProgressFluent -StatusMessage "Installation in Progress...`nThe installation may take 20 minutes to complete."

    .EXAMPLE
    Show-ADTInstallationProgressFluent -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -TopMost $false

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowTitle = (Get-ADTSession).GetPropertyValue('InstallTitle'),

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowSubtitle,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessage = (Get-ADTStrings).Progress."Message$((Get-ADTSession).GetPropertyValue('DeploymentType'))",

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessageDetail = (Get-ADTStrings).Progress."Message$((Get-ADTSession).GetPropertyValue('DeploymentType'))Detail",

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

    # Internal worker functions.
    function Update-ProgressWindowValues
    {
        # Blanketly update values from incoming parameters.
        $Script:ProgressWindow.Window.SetDeploymentTitle($WindowTitle)
        $Script:ProgressWindow.Window.SetDeploymentSubtitle($WindowSubtitle)
        $Script:ProgressWindow.Window.SetProgressMessage($StatusMessage)
        $Script:ProgressWindow.Window.SetProgressMessageDetail($StatusMessageDetail)
    }
    function Update-WindowLocation
    {
        [CmdletBinding()]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Windows.Window]$Window
        )

        # Calculate the position on the screen where the progress dialog should be placed.
        [System.Double]$screenCenterWidth = [System.Windows.SystemParameters]::WorkArea.Width - $Window.ActualWidth
        [System.Double]$screenCenterHeight = [System.Windows.SystemParameters]::WorkArea.Height - $Window.ActualHeight

        # Set the start position of the Window based on the screen size.
        switch ($WindowLocation)
        {
            'TopLeft' {
                $Window.Left = 0.
                $Window.Top = 0.
                break
            }
            'Top' {
                $Window.Left = $screenCenterWidth * 0.5
                $Window.Top = 0.
                break
            }
            'TopRight' {
                $Window.Left = $screenCenterWidth
                $Window.Top = 0.
                break
            }
            'TopCenter' {
                $Window.Left = $screenCenterWidth * 0.5
                $Window.Top = $screenCenterHeight * (1. / 6.)
                break
            }
            'BottomLeft' {
                $Window.Left = 0.
                $Window.Top = $screenCenterHeight
                break
            }
            'Bottom' {
                $Window.Left = $screenCenterWidth * 0.5
                $Window.Top = $screenCenterHeight
                break
            }
            'BottomRight' {
                # The -100 offset is needed to not overlap system tray toast notifications.
                $Window.Left = $screenCenterWidth
                $Window.Top = $screenCenterHeight - 100
                break
            }
            default {
                # Center the progress window by calculating the center of the workable screen based on the width of the screen minus half the width of the progress bar
                $Window.Left = $screenCenterWidth * 0.5
                $Window.Top = $screenCenterHeight * 0.5
                break
            }
        }
    }

    # Return early in silent mode.
    if (($adtSession = Get-ADTSession).IsSilent())
    {
        Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.GetPropertyValue('deployMode'))]. Status message: $StatusMessage" -DebugMessage:$Quiet
        return
    }

    # Write warnings for functionality that is not yet implemented.
    if ($WindowLocation -ne 'Default')
    {
        Write-ADTLogEntry -Message "The parameter '-WindowLocation' is not yet implemented within this function." -Severity 2
    }
    if (!$NotTopMost)
    {
        Write-ADTLogEntry -Message "The TopMost functionality has not yet been implemented within this function." -Severity 2
    }

    # Check if the progress thread is running before invoking methods on it.
    if (!$Script:ProgressWindow.Running)
    {
        # Notify user that the software installation has started.
        Show-ADTBalloonTipFluent -BalloonTipIcon Info -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((Get-ADTStrings).BalloonText.Start)"

        # Instantiate a new progress window object and start it up.
        Write-ADTLogEntry -Message "Creating the progress dialog in a separate thread with message: [$StatusMessage]."
        if (!$Script:ProgressWindow.Window)
        {
            $Script:ProgressWindow.Window = [PSADT.UserInterface.ADTProgressWindow]::new($WindowTitle, $WindowSubtitle, (Get-ADTConfig).Assets.Logo, $StatusMessage, $StatusMessageDetail)
            $Script:ProgressWindow.Thread = $Script:ProgressWindow.Window.Start()

            # Allow the thread to be spun up safely before invoking actions against it.
            do
            {
                $Script:ProgressWindow.Running = $Script:ProgressWindow.Thread -and $Script:ProgressWindow.Thread.ThreadState.Equals([System.Threading.ThreadState]::Running)
            }
            until ($Script:ProgressWindow.Running)
        }
        else
        {
            # Update an existing object and present the dialog.
            Update-ProgressWindowValues
            $Script:ProgressWindow.Window.ShowDialog()
            $Script:ProgressWindow.Running = $true
        }
    }
    else
    {
        # Update all values.
        Update-ProgressWindowValues
        Write-ADTLogEntry -Message "Updated the progress message: [$StatusMessage]." -DebugMessage:$Quiet
    }
}
