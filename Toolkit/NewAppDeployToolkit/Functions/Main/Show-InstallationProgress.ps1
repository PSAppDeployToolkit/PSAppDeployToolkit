#region Function Show-InstallationProgress
Function Show-InstallationProgress {
<#
.SYNOPSIS
	Displays a progress dialog in a separate thread with an updateable custom message.
.DESCRIPTION
	Create a WPF window in a separate thread to display a marquee style progress ellipse with a custom message that can be updated.
	The status message supports line breaks.
	The first time this function is called in a script, it will display a balloon tip notification to indicate that the installation has started (provided balloon tips are enabled in the configuration).
.PARAMETER StatusMessage
	The status message to be displayed. The default status message is taken from the XML configuration file.
.PARAMETER WindowLocation
	The location of the progress window. Default: center of the screen.
.PARAMETER TopMost
	Specifies whether the progress window should be topmost. Default: $true.
.EXAMPLE
	Show-InstallationProgress
	Uses the default status message from the XML configuration file.
.EXAMPLE
	Show-InstallationProgress -StatusMessage 'Installation in Progress...'
.EXAMPLE
	Show-InstallationProgress -StatusMessage "Installation in Progress...`r`nThe installation may take 20 minutes to complete."
.EXAMPLE
	Show-InstallationProgress -StatusMessage 'Installation in Progress...' -WindowLocation 'BottomRight' -TopMost $false
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$StatusMessage = $configProgressMessageInstall,
		[Parameter(Mandatory=$false)]
		[ValidateSet('Default','BottomRight','TopCenter')]
		[string]$WindowLocation = 'Default',
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$TopMost = $true
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		If ($deployModeSilent) {
			Write-Log -Message "Bypassing Show-InstallationProgress [Mode: $deployMode]. Status message:$StatusMessage" -Source ${CmdletName}
			Return
		}

		## If the default progress message hasn't been overridden and the deployment type is uninstall, use the default uninstallation message
		If ($StatusMessage -eq $configProgressMessageInstall) {
			if ($deploymentType -eq 'Uninstall') {
				$StatusMessage = $configProgressMessageUninstall
			} elseif ($deploymentType -eq 'Repair') {
				$StatusMessage = $configProgressMessageRepair
			}
		}

		If ($envHost.Name -match 'PowerGUI') {
			Write-Log -Message "$($envHost.Name) is not a supported host for WPF multi-threading. Progress dialog with message [$statusMessage] will not be displayed." -Severity 2 -Source ${CmdletName}
			Return
		}

		## Check if the progress thread is running before invoking methods on it
		If ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -ne 'Running') {
			#  Notify user that the software installation has started
			$balloonText = "$deploymentTypeName $configBalloonTextStart"
			Show-BalloonTip -BalloonTipIcon 'Info' -BalloonTipText $balloonText
			#  Create a synchronized hashtable to share objects between runspaces
			$script:ProgressSyncHash = [hashtable]::Synchronized(@{ })
			#  Create a new runspace for the progress bar
			$script:ProgressRunspace = [runspacefactory]::CreateRunspace()
			$script:ProgressRunspace.ApartmentState = 'STA'
			$script:ProgressRunspace.ThreadOptions = 'ReuseThread'
			$script:ProgressRunspace.Open()
			#  Add the sync hash to the runspace
			$script:ProgressRunspace.SessionStateProxy.SetVariable('progressSyncHash', $script:ProgressSyncHash)
			#  Add other variables from the parent thread required in the progress runspace
			$script:ProgressRunspace.SessionStateProxy.SetVariable('installTitle', $installTitle)
			$script:ProgressRunspace.SessionStateProxy.SetVariable('windowLocation', $windowLocation)
			$script:ProgressRunspace.SessionStateProxy.SetVariable('topMost', $topMost.ToString())
			$script:ProgressRunspace.SessionStateProxy.SetVariable('appDeployLogoBanner', $appDeployLogoBanner)
			$script:ProgressRunspace.SessionStateProxy.SetVariable('ProgressStatusMessage', $statusMessage)
			$script:ProgressRunspace.SessionStateProxy.SetVariable('AppDeployLogoIcon', $AppDeployLogoIcon)

			#  Add the script block to be executed in the progress runspace
			$progressCmd = [PowerShell]::Create().AddScript({
				[string]$xamlProgressString = @'
				<Window
				xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
				xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
				x:Name="Window" Title="PSAppDeployToolkit"
				Padding="0,0,0,0" Margin="0,0,0,0"
				WindowStartupLocation = "Manual"
				Icon=""
				Top="0"
				Left="0"
				Topmost="True"
				ResizeMode="NoResize"
				ShowInTaskbar="True" VerticalContentAlignment="Center" HorizontalContentAlignment="Center" SizeToContent="WidthAndHeight">
					<Window.Resources>
					<Storyboard x:Key="Storyboard1" RepeatBehavior="Forever">
					<DoubleAnimationUsingKeyFrames BeginTime="00:00:00" Storyboard.TargetName="ellipse" Storyboard.TargetProperty="(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)">
						<SplineDoubleKeyFrame KeyTime="00:00:02" Value="360"/>
					</DoubleAnimationUsingKeyFrames>
					</Storyboard>
					</Window.Resources>
					<Window.Triggers>
					<EventTrigger RoutedEvent="FrameworkElement.Loaded">
					<BeginStoryboard Storyboard="{StaticResource Storyboard1}"/>
					</EventTrigger>
					</Window.Triggers>
					<Grid Background="#F0F0F0" MinWidth="450" MaxWidth="450" Width="450">
					<Grid.RowDefinitions>
					<RowDefinition Height="*"/>
					<RowDefinition Height="*"/>
					</Grid.RowDefinitions>
					<Grid.ColumnDefinitions>
					<ColumnDefinition MinWidth="50" MaxWidth="50" Width="50"></ColumnDefinition>
					<ColumnDefinition MinWidth="400" MaxWidth="400" Width="400"></ColumnDefinition>
					</Grid.ColumnDefinitions>
					<Image x:Name = "ProgressBanner" Grid.ColumnSpan="2" Margin="0,0,0,0" Source="" Grid.Row="0"/>
					<TextBlock x:Name = "ProgressText" Grid.Row="1" Grid.Column="1" Margin="0,20,50,20" Text="Installation in progress" FontSize="14" FontFamily="Microsoft Sans Serif" HorizontalAlignment="Center" VerticalAlignment="Center" TextAlignment="Center" Padding="10,0,10,0" TextWrapping="Wrap"></TextBlock>
					<Ellipse x:Name = "ellipse" Grid.Row="1" Grid.Column="0" Margin="0,0,5,0" StrokeThickness="5" RenderTransformOrigin="0.5,0.5" Height="25" Width="25" HorizontalAlignment="Right" VerticalAlignment="Center">
					<Ellipse.RenderTransform>
						<TransformGroup>
							<ScaleTransform/>
							<SkewTransform/>
							<RotateTransform/>
						</TransformGroup>
					</Ellipse.RenderTransform>
					<Ellipse.Stroke>
						<LinearGradientBrush EndPoint="0.445,0.997" StartPoint="0.555,0.003">
							<GradientStop Color="White" Offset="0"/>
							<GradientStop Color="#008000" Offset="1"/>
						</LinearGradientBrush>
					</Ellipse.Stroke>
					</Ellipse>
					</Grid>
				</Window>
'@
				[Xml.XmlDocument]$xamlProgress = New-Object 'System.Xml.XmlDocument'
				$xamlProgress.LoadXml($xamlProgressString)
				## Set the configurable values using variables added to the runspace from the parent thread
				$xamlProgress.Window.TopMost = $topMost
				$xamlProgress.Window.Icon = $AppDeployLogoIcon
				$xamlProgress.Window.Grid.Image.Source = $appDeployLogoBanner
				$xamlProgress.Window.Grid.TextBlock.Text = $ProgressStatusMessage
				$xamlProgress.Window.Title = $installTitle
				#  Parse the XAML
				$progressReader = New-Object -TypeName 'System.Xml.XmlNodeReader' -ArgumentList $xamlProgress
				$script:ProgressSyncHash.Window = [Windows.Markup.XamlReader]::Load($progressReader)
				#  Grey out the X button
				$script:ProgressSyncHash.Window.add_Loaded({
					#  Calculate the position on the screen where the progress dialog should be placed
					[int32]$screenWidth = [System.Windows.SystemParameters]::WorkArea.Width
					[int32]$screenHeight = [System.Windows.SystemParameters]::WorkArea.Height
					[int32]$screenCenterWidth = $screenWidth - $script:ProgressSyncHash.Window.ActualWidth
					[int32]$screenCenterHeight = $screenHeight - $script:ProgressSyncHash.Window.ActualHeight
					#  Set the start position of the Window based on the screen size
					If ($windowLocation -eq 'BottomRight') {
						#  Put the window in the corner
						$script:ProgressSyncHash.Window.Left = [Double]($screenCenterWidth)
						$script:ProgressSyncHash.Window.Top = [Double]($screenCenterHeight)
					}
					ElseIf($windowLocation -eq 'TopCenter'){
						$script:ProgressSyncHash.Window.Left = [Double]($screenCenterWidth / 2)
						$script:ProgressSyncHash.Window.Top = [Double]($screenCenterHeight / 6)
					}
					Else {
						#  Center the progress window by calculating the center of the workable screen based on the width of the screen minus half the width of the progress bar
						$script:ProgressSyncHash.Window.Left = [Double]($screenCenterWidth / 2)
						$script:ProgressSyncHash.Window.Top = [Double]($screenCenterHeight / 2)
					}
					#  Disable the X button
					try {
						$windowHandle = (New-Object -TypeName System.Windows.Interop.WindowInteropHelper -ArgumentList $this).Handle
						If ($windowHandle -and ($windowHandle -ne [IntPtr]::Zero)) {
							$menuHandle = [PSADT.UiAutomation]::GetSystemMenu($windowHandle, $false)
							If ($menuHandle -and ($menuHandle -ne [IntPtr]::Zero)) {
								[PSADT.UiAutomation]::EnableMenuItem($menuHandle, 0xF060, 0x00000001)
								[PSADT.UiAutomation]::DestroyMenu($menuHandle)
							}
						}
					}
					catch {
						# Not a terminating error if we can't disable the close button
						Write-Log "Failed to disable the Close button." -Severity 2 -Source ${CmdletName}
					}
				})
				#  Prepare the ProgressText variable so we can use it to change the text in the text area
				$script:ProgressSyncHash.ProgressText = $script:ProgressSyncHash.Window.FindName('ProgressText')
				#  Add an action to the Window.Closing event handler to disable the close button
				$script:ProgressSyncHash.Window.Add_Closing({ $_.Cancel = $true })
				#  Allow the window to be dragged by clicking on it anywhere
				$script:ProgressSyncHash.Window.Add_MouseLeftButtonDown({ $script:ProgressSyncHash.Window.DragMove() })
				#  Add a tooltip
				$script:ProgressSyncHash.Window.ToolTip = $installTitle
				$null = $script:ProgressSyncHash.Window.ShowDialog()
				$script:ProgressSyncHash.Error = $Error
			})

			$progressCmd.Runspace = $script:ProgressRunspace
			Write-Log -Message "Creating the progress dialog in a separate thread with message: [$statusMessage]." -Source ${CmdletName}
			#  Invoke the progress runspace
			$null = $progressCmd.BeginInvoke()
			#  Allow the thread to be spun up safely before invoking actions against it.
			Start-Sleep -Seconds 1
			If ($script:ProgressSyncHash.Error) {
				Write-Log -Message "Failure while displaying progress dialog. `r`n$(Resolve-Error -ErrorRecord $script:ProgressSyncHash.Error)" -Severity 3 -Source ${CmdletName}
			}
		}
		## Check if the progress thread is running before invoking methods on it
		ElseIf ($script:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -eq 'Running') {
			#  Update the progress text
			Try {
				$script:ProgressSyncHash.Window.Dispatcher.Invoke([Windows.Threading.DispatcherPriority]::Send, [Windows.Input.InputEventHandler]{ $script:ProgressSyncHash.ProgressText.Text = $statusMessage }, $null, $null)
				Write-Log -Message "Updated the progress message: [$statusMessage]." -Source ${CmdletName}
			}
			Catch {
				Write-Log -Message "Unable to update the progress message. `r`n$(Resolve-Error)" -Severity 2 -Source ${CmdletName}
			}
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
