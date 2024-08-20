#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue

# Build out lookup table for all cmdlets used within module, starting with the core cmdlets.
$ModuleManifest = [System.Management.Automation.Language.Parser]::ParseFile("$PSScriptRoot\$($MyInvocation.MyCommand.ScriptBlock.Module.Name).psd1", [ref]$null, [ref]$null).EndBlock.Statements.PipelineElements.Expression.SafeGetValue()
$CommandTable = [ordered]@{}; $ExecutionContext.SessionState.InvokeCommand.GetCmdlets() | & { process { if ($_.PSSnapIn -and $_.PSSnapIn.Name.Equals('Microsoft.PowerShell.Core') -and $_.PSSnapIn.IsDefault) { $CommandTable.Add($_.Name, $_) } } }
& $CommandTable.'Get-Command' -FullyQualifiedModule $ModuleManifest.RequiredModules | & { process { $CommandTable.Add($_.Name, $_) } }
& $CommandTable.'New-Variable' -Name CommandTable -Value $CommandTable.AsReadOnly() -Option Constant -Force -Confirm:$false
& $CommandTable.'New-Variable' -Name ModuleManifest -Value $ModuleManifest -Option Constant -Force -Confirm:$false

# Ensure module operates under the strictest of conditions.
& $CommandTable.'Set-StrictMode' -Version 3

# Add the custom types required for the toolkit.
& $CommandTable.'Add-Type' -LiteralPath "$PSScriptRoot\$($MyInvocation.MyCommand.ScriptBlock.Module.Name).cs" -ReferencedAssemblies $(
    'System.DirectoryServices'
    if ($PSVersionTable.PSEdition.Equals('Core'))
    {
        'System.Net.NameResolution', 'System.Collections', 'System.Collections.Specialized', 'System.Text.RegularExpressions', 'System.Security.Principal.Windows', 'System.ComponentModel.Primitives', 'Microsoft.Win32.Primitives'
    }
)

# Set the process as HiDPI so long as we're in a real console.
if ($Host.Name.Equals('ConsoleHost'))
{
    # Use the most recent API supported by the operating system.
    $null = switch ([System.Environment]::OSVersion.Version)
    {
        { $_ -ge '10.0.15063.0' }
        {
            [PSADT.UiAutomation]::SetProcessDpiAwarenessContext([PSADT.UiAutomation+DPI_AWARENESS_CONTEXT]::DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)
            break
        }
        { $_ -ge '10.0.14393.0' }
        {
            [PSADT.UiAutomation]::SetProcessDpiAwarenessContext([PSADT.UiAutomation+DPI_AWARENESS_CONTEXT]::DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE)
            break
        }
        { $_ -ge '6.3.9600.0' }
        {
            [PSADT.UiAutomation]::SetProcessDpiAwareness([PSADT.UiAutomation+PROCESS_DPI_AWARENESS]::PROCESS_PER_MONITOR_DPI_AWARE)
            break
        }
        { $_ -ge '6.0.6000.0' }
        {
            [PSADT.UiAutomation]::SetProcessDPIAware()
            break
        }
    }
}

# Add system types required by the module.
& $CommandTable.'Add-Type' -AssemblyName System.ServiceProcess, System.Drawing, System.Windows.Forms, PresentationCore, PresentationFramework, WindowsBase

# All WinForms-specific initialistion code.
try
{
    [System.Windows.Forms.Application]::EnableVisualStyles()
    [System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)
}
catch
{
    $null = $null
}

# Dot-source our imports and perform exports.
& $CommandTable.'New-Variable' -Name ModuleFiles -Option Constant -Value ([System.IO.FileInfo[]]$([System.IO.Directory]::GetFiles("$PSScriptRoot\Classes"); [System.IO.Directory]::GetFiles("$PSScriptRoot\Private"); [System.IO.Directory]::GetFiles("$PSScriptRoot\Public")))
& $CommandTable.'New-Variable' -Name FunctionPaths -Option Constant -Value ($ModuleFiles.BaseName -replace '^', 'Microsoft.PowerShell.Core\Function::')
& $CommandTable.'Remove-Item' -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
$ModuleFiles.FullName | . { process { . $_ } }
& $CommandTable.'Set-Item' -LiteralPath $FunctionPaths -Options ReadOnly
& $CommandTable.'Export-ModuleMember' -Function $ModuleManifest.FunctionsToExport

# Define object for holding all PSADT variables.
& $CommandTable.'New-Variable' -Name ADT -Option Constant -Value ([pscustomobject]@{
        Callbacks = [pscustomobject]@{
            Starting = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
            Opening = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
            Closing = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
            Finishing = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
        }
        Sessions = [System.Collections.Generic.List[ADTSession]]::new()
        TerminalServerMode = $false
        Environment = $null
        Language = $null
        Config = $null
        Strings = $null
        LastExitCode = 0
        Initialised = $false
    })

# Define object for holding all dialog window variables.
& $CommandTable.'New-Variable' -Name Dialogs -Option Constant -Value ([ordered]@{
        Classic = [pscustomobject]@{
            ProgressWindow = [pscustomobject]@{
                SyncHash = [System.Collections.Hashtable]::Synchronized(@{})
                XamlCode = $null
                PowerShell = $null
                Invocation = $null
                Running = $false
            }
            Assets = [pscustomobject]@{
                Icon = $null
                Logo = $null
                Banner = $null
            }
            Font = [System.Drawing.SystemFonts]::MessageBoxFont
            Width = 450
            BannerHeight = 0
        }
        Fluent = [pscustomobject]@{
            ProgressWindow = [pscustomobject]@{
                Window = $null
                Thread = $null
                Running = $false
            }
        }
    }).AsReadOnly()

