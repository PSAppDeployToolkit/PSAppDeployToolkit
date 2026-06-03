---
title: Getting started
linkTitle: Getting started
description: Reference Fluence.Wpf, initialize the theme manager, and host a FluenceWindow shell.
weight: 10
---

## Reference the library

1. Add a **project reference** to `Fluence.Wpf/Fluence.Wpf.csproj` (recommended until a NuGet package is published):

    ```xml
    <ItemGroup>
    <ProjectReference Include="path/to/Fluence.Wpf/Fluence.Wpf.csproj" />
    </ItemGroup>
    ```

2. Optional: produce a local NuGet package for feed or `PackageReference` consumption:

    ```powershell
    dotnet pack Fluence.Wpf/Fluence.Wpf.csproj -c Release -o ./artifacts
    ```

The package id is **`Fluence.Wpf`**. Public feed publishing is a release decision; a local package is handy for smoke-testing before then.

## Initialize theme and accent

Call **before** showing your main window (typically in `App.OnStartup` or equivalent):

```csharp
Fluence.Wpf.ApplicationThemeManager.Apply(
    Fluence.Wpf.ApplicationTheme.Auto,
    Fluence.Wpf.BackdropType.Mica,
    updateAccent: true);
Fluence.Wpf.ApplicationAccentColorManager.ApplySystemAccent();
```

The first `Apply` seeds the resource stack in a fixed three-slot order. Later calls rebuild and replace the computed colors and brushes at slot `[0]`, so every `DynamicResource` binding re-resolves. See [theming.md](theming.md) for the full slot layout.

## Use a Fluent window shell

Derive your main window from `Fluence.Wpf.Controls.FluenceWindow`, **or** call `ApplicationThemeManager.Apply(...)` at startup and use Fluence controls inside a standard WPF `Window`. Do not manually merge `Themes/Generic.xaml` in `App.xaml`; the theme manager adds it at slot `[2]` on the first `Apply` call.

```xml
<fluence:FluenceWindow
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:fluence="http://schemas.fluencewpf.com"
    x:Class="YourApp.Shell"
    Title="Your App"
    Width="1200" Height="760"
    SystemBackdropType="Mica"
    ExtendsContentIntoTitleBar="True">

    <!-- Custom content in the extended title bar (search, breadcrumbs, etc.) -->
    <fluence:FluenceWindow.TitleBar>
        <fluence:TextBox PlaceholderText="Search..." Width="320" />
    </fluence:FluenceWindow.TitleBar>

    <fluence:NavigationView PaneDisplayMode="Left" IsPaneOpen="True">
        <fluence:NavigationViewItem Content="Home" />
        <fluence:NavigationViewItem Content="Settings" />
    </fluence:NavigationView>
</fluence:FluenceWindow>
```

## Clickable cards

`Fluence.Wpf.Controls.Card` is a `ContentControl`. Opt into press/release semantics by setting `IsClickable="True"` and handling the `Click` routed event:

```xml
<fluence:Card IsClickable="True" Click="OnCardClicked" Padding="16">
    <StackPanel>
        <TextBlock Text="Title bar and window controls" FontSize="18" FontWeight="SemiBold" />
        <TextBlock Text="Backdrop, caption, extended title bar"
                   Foreground="{DynamicResource TextFillColorSecondaryBrush}" />
    </StackPanel>
</fluence:Card>
```

## Optional: react to OS theme changes

For a given `Window` instance:

```csharp
Fluence.Wpf.SystemThemeWatcher.Watch(myWindow);
Fluence.Wpf.ApplicationThemeManager.Changed += (s, e) => { /* refresh theme-specific assets */ };

// On exit or when disabling:
Fluence.Wpf.SystemThemeWatcher.UnWatch(myWindow);
```

`ApplicationThemeManager.Changed` fires once per applied theme change. Use it to swap theme-specific image assets; `GalleryHomePage.xaml.cs` in the demo shows the banner swap pattern.

## Verify locally

- Run tests: `dotnet test Fluence.Wpf.sln`
- Run the gallery: `dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj`
- Run the MVVM demo: `dotnet run --project Fluence.Wpf.Demo.Mvvm/Fluence.Wpf.Demo.Mvvm.csproj`

## Using from PowerShell

Fluence.Wpf can theme a WPF window from a plain Windows PowerShell 5.1 script, with no project and no compile step of your own. See [powershell.md](powershell.md) for the bootstrap pattern and the runnable examples under `Fluence.Wpf.Demo.PowerShell/`.

Next: [theming.md](theming.md) for dictionary order and pitfalls, [controls.md](controls.md) for the control inventory and XAML snippets.
