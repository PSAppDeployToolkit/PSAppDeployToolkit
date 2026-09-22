# Show-FluenceWindow

Shows a themed Fluent (FluenceWindow) window built from a content scriptblock or XAML, and returns the value the window stashed before closing.

## Syntax

```text
Show-FluenceWindow [[-Content] <scriptblock>] [-Title <string>] [-Width <double>] [-Height <double>] [-MinWidth <double>] [-MinHeight <double>] [-SizeToContent <SizeToContent>] [-StartupLocation <WindowStartupLocation>] [-Topmost] [-ResizeMode <ResizeMode>] [-CornerStyle <string>] [-ExtendsContentIntoTitleBar] [-ShowTitle] [-ShowIcon] [-NoMinimizeButton] [-NoMaximizeButton] [-NoCloseButton] [-Theme <string>] [-Backdrop <string>] [-Accent <Color>] [-WatchSystemTheme] [-Owner <Window>] [-Data <hashtable>] [-PassThru] [<CommonParameters>]
Show-FluenceWindow -Xaml <string> [-Initialize <scriptblock>] [-Title <string>] [-Width <double>] [-Height <double>] [-MinWidth <double>] [-MinHeight <double>] [-SizeToContent <SizeToContent>] [-StartupLocation <WindowStartupLocation>] [-Topmost] [-ResizeMode <ResizeMode>] [-CornerStyle <string>] [-ExtendsContentIntoTitleBar] [-ShowTitle] [-ShowIcon] [-NoMinimizeButton] [-NoMaximizeButton] [-NoCloseButton] [-Theme <string>] [-Backdrop <string>] [-Accent <Color>] [-WatchSystemTheme] [-Owner <Window>] [-Data <hashtable>] [-PassThru] [<CommonParameters>]
Show-FluenceWindow -XamlPath <string> [-Initialize <scriptblock>] [-Title <string>] [-Width <double>] [-Height <double>] [-MinWidth <double>] [-MinHeight <double>] [-SizeToContent <SizeToContent>] [-StartupLocation <WindowStartupLocation>] [-Topmost] [-ResizeMode <ResizeMode>] [-CornerStyle <string>] [-ExtendsContentIntoTitleBar] [-ShowTitle] [-ShowIcon] [-NoMinimizeButton] [-NoMaximizeButton] [-NoCloseButton] [-Theme <string>] [-Backdrop <string>] [-Accent <Color>] [-WatchSystemTheme] [-Owner <Window>] [-Data <hashtable>] [-PassThru] [<CommonParameters>]
```

## Description

