#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationProgressFluent
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationProgressFluent
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowSubtitle,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessage,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessageDetail,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
        [System.String]$WindowLocation = 'Default',

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoRelocation,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Internal worker functions.
    function Update-ProgressWindowValues
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is an internal worker function that requires no end user confirmation.')]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
        [CmdletBinding(SupportsShouldProcess = $false)]
        param
        (
        )

        # Blanketly update values from incoming parameters.
        $Script:Dialogs.Fluent.ProgressWindow.Window.SetDeploymentTitle($WindowTitle)
        $Script:Dialogs.Fluent.ProgressWindow.Window.SetProgressMessage($StatusMessage)
        $Script:Dialogs.Fluent.ProgressWindow.Window.SetProgressMessageDetail($StatusMessageDetail)

        # Only update the window subtitle if it's been specified.
        if ($WindowSubtitle)
        {
            $Script:Dialogs.Fluent.ProgressWindow.Window.SetDeploymentSubtitle($WindowSubtitle)
        }
    }
    function Update-WindowLocation
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is an internal worker function that requires no end user confirmation.')]
        [CmdletBinding(SupportsShouldProcess = $false)]
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
            'TopLeft'
            {
                $Window.Left = 0.
                $Window.Top = 0.
                break
            }
            'Top'
            {
                $Window.Left = $screenCenterWidth * 0.5
                $Window.Top = 0.
                break
            }
            'TopRight'
            {
                $Window.Left = $screenCenterWidth
                $Window.Top = 0.
                break
            }
            'TopCenter'
            {
                $Window.Left = $screenCenterWidth * 0.5
                $Window.Top = $screenCenterHeight * (1. / 6.)
                break
            }
            'BottomLeft'
            {
                $Window.Left = 0.
                $Window.Top = $screenCenterHeight
                break
            }
            'Bottom'
            {
                $Window.Left = $screenCenterWidth * 0.5
                $Window.Top = $screenCenterHeight
                break
            }
            'BottomRight'
            {
                # The -100 offset is needed to not overlap system tray toast notifications.
                $Window.Left = $screenCenterWidth
                $Window.Top = $screenCenterHeight - 100
                break
            }
            default
            {
                # Center the progress window by calculating the center of the workable screen based on the width of the screen minus half the width of the progress bar
                $Window.Left = $screenCenterWidth * 0.5
                $Window.Top = $screenCenterHeight * 0.5
                break
            }
        }
    }

    # Write warnings for functionality that is not yet implemented.
    if (($WindowLocation -ne 'Default') -or $NoRelocation)
    {
        Write-ADTLogEntry -Message "The parameter '-WindowLocation' is not yet implemented within this function." -Severity 2
    }
    if (!$NotTopMost)
    {
        Write-ADTLogEntry -Message "The TopMost functionality has not yet been implemented within this function." -Severity 2
    }

    # Check if the progress thread is running before invoking methods on it.
    if (!$Script:Dialogs.Fluent.ProgressWindow.Running)
    {
        # Instantiate a new progress window object and start it up.
        Write-ADTLogEntry -Message "Creating the progress dialog in a separate thread with message: [$StatusMessage]."
        if (!$Script:Dialogs.Fluent.ProgressWindow.Window)
        {
            $Script:Dialogs.Fluent.ProgressWindow.Window = [PSADT.UserInterface.ADTProgressWindow]::new($WindowTitle, $WindowSubtitle, (Get-ADTConfig).Assets.Logo, $StatusMessage, $StatusMessageDetail)
            $Script:Dialogs.Fluent.ProgressWindow.Thread = $Script:Dialogs.Fluent.ProgressWindow.Window.Start()

            # Allow the thread to be spun up safely before invoking actions against it.
            do
            {
                $Script:Dialogs.Fluent.ProgressWindow.Running = $Script:Dialogs.Fluent.ProgressWindow.Thread -and $Script:Dialogs.Fluent.ProgressWindow.Thread.ThreadState.Equals([System.Threading.ThreadState]::Running)
            }
            until ($Script:Dialogs.Fluent.ProgressWindow.Running)
        }
        else
        {
            # Update an existing object and present the dialog.
            Update-ProgressWindowValues
            $Script:Dialogs.Fluent.ProgressWindow.Window.ShowDialog()
            $Script:Dialogs.Fluent.ProgressWindow.Running = $true
        }
    }
    else
    {
        # Update all values.
        Update-ProgressWindowValues
        Write-ADTLogEntry -Message "Updated the progress message: [$StatusMessage]."
    }
}