# Define dialog function dispatcher between classic/fluent dialogs.
& $CommandTable.'New-Variable' -Name DialogDispatcher -Option Constant -Value ([ordered]@{
        Classic = ([ordered]@{
                'Close-ADTInstallationProgress'     = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Close-ADTInstallationProgressClassic
                'Show-ADTBalloonTip'                = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTBalloonTipClassic
                'Show-ADTInstallationProgress'      = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTInstallationProgressClassic
                'Show-ADTInstallationPrompt'        = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTInstallationPromptClassic
                'Show-ADTInstallationRestartPrompt' = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTInstallationRestartPromptClassic
                'Show-ADTInstallationWelcome'       = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTWelcomePromptClassic
            }).AsReadOnly()
        Fluent = ([ordered]@{
                'Close-ADTInstallationProgress'     = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Close-ADTInstallationProgressFluent
                'Show-ADTBalloonTip'                = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTBalloonTipFluent
                'Show-ADTInstallationProgress'      = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTInstallationProgressFluent
                'Show-ADTInstallationPrompt'        = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTInstallationPromptClassic
                'Show-ADTInstallationRestartPrompt' = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTInstallationRestartPromptClassic
                'Show-ADTInstallationWelcome'       = & $CommandTable.'Get-Item' -LiteralPath Microsoft.PowerShell.Core\Function::Show-ADTWelcomePromptClassic
            }).AsReadOnly()
    }).AsReadOnly()

# Logging constants used within an [ADTSession] object.
& $CommandTable.'New-Variable' -Name Logging -Option Constant -Value ([ordered]@{
        Formats = ([ordered]@{
                CMTrace = "<![LOG[[{1}] :: {0}]LOG]!><time=`"{2}`" date=`"{3}`" component=`"{4}`" context=`"$([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)`" type=`"{5}`" thread=`"$PID`" file=`"{6}`">"
                Legacy = '[{1} {2}] [{3}] [{4}] [{5}] :: {0}'
            }).AsReadOnly()
        SeverityNames = [System.Array]::AsReadOnly([System.String[]]$(
                'Success'
                'Info'
                'Warning'
                'Error'
            ))
        SeverityColours = [System.Array]::AsReadOnly([System.Collections.Specialized.OrderedDictionary[]]$(
                ([ordered]@{ ForegroundColor = [System.ConsoleColor]::Green; BackgroundColor = [System.ConsoleColor]::Black }).AsReadOnly()
                ([ordered]@{}).AsReadOnly()
                ([ordered]@{ ForegroundColor = [System.ConsoleColor]::Yellow; BackgroundColor = [System.ConsoleColor]::Black }).AsReadOnly()
                ([ordered]@{ ForegroundColor = [System.ConsoleColor]::Red; BackgroundColor = [System.ConsoleColor]::Black }).AsReadOnly()
            ))
    }).AsReadOnly()

# DialogBox constants used within Show-ADTDialogBox.
& $CommandTable.'New-Variable' -Name DialogBox -Option Constant -Value ([ordered]@{
        Buttons = ([ordered]@{
                OK = 0
                OKCancel = 1
                AbortRetryIgnore = 2
                YesNoCancel = 3
                YesNo = 4
                RetryCancel = 5
                CancelTryAgainContinue = 6
            }).AsReadOnly()
        Icons = ([ordered]@{
                None = 0
                Stop = 16
                Question = 32
                Exclamation = 48
                Information = 64
            }).AsReadOnly()
        DefaultButtons = ([ordered]@{
                First = 0
                Second = 256
                Third = 512
            }).AsReadOnly()
    }).AsReadOnly()

# Registry path transformation constants used within Convert-ADTRegistryPath.
& $CommandTable.'New-Variable' -Name Registry -Option Constant -Value ([ordered]@{
        PathMatches = [System.Array]::AsReadOnly([System.String[]]$(
                ':\\'
                ':'
                '\\'
            ))
        PathReplacements = ([ordered]@{
                '^HKLM' = 'HKEY_LOCAL_MACHINE\'
                '^HKCR' = 'HKEY_CLASSES_ROOT\'
                '^HKCU' = 'HKEY_CURRENT_USER\'
                '^HKU' = 'HKEY_USERS\'
                '^HKCC' = 'HKEY_CURRENT_CONFIG\'
                '^HKPD' = 'HKEY_PERFORMANCE_DATA\'
            }).AsReadOnly()
        WOW64Replacements = ([ordered]@{
                '^(HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\|HKEY_CURRENT_USER\\SOFTWARE\\Classes\\|HKEY_CLASSES_ROOT\\)(AppID\\|CLSID\\|DirectShow\\|Interface\\|Media Type\\|MediaFoundation\\|PROTOCOLS\\|TypeLib\\)' = '$1Wow6432Node\$2'
                '^HKEY_LOCAL_MACHINE\\SOFTWARE\\' = 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\'
                '^HKEY_LOCAL_MACHINE\\SOFTWARE$' = 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node'
                '^HKEY_CURRENT_USER\\Software\\Microsoft\\Active Setup\\Installed Components\\' = 'HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\'
            }).AsReadOnly()
    }).AsReadOnly()

# Import the XML code for the classic progress window.
$Dialogs.Classic.ProgressWindow.XamlCode = [System.IO.StringReader]::new(@'
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
'@)
