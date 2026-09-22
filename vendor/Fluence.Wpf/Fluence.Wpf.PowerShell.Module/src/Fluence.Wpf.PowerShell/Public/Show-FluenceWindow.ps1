function Show-FluenceWindow
{
    <#
    .SYNOPSIS
        Shows a themed Fluent (FluenceWindow) window built from a content scriptblock or XAML, and
        returns the value the window stashed before closing.
    .DESCRIPTION
        Hosts a full Fluence.Wpf window from PowerShell. Three shapes are supported: a -Content
        scriptblock that builds the window body in code, a -Xaml string parsed at runtime, or a
        -XamlPath file loaded at runtime. A XAML root that is itself a Window is shown directly; any
        other root is hosted inside a FluenceWindow. The optional -Initialize block (XAML modes) runs
        against the window after it is built, so named controls can be wired with FindName and add_*
        handlers. Close the window with Close-FluenceWindow -Result <value> to set the return value;
        the value stashed on the window's Tag is returned. XAML and callbacks are trusted code:
        do not load XAML from an untrusted source. XamlReader does not provide a sandbox.
    .PARAMETER Content
        A scriptblock that builds the window body. It receives the FluenceWindow as the first argument
        and the -Data hashtable as the second, and typically sets $Window.Content and wires handlers.
    .PARAMETER Xaml
        A XAML string parsed with XamlReader.Parse. A FluenceWindow (or any Window) root is shown
        directly; any other root is hosted inside a new FluenceWindow.
    .PARAMETER XamlPath
        A path to a .xaml file loaded with XamlReader.Load. The path is resolved on the calling thread.
    .PARAMETER Initialize
        A scriptblock run against the window after XAML is loaded (XamlString and XamlFile only). It
        receives the window as the first argument and the -Data hashtable as the second.
    .PARAMETER Title
        The window title. Default 'Fluence'.
    .PARAMETER Width
        The window width in device-independent pixels.
    .PARAMETER Height
        The window height in device-independent pixels.
    .PARAMETER MinWidth
        The minimum window width.
    .PARAMETER MinHeight
        The minimum window height.
    .PARAMETER SizeToContent
        How the window sizes to its content (Manual, Width, Height, or WidthAndHeight). Default Manual.
    .PARAMETER StartupLocation
        Where the window first appears (Manual, CenterScreen, or CenterOwner). Default CenterScreen;
        forced to CenterOwner when -Owner is supplied.
    .PARAMETER Topmost
        Show the window above other windows.
    .PARAMETER ResizeMode
        The window resize mode (NoResize, CanMinimize, CanResize, or CanResizeWithGrip).
    .PARAMETER CornerStyle
        The DWM rounded-corner preference: Default, DoNotRound, Round, or RoundSmall.
    .PARAMETER ExtendsContentIntoTitleBar
        Extend the window content into the title bar area.
    .PARAMETER ShowTitle
        Show the title text in the title bar.
    .PARAMETER ShowIcon
        Show the window icon in the title bar.
    .PARAMETER NoMinimizeButton
        Hide the minimize caption button.
    .PARAMETER NoMaximizeButton
        Hide the maximize caption button.
    .PARAMETER NoCloseButton
        Hide the close caption button.
    .PARAMETER Theme
        Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process;
        the first Fluence call in a process applies Auto.
    .PARAMETER Backdrop
        Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the
        process; the first Fluence call in a process applies Mica.
    .PARAMETER Accent
        Optional accent color (System.Windows.Media.Color or a parseable string). Defaults to system accent.
    .PARAMETER WatchSystemTheme
        Follow OS light/dark/high-contrast changes while the window is open; stops watching on close.
    .PARAMETER Owner
        An owning System.Windows.Window for modal parenting; forces CenterOwner startup location.
    .PARAMETER Data
        A hashtable passed as the second argument to the -Content and -Initialize blocks. On an MTA
        host this is how values cross to the UI runspace, since caller-defined functions, variables,
        and closures are not available there.
    .PARAMETER PassThru
        Return a Fluence.WindowResult object (Result and Closed) instead of the bare stashed value.
    .EXAMPLE
        Show-FluenceWindow -Content {
            param($Window, $Data)
            $stack = [System.Windows.Controls.StackPanel]::new()
            $stack.Margin = [System.Windows.Thickness]::new(24)
            $ok = [Fluence.Wpf.Controls.Button]::new()
            $ok.Content = 'OK'
            $ok.add_Click({ Close-FluenceWindow -Window $Window -Result 'ok' }.GetNewClosure())
            $null = $stack.Children.Add($ok)
            $Window.Content = $stack
        } -Title 'Hello' -Width 400 -Height 200

        Builds the window body in code and returns 'ok' when the button is clicked.
    .EXAMPLE
        $xaml = @'
        <fluence:FluenceWindow
            xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:fluence="http://schemas.fluencewpf.com"
            Title="From XAML" Width="480" Height="260">
            <fluence:Button x:Name="Ok" Content="OK" />
        </fluence:FluenceWindow>
        '@
        Show-FluenceWindow -Xaml $xaml -Initialize {
            param($Window, $Data)
            $Window.FindName('Ok').add_Click({ Close-FluenceWindow -Window $Window -Result 'done' }.GetNewClosure())
        }

        Parses a FluenceWindow from a XAML string and wires its named button in -Initialize.
    .EXAMPLE
        Show-FluenceWindow -XamlPath 'C:\ui\MainWindow.xaml' -Initialize {
            param($Window, $Data)
            $Window.FindName('AccentButton').add_Click({
                [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
            }.GetNewClosure())
        } -WatchSystemTheme

        Loads the window from a .xaml file, wires a named control, and follows OS theme changes.
    .OUTPUTS
        System.Object
    .NOTES
        Establishes or reuses a WPF Application on an STA UI thread and blocks until the window closes.
        On a multi-threaded-apartment host (for example pwsh -mta) the content and handler scriptblocks
        run on a separate module-owned UI runspace and cannot reference caller-defined functions,
        variables, or closures; pass values through -Data instead.
    #>
    [CmdletBinding(DefaultParameterSetName = 'ContentBlock')]
    [OutputType([object])]
    param
    (
        [Parameter(ParameterSetName = 'ContentBlock', Position = 0)]
        [scriptblock]$Content,

        [Parameter(Mandatory = $true, ParameterSetName = 'XamlString')]
        [string]$Xaml,

        [Parameter(Mandatory = $true, ParameterSetName = 'XamlFile')]
        [string]$XamlPath,

        [Parameter(ParameterSetName = 'XamlString')]
        [Parameter(ParameterSetName = 'XamlFile')]
        [scriptblock]$Initialize,

        [Parameter()]
        [string]$Title = 'Fluence',

        [Parameter()]
        [double]$Width,

        [Parameter()]
        [double]$Height,

        [Parameter()]
        [double]$MinWidth,

        [Parameter()]
        [double]$MinHeight,

        [Parameter()]
        [System.Windows.SizeToContent]$SizeToContent = [System.Windows.SizeToContent]::Manual,

        [Parameter()]
        [System.Windows.WindowStartupLocation]$StartupLocation = [System.Windows.WindowStartupLocation]::CenterScreen,

        [Parameter()]
        [switch]$Topmost,

        [Parameter()]
        [System.Windows.ResizeMode]$ResizeMode,

        [Parameter()]
        [ValidateSet('Default', 'DoNotRound', 'Round', 'RoundSmall')]
        [string]$CornerStyle,

        [Parameter()]
        [switch]$ExtendsContentIntoTitleBar,

        [Parameter()]
        [switch]$ShowTitle,

        [Parameter()]
        [switch]$ShowIcon,

        [Parameter()]
        [switch]$NoMinimizeButton,

        [Parameter()]
        [switch]$NoMaximizeButton,

        [Parameter()]
        [switch]$NoCloseButton,

        [Parameter()]
        [ValidateSet('Auto', 'Light', 'Dark', 'HighContrast')]
        [string]$Theme,

        [Parameter()]
        [ValidateSet('Mica', 'Acrylic', 'Tabbed', 'None', 'Auto')]
        [string]$Backdrop,

        [Parameter()]
        [System.Windows.Media.Color]$Accent,

        [Parameter()]
        [switch]$WatchSystemTheme,

        [Parameter()]
        [System.Windows.Window]$Owner,

        [Parameter()]
        [hashtable]$Data,

        [Parameter()]
        [switch]$PassThru
    )

    # Caller-thread work: collect only the chrome the caller explicitly bound, so unset properties keep
    # the window's own defaults. Switches use ContainsKey so an explicit -ShowTitle:$false is honored.
    $chromeBound = @{}
    if ($PSBoundParameters.ContainsKey('Title')) { $chromeBound['Title'] = $Title }
    if ($PSBoundParameters.ContainsKey('Width')) { $chromeBound['Width'] = $Width }
    if ($PSBoundParameters.ContainsKey('Height')) { $chromeBound['Height'] = $Height }
    if ($PSBoundParameters.ContainsKey('MinWidth')) { $chromeBound['MinWidth'] = $MinWidth }
    if ($PSBoundParameters.ContainsKey('MinHeight')) { $chromeBound['MinHeight'] = $MinHeight }
    if ($PSBoundParameters.ContainsKey('SizeToContent')) { $chromeBound['SizeToContent'] = $SizeToContent }
    if ($PSBoundParameters.ContainsKey('StartupLocation')) { $chromeBound['WindowStartupLocation'] = $StartupLocation }
    if ($PSBoundParameters.ContainsKey('ResizeMode')) { $chromeBound['ResizeMode'] = $ResizeMode }
    if ($PSBoundParameters.ContainsKey('CornerStyle')) { $chromeBound['CornerStyle'] = ConvertTo-FluenceCornerPreference -CornerStyle $CornerStyle }
    if ($PSBoundParameters.ContainsKey('Backdrop')) { $chromeBound['SystemBackdropType'] = (ConvertTo-FluenceBackdropType -Backdrop $Backdrop) }
    if ($PSBoundParameters.ContainsKey('ExtendsContentIntoTitleBar')) { $chromeBound['ExtendsContentIntoTitleBar'] = [bool]$ExtendsContentIntoTitleBar }
    if ($PSBoundParameters.ContainsKey('ShowTitle')) { $chromeBound['ShowTitle'] = [bool]$ShowTitle }
    if ($PSBoundParameters.ContainsKey('ShowIcon')) { $chromeBound['ShowIcon'] = [bool]$ShowIcon }
    if ($PSBoundParameters.ContainsKey('Topmost')) { $chromeBound['Topmost'] = [bool]$Topmost }
    if ($PSBoundParameters.ContainsKey('NoMinimizeButton') -and $NoMinimizeButton) { $chromeBound['IsMinimizeButtonVisible'] = [System.Windows.Visibility]::Collapsed }
    if ($PSBoundParameters.ContainsKey('NoMaximizeButton') -and $NoMaximizeButton) { $chromeBound['IsMaximizeButtonVisible'] = [System.Windows.Visibility]::Collapsed }
    if ($PSBoundParameters.ContainsKey('NoCloseButton') -and $NoCloseButton) { $chromeBound['IsCloseButtonVisible'] = [System.Windows.Visibility]::Collapsed }

    # A distinct local name: $accent would collide case-insensitively with the [Color] $Accent
    # parameter, and assigning to it would force the unbound struct parameter to materialize,
    # coercing null to Color and throwing.
    $accentColor = $null
    if ($PSBoundParameters.ContainsKey('Accent'))
    {
        $accentColor = $Accent
    }

    $mode = $PSCmdlet.ParameterSetName

    # Resolve the XAML file on the calling thread so a missing path surfaces a clear caller-thread
    # error before any UI work is dispatched.
    $resolved = $null
    if ($mode -eq 'XamlFile')
    {
        # -ErrorAction Stop is what makes that true: without it a missing path writes a
        # non-terminating error, $resolved stays $null, and the failure resurfaces on the UI thread
        # as an opaque "value cannot be an empty string" from the XAML reader.
        $resolved = (Resolve-Path -LiteralPath $XamlPath -ErrorAction Stop).Path
    }

    # Capture each user block's text alongside the live object. The UI thread picks one per host shape
    # (live on a shared runspace, recreated from text on the module-owned MTA runspace).
    $contentText = $null
    if ($null -ne $Content)
    {
        $contentText = $Content.ToString()
    }
    $initializeText = $null
    if ($null -ne $Initialize)
    {
        $initializeText = $Initialize.ToString()
    }

    $callerRunspaceId = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace.InstanceId

    # One-time notice on an MTA host with no host application: the UI runspace cannot see caller scope.
    if ($script:WindowMtaWarningShown -ne $true -and
        [System.Threading.Thread]::CurrentThread.GetApartmentState() -eq [System.Threading.ApartmentState]::MTA -and
        $null -eq [System.Windows.Application]::Current)
    {
        Write-Warning ('On a multi-threaded-apartment host, Show-FluenceWindow content and handler ' +
            'scriptblocks run on a separate UI runspace and cannot reference caller-defined functions, ' +
            'variables, or closures. Pass values through -Data instead.')
        $script:WindowMtaWarningShown = $true
    }

    $spec = @{
        Mode             = $mode
        Accent           = $accentColor
        ChromeBound      = $chromeBound
        Content          = $Content
        ContentText      = $contentText
        Initialize       = $Initialize
        InitializeText   = $initializeText
        Xaml             = $Xaml
        XamlPath         = $resolved
        Owner            = $Owner
        Data             = $Data
        WatchSystemTheme = [bool]$WatchSystemTheme
        CallerRunspaceId = $callerRunspaceId
    }

    # Theme and backdrop reach the spec only when the caller asked for one, so a window opened after
    # Set-FluenceTheme or Set-FluenceBackdrop keeps that state; see Initialize-FluenceApplication.
    foreach ($name in @('Theme', 'Backdrop'))
    {
        if ($PSBoundParameters.ContainsKey($name))
        {
            $spec[$name] = $PSBoundParameters[$name]
        }
    }

    $raw = Invoke-OnFluenceUi -Script {
        param($s)
        Show-FluenceWindowCore -Spec $s
    } -ArgumentList @($spec)

    # Invoke-OnFluenceUi yields the bare result hashtable (@{ Result = ... } from
    # Show-FluenceWindowCore); a $null result means the window returned nothing.
    $value = $null
    if ($null -ne $raw)
    {
        $value = $raw.Result
    }

    if ($PassThru)
    {
        return [pscustomobject]@{
            PSTypeName = 'Fluence.WindowResult'
            Result     = $value
            Closed     = $true
        }
    }
    return $value
}
