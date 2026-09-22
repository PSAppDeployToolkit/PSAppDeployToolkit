# How to host a window from XAML

This guide shows how to show a full, themed `FluenceWindow` whose layout you write in XAML, wire its controls from PowerShell, keep state between clicks, and return a value when it closes.

Treat XAML and callbacks as trusted code. `XamlReader` constructs executable objects and is not a sandbox; load only XAML you trust.

## Show a window from a XAML string

Pass the XAML to `Show-FluenceWindow -Xaml`. Declare the Fluence namespace as `xmlns:fluence="http://schemas.fluencewpf.com"` and use `fluence:FluenceWindow` as the root. The call blocks until the window closes.

```powershell
$xaml = @'
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    Title="Hello" Width="520" Height="340" SystemBackdropType="Mica">
    <StackPanel VerticalAlignment="Center" HorizontalAlignment="Center">
        <TextBlock x:Name="HelloLabel" Text="Hello, World!" fluence:TextBlockExtensions.Typography="Title" Foreground="{DynamicResource TextFillColorPrimaryBrush}" />
        <fluence:Button x:Name="CloseButton" Content="Close" Appearance="Accent" Margin="0,16,0,0" HorizontalAlignment="Center" />
    </StackPanel>
</fluence:FluenceWindow>
'@

Show-FluenceWindow -Xaml $xaml -Initialize {
    param($Window, $Data)
    $Window.FindName('CloseButton').add_Click({ Close-FluenceWindow -Window $Window }.GetNewClosure())
}
```

Use `DynamicResource` for every theme brush so the window follows theme and accent changes. The brush and typography names are the WinUI ones listed in [theming.md](../../theming.md).

If the XAML root is not a `Window` (a `Grid`, say), the module hosts it inside a new `FluenceWindow` and the `-Title`, `-Width`, `-Height` and chrome switches on the cmdlet apply to that window.

## Load the XAML from a file

Keep the layout in a `.xaml` file and pass its path to `-XamlPath`. The file must be a complete document with the namespace declarations above.

```powershell
Show-FluenceWindow -XamlPath (Join-Path $PSScriptRoot 'MainWindow.xaml') -WatchSystemTheme -Initialize {
    param($Window, $Data)
    # wire controls here
}
```

`Fluence.Wpf.PowerShell.Module/examples/LoadXamlFile.ps1` and its `MainWindow.xaml` are a complete example.

## Wire controls

Do all wiring inside `-Initialize`. It receives the window as `$Window` and the `-Data` hashtable as `$Data`. Find controls by their `x:Name` and subscribe to events with `add_<Event>`.

```powershell
-Initialize {
    param($Window, $Data)
    $status = $Window.FindName('StatusBar')
    $Window.FindName('ToggleSwitch').add_Checked({ $status.Message = 'On' }.GetNewClosure())
    $Window.FindName('ToggleSwitch').add_Unchecked({ $status.Message = 'Off' }.GetNewClosure())
}
```

Add `param($s, $e)` inside a handler when you need the sender or the event arguments:

```powershell
$Window.FindName('ThemeComboBox').add_SelectionChanged({
    param($s, $e)
    Set-FluenceTheme -Theme $s.SelectedItem.Tag
}.GetNewClosure())
```

## Keep state between clicks

Pass a hashtable to `-Data` and mutate it from handlers. Do not rely on `$script:` variables: on an MTA host (`pwsh -MTA`, or a runspace without an STA thread) the handlers run on the module's UI runspace, where the caller's variables and functions do not exist. The cmdlet writes that warning once per session when it detects an MTA host with no host application, so the first `Show-FluenceWindow` on `pwsh -MTA` says it before the window opens.

```powershell
Show-FluenceWindow -Xaml $xaml -Data @{ Tick = 0 } -Initialize {
    param($Window, $Data)
    $label = $Window.FindName('CountLabel')
    $Window.FindName('CountButton').add_Click({
        $Data.Tick++
        $label.Text = "Clicked $($Data.Tick) times"
    }.GetNewClosure())
}
```

Call `.GetNewClosure()` on every handler that reads a local of `-Initialize` (`$label`, `$Data`, `$Window`), so the values are captured when the handler is created.

## Return a value from the window

Call `Close-FluenceWindow -Window $Window -Result <value>` from a handler. `Show-FluenceWindow` returns that value; with `-PassThru` it returns a `Fluence.WindowResult` with `Result` and `Closed` instead.

```powershell
$choice = Show-FluenceWindow -Xaml $xaml -Initialize {
    param($Window, $Data)
    $Window.FindName('SaveButton').add_Click({ Close-FluenceWindow -Window $Window -Result 'Saved' }.GetNewClosure())
    $Window.FindName('DiscardButton').add_Click({ Close-FluenceWindow -Window $Window -Result 'Discarded' }.GetNewClosure())
}
```

Closing with the X returns `$null`.

## Follow the Windows theme while the window is open

Add `-WatchSystemTheme`. With the theme at `Auto`, the window re-themes when the user switches Light and Dark in Windows Settings, and stops watching when it closes.

## Shape the window chrome

The cmdlet exposes the `FluenceWindow` chrome as switches: `-ExtendsContentIntoTitleBar`, `-ShowTitle`, `-ShowIcon`, `-NoMinimizeButton`, `-NoMaximizeButton`, `-NoCloseButton`, `-CornerStyle` (`Default`, `DoNotRound`, `Round`, `RoundSmall`), `-ResizeMode`, `-SizeToContent`, `-StartupLocation`, `-Topmost` and `-Owner`. They apply to a hosted non-window root and to a `FluenceWindow` root alike. `-Owner` parents the window only when the owner lives on the Fluence UI thread; an owner created on another thread (the usual case on an MTA host) draws a warning and the window is shown unparented rather than failing the call. `Show-FluenceDialog -ParentWindow` behaves the same way.

```powershell
Show-FluenceWindow -Xaml $xaml -NoMaximizeButton -ResizeMode NoResize -CornerStyle RoundSmall -Topmost
```

## Build the window in code instead of XAML

Pass a `-Content` scriptblock. It receives the window and `-Data`, sets `$Window.Content` and wires handlers; the cmdlet's chrome switches shape the window.

```powershell
Show-FluenceWindow -Title 'Built in code' -Width 400 -Height 200 -SizeToContent Height -Content {
    param($Window, $Data)
    $button = [Fluence.Wpf.Controls.Button]::new()
    $button.Content = 'Close'
    $button.Margin = [System.Windows.Thickness]::new(24)
    $button.add_Click({ Close-FluenceWindow -Window $Window }.GetNewClosure())
    $Window.Content = $button
}
```

## Related

- [Show-FluenceWindow](../reference/Show-FluenceWindow.md), [Close-FluenceWindow](../reference/Close-FluenceWindow.md)
- [Change theme, accent and backdrop at runtime](theming-at-runtime.md) for `Set-FluenceBackdrop -Window`
- [Explanation](../explanation.md) for why handlers run on a different runspace on MTA hosts
- Runnable examples: `Fluence.Wpf.PowerShell.Module/examples/HelloWindow.ps1`, `ThemeAndAccent.ps1`, `ControlsTour.ps1`, `LoadXamlFile.ps1`
