#-----------------------------------------------------------------------------
#
# MARK: Show-ADTInstallationProgressClassic
#
#-----------------------------------------------------------------------------

function Show-ADTInstallationProgressClassic
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'DisableWindowCloseButton', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UpdateWindowLocation', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'UnboundArguments', Justification = "This parameter is just to trap any superfluous input at the end of the function's call.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSReviewUnusedParameter', 'WindowLocation', Justification = "This parameter is used within delegates that PSScriptAnalyzer has no visibility of. See https://github.com/PowerShell/PSScriptAnalyzer/issues/1472 for more details.")]
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
        [System.Management.Automation.SwitchParameter]$NotTopMost,

        [Parameter(Mandatory = $false)]
        [System.Management.Automation.SwitchParameter]$Silent,

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
        $adtConfig = & $Script:CommandTable.'Get-ADTConfig'
        $xaml = [System.Xml.XmlDocument]::new()
        $xaml.Load($Script:Dialogs.Classic.ProgressWindow.XamlCode)
        $xaml.Window.Title = $xaml.Window.ToolTip = $WindowTitle
        $xaml.Window.TopMost = (!$NotTopMost).ToString()
        $xaml.Window.Grid.TextBlock.Text = $StatusMessage

        # Set up the PowerShell instance and add the initial scriptblock.
        $Script:Dialogs.Classic.ProgressWindow.PowerShell = [System.Management.Automation.PowerShell]::Create().AddScript({
                [CmdletBinding()]
                param
                (
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullOrEmpty()]
                    [System.Xml.XmlDocument]$Xaml,

                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullOrEmpty()]
                    [System.IO.FileInfo]$Icon,

                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullOrEmpty()]
                    [System.IO.FileInfo]$Banner,

                    [Parameter(Mandatory = $true)]
                    [ValidateSet('Default', 'TopLeft', 'Top', 'TopRight', 'TopCenter', 'BottomLeft', 'Bottom', 'BottomRight')]
                    [System.String]$WindowLocation,

                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullOrEmpty()]
                    [System.Management.Automation.ScriptBlock]$UpdateWindowLocation,

                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullOrEmpty()]
                    [System.Management.Automation.ScriptBlock]$DisableWindowCloseButton
                )

                # Set required variables to ensure script functionality.
                $ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
                $ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
                Set-StrictMode -Version 3

                # Create XAML window and bring it up.
                try
                {
                    $SyncHash.Add('Window', [System.Windows.Markup.XamlReader]::Load([System.Xml.XmlNodeReader]::new($Xaml)))
                    $SyncHash.Add('Message', $SyncHash.Window.FindName('ProgressText'))
                    $SyncHash.Window.Icon = [System.Windows.Media.Imaging.BitmapFrame]::Create([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Icon)), [System.Windows.Media.Imaging.BitmapCreateOptions]::IgnoreImageCache, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
                    $SyncHash.Window.FindName('ProgressBanner').Source = [System.Windows.Media.Imaging.BitmapFrame]::Create([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Banner)), [System.Windows.Media.Imaging.BitmapCreateOptions]::IgnoreImageCache, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
                    $SyncHash.Window.add_MouseLeftButtonDown({ $this.DragMove() })
                    $SyncHash.Window.add_Loaded({
                            # Relocate the window and disable the X button.
                            & $UpdateWindowLocation -Window $this -Location $WindowLocation
                            & $DisableWindowCloseButton -WindowHandle ([System.Windows.Interop.WindowInteropHelper]::new($this).Handle)
                        })
                    $null = $SyncHash.Window.ShowDialog()
                }
                catch
                {
                    $SyncHash.Error = $_
                    $PSCmdlet.ThrowTerminatingError($_)
                }
            }).AddArgument($Xaml).AddArgument($adtConfig.Assets.Logo).AddArgument($adtConfig.Assets.Banner).AddArgument($WindowLocation).AddArgument(${Function:Update-WindowLocation}.Ast.Body.GetScriptBlock()).AddArgument($Script:CommandTable.'Disable-ADTWindowCloseButton'.ScriptBlock.Ast.Body.GetScriptBlock())

        # Commence invocation.
        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Creating the progress dialog in a separate thread with message: [$StatusMessage]."
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.ApartmentState = [System.Threading.ApartmentState]::STA
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.ThreadOptions = [System.Management.Automation.Runspaces.PSThreadOptions]::ReuseThread
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.Open()
        $Script:Dialogs.Classic.ProgressWindow.PowerShell.Runspace.SessionStateProxy.SetVariable('SyncHash', $Script:Dialogs.Classic.ProgressWindow.SyncHash)
        $Script:Dialogs.Classic.ProgressWindow.Invocation = $Script:Dialogs.Classic.ProgressWindow.PowerShell.BeginInvoke()

        # Allow the thread to be spun up safely before invoking actions against it.
        while (!($Script:Dialogs.Classic.ProgressWindow.SyncHash.ContainsKey('Window') -and $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window.IsInitialized -and $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window.Dispatcher.Thread.ThreadState.Equals([System.Threading.ThreadState]::Running)))
        {
            if ($Script:Dialogs.Classic.ProgressWindow.SyncHash.ContainsKey('Error'))
            {
                $PSCmdlet.ThrowTerminatingError($Script:Dialogs.Classic.ProgressWindow.SyncHash.Error)
            }
            elseif ($Script:Dialogs.Classic.ProgressWindow.Invocation.IsCompleted)
            {
                $naerParams = @{
                    Exception = [System.InvalidOperationException]::new("The separate thread completed without presenting the progress dialog.")
                    Category = [System.Management.Automation.ErrorCategory]::InvalidResult
                    ErrorId = 'InstallationProgressDialogFailure'
                    TargetObject = $(if ($SyncHash.ContainsKey('Window')) { $SyncHash.Window })
                    RecommendedAction = "Please review the result in this error's TargetObject property and try again."
                }
                $PSCmdlet.ThrowTerminatingError((& $Script:CommandTable.'New-ADTErrorRecord' @naerParams))
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
                if (!$NoRelocation)
                {
                    Update-WindowLocation -Window $Script:Dialogs.Classic.ProgressWindow.SyncHash.Window -Location $WindowLocation
                }
            },
            [System.Windows.Threading.DispatcherPriority]::Send
        )
        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Updated the progress message: [$StatusMessage]." -DebugMessage:$Silent
    }
}
