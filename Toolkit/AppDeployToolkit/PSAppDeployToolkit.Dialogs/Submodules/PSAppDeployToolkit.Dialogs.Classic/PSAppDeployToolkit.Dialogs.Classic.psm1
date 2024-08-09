#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3

# Set process DPI awareness before importing anything else.
Set-ADTProcessDpiAware

# Add system types required by the module.
Add-Type -AssemblyName System.Drawing, System.Windows.Forms, PresentationCore, PresentationFramework, WindowsBase

# All WinForms-specific initialistion code.
[System.Windows.Forms.Application]::EnableVisualStyles()
try {[System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)} catch {$null = $null}

# Dot-source our imports and perform exports.
Export-ModuleMember -Function (Get-ChildItem -Path $PSScriptRoot\*\*.ps1).ForEach({
    # As we declare all functions read-only, attempt removal before dot-sourcing the function again.
    Remove-Item -LiteralPath "Function:$($_.BaseName)" -Force -ErrorAction Ignore

    # Dot source in the function code.
    . $_.FullName

    # Mark the dot-sourced function as read-only.
    Set-Item -LiteralPath "Function:$($_.BaseName)" -Options ReadOnly

    # Echo out the public functions.
    if ($_.DirectoryName.EndsWith('Public'))
    {
        return $_.BaseName
    }
})

# WinForms global data.
New-Variable -Name FormData -Option Constant -Value @{
    Font = [System.Drawing.SystemFonts]::MessageBoxFont
    Width = 450
    BannerHeight = 0
    Assets = @{
        Icon = $null
        Logo = $null
        Banner = $null
    }
}

# State data for the Installation Progress window.
New-Variable -Name ProgressWindow -Option Constant -Value @{
    SyncHash = [System.Collections.Hashtable]::Synchronized(@{})
    XamlCode = [System.Xml.XmlDocument]::new()
    PowerShell = $null
    Invocation = $null
    Running = $false
}

# Import the XML code for the progress window.
$ProgressWindow.XamlCode.Load([System.IO.StringReader]::new(@'
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" x:Name="Window" Title="" ToolTip="" Padding="0,0,0,0" Margin="0,0,0,0" WindowStartupLocation="Manual" Top="0" Left="0" Topmost="" ResizeMode="NoResize" ShowInTaskbar="True" VerticalContentAlignment="Center" HorizontalContentAlignment="Center" SizeToContent="WidthAndHeight">
    <Window.Resources>
        <Storyboard x:Key="Storyboard1" RepeatBehavior="Forever">
            <DoubleAnimationUsingKeyFrames BeginTime="00:00:00" Storyboard.TargetName="ellipse" Storyboard.TargetProperty="(UIElement.RenderTransform).(TransformGroup.Children)[2].(RotateTransform.Angle)">
                <SplineDoubleKeyFrame KeyTime="00:00:02" Value="360" />
            </DoubleAnimationUsingKeyFrames>
        </Storyboard>
    </Window.Resources>
    <Window.Triggers>
        <EventTrigger RoutedEvent="FrameworkElement.Loaded">
            <BeginStoryboard Storyboard="{StaticResource Storyboard1}" />
        </EventTrigger>
    </Window.Triggers>
    <Grid Background="#F0F0F0" MinWidth="450" MaxWidth="450" Width="450">
        <Grid.RowDefinitions>
            <RowDefinition Height="*" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>
        <Grid.ColumnDefinitions>
            <ColumnDefinition MinWidth="100" MaxWidth="100" Width="100" />
            <ColumnDefinition MinWidth="350" MaxWidth="350" Width="350" />
        </Grid.ColumnDefinitions>
        <Image x:Name="ProgressBanner" Grid.ColumnSpan="2" Margin="0,0,0,0" Grid.Row="0" />
        <TextBlock x:Name="ProgressText" Grid.Row="1" Grid.Column="1" Margin="0,30,64,30" Text="" FontSize="14" HorizontalAlignment="Center" VerticalAlignment="Center" TextAlignment="Center" Padding="10,0,10,0" TextWrapping="Wrap" />
        <Ellipse x:Name="ellipse" Grid.Row="1" Grid.Column="0" Margin="0,0,0,0" StrokeThickness="5" RenderTransformOrigin="0.5,0.5" Height="32" Width="32" HorizontalAlignment="Center" VerticalAlignment="Center">
            <Ellipse.RenderTransform>
                <TransformGroup>
                    <ScaleTransform />
                    <SkewTransform />
                    <RotateTransform />
                </TransformGroup>
            </Ellipse.RenderTransform>
            <Ellipse.Stroke>
                <LinearGradientBrush EndPoint="0.445,0.997" StartPoint="0.555,0.003">
                    <GradientStop Color="White" Offset="0" />
                    <GradientStop Color="#0078d4" Offset="1" />
                </LinearGradientBrush>
            </Ellipse.Stroke>
        </Ellipse>
    </Grid>
</Window>
'@))
