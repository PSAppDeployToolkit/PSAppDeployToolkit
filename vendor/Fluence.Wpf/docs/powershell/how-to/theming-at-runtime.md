# How to change theme, accent and backdrop at runtime

This guide shows how to set the theme, accent colour and window backdrop for the dialogs and windows the module shows, and how to change them while a window is open.

## Set them for one dialog

Every dialog cmdlet takes `-Theme` and `-Backdrop`; `Show-FluenceDialog`, `Show-FluenceProgress` and `Show-FluenceWindow` also take `-Accent`.

```powershell
Show-FluenceMessage -Message 'Dark, no backdrop.' -Theme Dark -Backdrop None
Show-FluenceDialog -Message 'Branded' -Accent '#C42B1C' -Prompts 'Name'
```

`-Accent` accepts a `System.Windows.Media.Color` or any string the WPF colour converter parses (`'#C42B1C'`, `'Crimson'`).

These settings are process-wide, and a dialog applies one only when you pass it. A later dialog that omits them keeps whatever was applied last, whether that came from another dialog or from `Set-FluenceTheme`, `Set-FluenceBackdrop` or `Set-FluenceAccent`. The first Fluence call in a process seeds `Auto` (follow Windows), `Mica` and the system accent.

## Change the theme between or during dialogs

`Set-FluenceTheme -Theme Auto|Light|Dark|HighContrast` re-runs the theme engine. Every open window and every later dialog picks the change up at once, because the library's brushes are `DynamicResource` bindings.

```powershell
Set-FluenceTheme -Theme Dark
Set-FluenceTheme -Theme Light -Backdrop Acrylic
```

Without `-Backdrop` the current backdrop is kept. `-UpdateAccent` is accepted and ignored; the engine always keeps the accent intent.

## Change the accent

```powershell
Set-FluenceAccent -Color '#10893E'
Set-FluenceAccent -System
```

`-Color` pins the whole accent ramp to that seed, and the pin holds until something else changes it: a later dialog resets the accent only when it carries an `-Accent` of its own; `-System` returns to the colour chosen in Windows Settings.

## Change the backdrop

```powershell
Set-FluenceBackdrop -Backdrop Acrylic
Set-FluenceBackdrop -Backdrop Mica -Window $Window
```

Without `-Window` the pipeline is re-applied so later windows get the new backdrop; pass `-Window` from inside a `Show-FluenceWindow -Initialize` block to change an open window's `SystemBackdropType` live. The window must belong to the module's UI thread; a window created elsewhere on an MTA host raises a clear error instead of a cross-thread exception.

Mica and Tabbed need Windows 11; on Windows 10 they fall back to a solid background. Acrylic uses the legacy composition attribute on Windows 10 and is disabled while the Windows "Transparency effects" setting is off.

## Follow the Windows theme while a window is open

Add `-WatchSystemTheme` to `Show-FluenceWindow`. With the theme at `Auto`, a Light or Dark change in Windows Settings re-themes the window; the watcher is removed when the window closes. Dialogs are short-lived and do not watch.

## Read the current state

`Get-FluenceTheme` returns a `Fluence.ThemeInfo` without touching the UI thread:

```powershell
$theme = Get-FluenceTheme
$theme.CurrentTheme     # what was requested: Auto, Light, Dark, HighContrast
$theme.ResolvedTheme    # what is showing: Light or Dark
$theme.IsAppInDarkMode  # $true when ResolvedTheme is Dark
$theme.CurrentBackdrop.ToString()
```

Compare `CurrentBackdrop` by name. Its enum type is `WindowBackdropType` on newer library builds and `BackdropType` on older ones, and the module works with both.

## React to theme changes in your own code

Inside `-Initialize`, subscribe to the library's `Changed` event and unsubscribe when the window closes. The `ThemeAndAccent.ps1` example uses this to swap the window icon between a light and a dark variant:

```powershell
$applyIcon = {
    $dark = [Fluence.Wpf.ApplicationThemeManager]::ResolvedTheme -eq [Fluence.Wpf.ApplicationTheme]::Dark
    $Window.Icon = if ($dark) { $darkIcon } else { $lightIcon }
}.GetNewClosure()
& $applyIcon
[Fluence.Wpf.ApplicationThemeManager]::add_Changed($applyIcon)
$Window.add_Closed({ [Fluence.Wpf.ApplicationThemeManager]::remove_Changed($applyIcon) }.GetNewClosure())
```

## Related

- [Set-FluenceTheme](../reference/Set-FluenceTheme.md), [Set-FluenceAccent](../reference/Set-FluenceAccent.md), [Set-FluenceBackdrop](../reference/Set-FluenceBackdrop.md), [Get-FluenceTheme](../reference/Get-FluenceTheme.md)
- [theming.md](../../theming.md) for the library's colour and brush catalogue
- [Explanation](../explanation.md) for what theme seeding and the dictionary slots are
- Runnable examples: `Fluence.Wpf.PowerShell.Module/examples/ThemeAndAccent.ps1`, `HelloWindow.ps1`
