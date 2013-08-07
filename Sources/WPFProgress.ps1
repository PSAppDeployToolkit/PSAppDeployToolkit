$configIconFilePath = "C:\Users\Public\PwC\Icons\PwCLogo.ico"
$configProgressMessage = "Installation In Progress... Please Wait. "    
$installTitle = "PwC Installation"

Function Show-InstallationProgress {
    # Calculate the position 
    Add-Type -AssemblyName System.Windows.Forms
    $screenBounds = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds     
    # Create a synchronized hashtable to share objects between runspaces
    $Global:ProgressSyncHash = [hashtable]::Synchronized(@{})
    # Create a new runspace for the progress bar
    $progressRunspace =[runspacefactory]::CreateRunspace()
    $progressRunspace.ApartmentState = "STA"
    $progressRunspace.ThreadOptions = "ReuseThread"          
    $progressRunspace.Open()
    # Add the sync hash to the runspace
    $progressRunspace.SessionStateProxy.SetVariable("ProgressSyncHash",$Global:ProgressSyncHash)   
    # Add other variables from the parent thread required in the progress runspace
    $progressRunspace.SessionStateProxy.SetVariable("installTitle",$installTitle) 
    $progressRunspace.SessionStateProxy.SetVariable("configProgressMessage",$configProgressMessage)   
    $progressRunspace.SessionStateProxy.SetVariable("configIconFilePath",$configIconFilePath)           
    $ProgressRunspace.SessionStateProxy.SetVariable("screenBounds",$screenBounds)    
    # Add the script block to be execution in the progress runspace          
    $progressCmd = [PowerShell]::Create().AddScript({   

    [xml]$xamlProgress = @"
        <Window
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" 
            x:Name="Window" Title=""
            MaxHeight="160" MinHeight="160" Height="160" 
            MaxWidth="520" MinWidth="500" Width="500"
            WindowStartupLocation = "Manual"
            Top=""
            Left=""
            Topmost="True"   
            ResizeMode="NoResize"  
            Icon=""
            ShowInTaskbar="True" >
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
            <Grid Background="White">
                <Grid.RowDefinitions>
                    <RowDefinition Height="90"/>
                    <RowDefinition Height="60"/>
                </Grid.RowDefinitions>
                <TextBlock x:Name = "ProgressText" Grid.Row="0" Grid.Column="0" Margin="0,0,0,0" Text="" FontSize="14" HorizontalAlignment="Center" VerticalAlignment="Center" TextAlignment="Center" TextWrapping="Wrap"></TextBlock>
                <Ellipse x:Name="ellipse" Grid.Row="1" Grid.Column="0" Margin="0,0,0,35" StrokeThickness="5" RenderTransformOrigin="0.5,0.5" Height="25" Width="25">
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
"@

    ## Set the configurable values based using variables addded to the runspace from the parent thread   
    # Select the screen heigth and width   
    $screenWidth = $screenBounds | Select Width -ExpandProperty Width    
    $screenHeight = $screenBounds | Select Height -ExpandProperty Height    
    # Set the start position of the Window based on the screen size
    $xamlProgress.Window.Left =  [string](($screenWidth / 2) - ($xamlProgress.Window.Width /2))
    $xamlProgress.Window.Top = [string]($screenHeight / 10)
    $xamlProgress.Window.Icon = $configIconFilePath
    $xamlProgress.Window.Grid.TextBlock.Text = $configProgressMessage  
    $xamlProgress.Window.Title = $installTitle    
    $progressReader = (New-Object System.Xml.XmlNodeReader $xamlProgress)
    $Global:ProgressSyncHash.Window = [Windows.Markup.XamlReader]::Load( $progressReader )    
    $Global:ProgressSyncHash.ProgressText = $Global:ProgressSyncHash.Window.FindName("ProgressText")       
    # Add an action to the Windo.Closing event handler to disable the close button
    $Global:ProgressSyncHash.Window.Add_Closing({$_.Cancel = $true}) 
    # Allow the window to be dragged by clicking on it anywhere
    $Global:ProgressSyncHash.Window.Add_MouseLeftButtonDown({$Global:ProgressSyncHash.Window.DragMove()})  
    # Add a tooltip  
    $Global:ProgressSyncHash.Window.ToolTip = $installTitle
    $Global:ProgressSyncHash.Window.ShowDialog() | Out-Null    
    $Global:ProgressSyncHash.Error = $Error
    })

    $progressCmd.Runspace = $progressRunspace
    # Invoke the progress runspace
    $progressData = $progressCmd.BeginInvoke()
    # Allow the thread to be spin up safely before invoking actions against it.
    Sleep -Seconds 1
}

Function Update-InstallationProgress {
    Param (
		[string]$statusMessage = $(throw "Status Message param required")
	)
	
    If ($silentMode -ne $true) {
        # Check if the progress thread is running before invoking methods on it
        If ($Global:ProgressSyncHash.Window.Dispatcher.Thread.ThreadState -eq "Running") {
            # Update the progress text
            $Global:ProgressSyncHash.Window.Dispatcher.Invoke("Normal",[action]{$Global:ProgressSyncHash.ProgressText.Text=$statusMessage})  
            
        }
    }
}

Show-InstallationProgress


Update-InstallationProgress -statusMessage "The installation may take 40 minutes to complete."

Sleep -Seconds 3

Update-InstallationProgress -statusMessage "The installation may take 60 minutes to complete."

Sleep -Seconds 3

Update-InstallationProgress -statusMessage "The installation may take 80 minutes to complete."

Sleep -Seconds 2

            # Close the progress thread 
            $Global:ProgressSyncHash.Window.Dispatcher.InvokeShutdown()
            $Global:ProgressSyncHash.Clear()
            # Close the progress run space
            #$progressRunspace.Close()