Hosts a full Fluence.Wpf window from PowerShell. Three shapes are supported: a -Content scriptblock that builds the window body in code, a -Xaml string parsed at runtime, or a -XamlPath file loaded at runtime. A XAML root that is itself a Window is shown directly; any other root is hosted inside a FluenceWindow. The optional -Initialize block (XAML modes) runs against the window after it is built, so named controls can be wired with FindName and add_* handlers. Close the window with Close-FluenceWindow -Result <value> to set the return value; the value stashed on the window's Tag is returned. XAML and callbacks are trusted code: do not load XAML from an untrusted source. XamlReader does not provide a sandbox.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Content` | ScriptBlock | No |  | A scriptblock that builds the window body. It receives the FluenceWindow as the first argument and the -Data hashtable as the second, and typically sets $Window.Content and wires handlers. |
| `-Xaml` | String | Yes |  | A XAML string parsed with XamlReader.Parse. A FluenceWindow (or any Window) root is shown directly; any other root is hosted inside a new FluenceWindow. |
| `-XamlPath` | String | Yes |  | A path to a .xaml file loaded with XamlReader.Load. The path is resolved on the calling thread. |
| `-Initialize` | ScriptBlock | No |  | A scriptblock run against the window after XAML is loaded (XamlString and XamlFile only). It receives the window as the first argument and the -Data hashtable as the second. |
| `-Title` | String | No | Fluence | The window title. Default 'Fluence'. |
| `-Width` | Double | No |  | The window width in device-independent pixels. |
| `-Height` | Double | No |  | The window height in device-independent pixels. |
| `-MinWidth` | Double | No |  | The minimum window width. |
| `-MinHeight` | Double | No |  | The minimum window height. |
| `-SizeToContent` | SizeToContent | No | [System.Windows.SizeToContent]::Manual | How the window sizes to its content (Manual, Width, Height, or WidthAndHeight). Default Manual. |
| `-StartupLocation` | WindowStartupLocation | No | [System.Windows.WindowStartupLocation]::CenterScreen | Where the window first appears (Manual, CenterScreen, or CenterOwner). Default CenterScreen; forced to CenterOwner when -Owner is supplied. |
| `-Topmost` | switch | No |  | Show the window above other windows. |
| `-ResizeMode` | ResizeMode | No |  | The window resize mode (NoResize, CanMinimize, CanResize, or CanResizeWithGrip). |
| `-CornerStyle` | String | No |  | The DWM rounded-corner preference: Default, DoNotRound, Round, or RoundSmall. Values: Default, DoNotRound, Round, RoundSmall. |
| `-ExtendsContentIntoTitleBar` | switch | No |  | Extend the window content into the title bar area. |
| `-ShowTitle` | switch | No |  | Show the title text in the title bar. |
| `-ShowIcon` | switch | No |  | Show the window icon in the title bar. |
| `-NoMinimizeButton` | switch | No |  | Hide the minimize caption button. |
| `-NoMaximizeButton` | switch | No |  | Hide the maximize caption button. |
| `-NoCloseButton` | switch | No |  | Hide the close caption button. |
| `-Theme` | String | No |  | Auto, Light, Dark, or HighContrast. Omit it to keep the theme already applied to the process; the first Fluence call in a process applies Auto. Values: Auto, Light, Dark, HighContrast. |
| `-Backdrop` | String | No |  | Mica, Acrylic, Tabbed, None, or Auto. Omit it to keep the backdrop already applied to the process; the first Fluence call in a process applies Mica. Values: Mica, Acrylic, Tabbed, None, Auto. |
| `-Accent` | Color | No |  | Optional accent color (System.Windows.Media.Color or a parseable string). Defaults to system accent. |
| `-WatchSystemTheme` | switch | No |  | Follow OS light/dark/high-contrast changes while the window is open; stops watching on close. |
| `-Owner` | Window | No |  | An owning System.Windows.Window for modal parenting; forces CenterOwner startup location. |
| `-Data` | Hashtable | No |  | A hashtable passed as the second argument to the -Content and -Initialize blocks. On an MTA host this is how values cross to the UI runspace, since caller-defined functions, variables, and closures are not available there. |
| `-PassThru` | switch | No |  | Return a Fluence.WindowResult object (Result and Closed) instead of the bare stashed value. |

## Outputs

- System.Object

## Examples

### Example 1

```powershell
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
```

Builds the window body in code and returns 'ok' when the button is clicked.

### Example 2

```powershell
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
```

Parses a FluenceWindow from a XAML string and wires its named button in -Initialize.

### Example 3

```powershell
Show-FluenceWindow -XamlPath 'C:\ui\MainWindow.xaml' -Initialize {
    param($Window, $Data)
    $Window.FindName('AccentButton').add_Click({
        [Fluence.Wpf.ApplicationAccentColorManager]::ApplySystemAccent()
    }.GetNewClosure())
} -WatchSystemTheme
```

Loads the window from a .xaml file, wires a named control, and follows OS theme changes.

## Notes

Establishes or reuses a WPF Application on an STA UI thread and blocks until the window closes. On a multi-threaded-apartment host (for example pwsh -mta) the content and handler scriptblocks run on a separate module-owned UI runspace and cannot reference caller-defined functions, variables, or closures; pass values through -Data instead.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
