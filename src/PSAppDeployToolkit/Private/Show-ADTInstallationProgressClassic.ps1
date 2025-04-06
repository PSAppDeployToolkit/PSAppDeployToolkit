#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationProgressClassic
#
#-----------------------------------------------------------------------------

function Private:Show-ADTInstallationProgressClassic
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'NoRelocation', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowTitle,

        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessage,

        [Parameter(Mandatory = $false)]
        [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
        [System.String]$WindowLocation = 'Default',

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.Windows.TextAlignment]$MessageAlignment = [System.Windows.TextAlignment]::Center,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$NoRelocation,

        [Parameter(Mandatory = $false, ValueFromRemainingArguments = $true, DontShow = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Generic.List[System.Object]]$UnboundArguments
    )

    # Internal worker function.
    function Update-WindowLocation
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'This is an internal worker function that requires no end user confirmation.')]
        [CmdletBinding(SupportsShouldProcess = $false)]
        param
        (
            [Parameter(Mandatory = $true)]
            [ValidateNotNullOrEmpty()]
            [System.Windows.Window]$Window,

            [Parameter(Mandatory = $false)]
            [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
            [System.String]$Location = 'Default'
        )

        # Calculate the position on the screen where the progress dialog should be placed.
        [System.Double]$screenCenterWidth = [System.Windows.SystemParameters]::WorkArea.Width - $Window.ActualWidth
        [System.Double]$screenCenterHeight = [System.Windows.SystemParameters]::WorkArea.Height - $Window.ActualHeight

        # Set the start position of the Window based on the screen size.
        switch ($Location)
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

    # Check if the progress thread is running before invoking methods on it.
    if (!$Script:Dialogs.Classic.ProgressWindow.Running)
    {
        # Load up the XML file.
        $adtConfig = Get-ADTConfig
        $xaml = [System.Xml.XmlDocument]::new()
        $xaml.Load($Script:Dialogs.Classic.ProgressWindow.XamlCode)
        $xaml.Window.Title = $xaml.Window.ToolTip = $WindowTitle
        $xaml.Window.TopMost = (!$NotTopMost).ToString()
        $xaml.Window.Grid.TextBlock.Text = $StatusMessage
        $xaml.Window.Grid.TextBlock.TextAlignment = $MessageAlignment.ToString()

        # Set up the PowerShell instance and commence invocation.
        $Script:Dialogs.Classic.ProgressWindow.PowerShell = [System.Management.Automation.PowerShell]::Create().AddScript($Script:CommandTable.'Show-ADTInstallationProgressClassicInternal'.ScriptBlock).AddArgument($Xaml).AddArgument($adtConfig.Assets.Logo).AddArgument($adtConfig.Assets.Banner).AddArgument($WindowLocation).AddArgument(${Function:Update-WindowLocation}.Ast.Body.GetScriptBlock()).AddArgument($Script:CommandTable.'Disable-ADTWindowCloseButton'.ScriptBlock.Ast.Body.GetScriptBlock())
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.ApartmentState = [System.Threading.ApartmentState]::STA
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.ThreadOptions = [System.Management.Automation.Runspaces.PSThreadOptions]::ReuseThread
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.Open()
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.SessionStateProxy.SetVariable('SyncHash', $Script:Dialogs.Classic.ProgressWindow.SyncHash)
        $Script:Dialogs.Classic.ProgressWindow.Invocation = $Script:Dialogs.Classic.ProgressWindow.PowerShell.BeginInvoke()

        # Allow the thread to be spun up safely before invoking actions against it.
        while (!($Script:Dialogs.Classic.ProgressWindow.SyncHash.ContainsKey('Window') -and $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window.IsInitialized -and $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window.Dispatcher.Thread.ThreadState.Equals([System.Threading.ThreadState]::Running)))
        {
            if ($Script:Dialogs.Classic.ProgressWindow.Invocation.IsCompleted)
            {
                if (!$Script:Dialogs.Classic.ProgressWindow.PowerShell.HadErrors)
                {
                    $naerParams = @{
                        Exception = [System.InvalidOperationException]::new("The separate thread completed without presenting the progress dialog.")
                        Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                        ErrorId = 'InstallationProgressDialogFailure'
                        TargetObject = $(if ($Script:Dialogs.Classic.ProgressWindow.SyncHash.ContainsKey('Window')) { $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window })
                        RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                    }
                    $PSCmdlet.ThrowTerminatingError((New-ADTErrorRecord @naerParams))
                }
                $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.SessionStateProxy.PSVariable.GetValue('Error') | & { process { if ($_ -is [System.Management.Automation.ErrorRecord]) { $PSCmdlet.ThrowTerminatingError($_) } } }
            }
        }

        # If we're here, the window came up.
        $Script:Dialogs.Classic.ProgressWindow.Running = $true
    }
    else
    {
        # Invoke update events against an established window.
        $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window.Dispatcher.Invoke(
            {
                $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window.Title = $WindowTitle
                $Script:Dialogs.Classic.ProgressWindow.SyncHash.Message.Text = $StatusMessage
                $Script:Dialogs.Classic.ProgressWindow.SyncHash.Message.TextAlignment = $MessageAlignment
                if (!$NoRelocation)
                {
                    Update-WindowLocation -Window $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window -Location $WindowLocation
                }
            },
            [System.Windows.Threading.DispatcherPriority]::Send
        )
    }
}
