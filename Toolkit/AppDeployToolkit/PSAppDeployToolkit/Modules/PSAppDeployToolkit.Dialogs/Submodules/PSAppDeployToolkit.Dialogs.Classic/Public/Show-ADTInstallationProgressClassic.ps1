function Show-ADTInstallationProgressClassic
{
    <#

    .SYNOPSIS
    Displays a progress dialog in a separate thread with an updateable custom message.

    .DESCRIPTION
    Create a WPF window in a separate thread to display a marquee style progress ellipse with a custom message that can be updated. The status message supports line breaks.

    The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the configuration).

    .PARAMETER WindowTitle
    The title of the window to be displayed. The default is the derived value from $InstallTitle.

    .PARAMETER StatusMessage
    The status message to be displayed. The default status message is taken from the configuration file.

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
    Show-ADTInstallationProgressClassic

    .EXAMPLE
    Show-ADTInstallationProgressClassic -StatusMessage 'Installation in Progress...'

    .EXAMPLE
    Show-ADTInstallationProgressClassic -StatusMessage "Installation in Progress...`nThe installation may take 20 minutes to complete."

    .EXAMPLE
    Show-ADTInstallationProgressClassic -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -TopMost $false

    .LINK
    https://psappdeploytoolkit.com

    #>

    [CmdletBinding()]
    param (
        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$WindowTitle = (Get-ADTSession).GetPropertyValue('InstallTitle'),

        [Parameter(Mandatory = $false)]
        [ValidateNotNullOrEmpty()]
        [System.String]$StatusMessage = (Get-ADTStrings).Progress."Message$((Get-ADTSession).GetPropertyValue('DeploymentType'))",

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

    begin {
        function Update-WindowLocation
        {
            [CmdletBinding()]
            param (
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

        $adtSession = Get-ADTSession
        Write-ADTDebugHeader
    }

    process {
        # Return early in silent mode.
        if ($adtSession.IsSilent())
        {
            Write-ADTLogEntry -Message "Bypassing $($MyInvocation.MyCommand.Name) [Mode: $($adtSession.GetPropertyValue('deployMode'))]. Status message: $StatusMessage" -DebugMessage:$Quiet
            return
        }

        # Check if the progress thread is running before invoking methods on it.
        if (!$Script:ProgressWindow.Running)
        {
            # Notify user that the software installation has started.
            Show-ADTBalloonTipClassic -BalloonTipIcon Info -BalloonTipText "$($adtSession.GetDeploymentTypeName()) $((Get-ADTStrings).BalloonText.Start)"

            # Load up the XML file.
            $adtConfig = Get-ADTConfig
            $xaml = [System.Xml.XmlDocument]::new()
            $xaml.Load("$Script:PSScriptRoot\Files\$($MyInvocation.MyCommand.Name).xml")
            $xaml.Window.Title = $xaml.Window.ToolTip = $WindowTitle
            $xaml.Window.TopMost = (!$NotTopMost).ToString()
            $xaml.Window.Grid.TextBlock.Text = $StatusMessage

            # Set up the PowerShell instance and add the initial scriptblock.
            $Script:ProgressWindow.PowerShell = [System.Management.Automation.PowerShell]::Create().AddScript({
                [CmdletBinding()]
                param (
                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullOrEmpty()]
                    [System.Xml.XmlDocument]$Xaml,

                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullOrEmpty()]
                    [System.String]$WindowTitle,

                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullOrEmpty()]
                    [System.IO.FileInfo]$Icon,

                    [Parameter(Mandatory = $true)]
                    [ValidateNotNullOrEmpty()]
                    [System.IO.FileInfo]$Banner,

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

                # Create XAML window.
                $SyncHash.Add('Window', [System.Windows.Markup.XamlReader]::Load([System.Xml.XmlNodeReader]::new($Xaml)))
                $SyncHash.Add('Message', $SyncHash.Window.FindName('ProgressText'))
                $SyncHash.Window.Icon = [System.Windows.Media.Imaging.BitmapFrame]::Create([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Icon)), [System.Windows.Media.Imaging.BitmapCreateOptions]::IgnoreImageCache, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
                $SyncHash.Window.FindName('ProgressBanner').Source = [System.Windows.Media.Imaging.BitmapFrame]::Create([System.IO.MemoryStream]::new([System.IO.File]::ReadAllBytes($Banner)), [System.Windows.Media.Imaging.BitmapCreateOptions]::IgnoreImageCache, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
                $SyncHash.Window.add_MouseLeftButtonDown({$this.DragMove()})
                $SyncHash.Window.add_Loaded({
                    # Relocate the window and disable the X button.
                    & $UpdateWindowLocation.GetNewClosure() -Window $this
                    & $DisableWindowCloseButton.GetNewClosure() -WindowHandle ([System.Windows.Interop.WindowInteropHelper]::new($this).Handle)
                })

                # Bring up the window and capture any errors thereafter.
                [System.Void]$SyncHash.Window.ShowDialog()
                if ($Error.Count) {$SyncHash.Add('Error', $Error)}
            })

            # Add in local variables, assets, and function delegates.
            $Script:ProgressWindow.PowerShell = $Script:ProgressWindow.PowerShell.AddArgument($Xaml).AddArgument($WindowTitle)
            $Script:ProgressWindow.PowerShell = $Script:ProgressWindow.PowerShell.AddArgument($adtConfig.Assets.Logo).AddArgument($adtConfig.Assets.Banner)
            $Script:ProgressWindow.PowerShell = $Script:ProgressWindow.PowerShell.AddArgument(${Function:Update-WindowLocation}).AddArgument(${Function:Disable-ADTWindowCloseButton})

            # Commence invocation.
            Write-ADTLogEntry -Message "Creating the progress dialog in a separate thread with message: [$StatusMessage]."
            $Script:ProgressWindow.PowerShell.Runspace = [System.Management.Automation.Runspaces.RunspaceFactory]::CreateRunspace()
            $Script:ProgressWindow.PowerShell.Runspace.ApartmentState = [System.Threading.ApartmentState]::STA
            $Script:ProgressWindow.PowerShell.Runspace.ThreadOptions = [System.Management.Automation.Runspaces.PSThreadOptions]::ReuseThread
            $Script:ProgressWindow.PowerShell.Runspace.Open()
            $Script:ProgressWindow.PowerShell.Runspace.SessionStateProxy.SetVariable('SyncHash', $Script:ProgressWindow.SyncHash)
            $Script:ProgressWindow.Invocation = $Script:ProgressWindow.PowerShell.BeginInvoke()

            # Allow the thread to be spun up safely before invoking actions against it.
            while (!($Script:ProgressWindow.Running = $Script:ProgressWindow.SyncHash.ContainsKey('Window') -and $Script:ProgressWindow.SyncHash.Window.IsInitialized -and $Script:ProgressWindow.SyncHash.Window.Dispatcher.Thread.ThreadState.Equals([System.Threading.ThreadState]::Running)))
            {
                if ($Script:ProgressWindow.SyncHash.ContainsKey('Error') -and $Script:ProgressWindow.SyncHash.Error.Count)
                {
                    Write-ADTLogEntry -Message "Failure while displaying progress dialog.`n$(Resolve-ADTError -ErrorRecord $Script:ProgressWindow.SyncHash.Error)" -Severity 3
                    Close-ADTInstallationProgressClassic
                    break
                }
                elseif ($Script:ProgressWindow.Invocation.IsCompleted)
                {
                    try
                    {
                        [System.Void]$Script:ProgressWindow.PowerShell.EndInvoke($Script:ProgressWindow.Invocation)
                    }
                    catch
                    {
                        Write-ADTLogEntry -Message "Failure while displaying progress dialog.`n$(Resolve-ADTError -ErrorRecord $_)" -Severity 3
                    }
                    finally
                    {
                        $Script:ProgressWindow.Invocation = $null
                        Close-ADTInstallationProgressClassic
                    }
                    break
                }
            }
        }
        else
        {
            # Invoke update events against an established window.
            $Script:ProgressWindow.SyncHash.Window.Dispatcher.Invoke(
                {
                    $Script:ProgressWindow.SyncHash.Window.Title = $WindowTitle
                    $Script:ProgressWindow.SyncHash.Message.Text = $StatusMessage
                    if (!$NoRelocation)
                    {
                        Update-WindowLocation -Window $Script:ProgressWindow.SyncHash.Window
                    }
                },
                [System.Windows.Threading.DispatcherPriority]::Send
            )
            Write-ADTLogEntry -Message "Updated the progress message: [$StatusMessage]." -DebugMessage:$Quiet
        }
    }

    end {
        Write-ADTDebugFooter
    }
}
