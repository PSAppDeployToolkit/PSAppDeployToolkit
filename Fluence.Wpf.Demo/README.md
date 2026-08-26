# Fluence.Wpf Control Gallery (beginner guide)

This app tours every Fluence.Wpf control and doubles as a worked example of a themed WPF
desktop app. Read this first, then follow the "where to look next" pointers into the code. It
targets `net472` and `net10.0-windows10.0.26100.0`, references the library by project reference,
and is the primary manual verification surface for control behavior, theming, accent, and window
chrome.

## Run it

From the repository root:

```powershell
dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug
# add -f net472 or -f net10.0-windows10.0.26100.0 to validate a specific target framework
```

## The 60-second mental model

1. **`App.xaml.cs` -> `OnStartup`** turns the theme engine on *before* any window exists:
   `ApplicationThemeManager.Apply(ApplicationTheme.Auto, BackdropType.Mica)` then
   `ApplicationAccentColorManager.ApplySystemAccent()`. This publishes all the brushes the
   controls bind to. Then it merges the gallery's own `Resources/DemoSharedStyles.xaml` and
   shows `MainWindow`.
2. **`MainWindow`** is a `FluenceWindow` (a `Window` with Fluent chrome). It hosts a
   `NavigationView` (the left menu) and a `TitleBar` (icon, title, search box). Each menu entry
   has a `Tag`; `NavigateTo(tag)` swaps the content frame to the matching `Gallery*Page` and
   keeps a lightweight visited-page stack for the shell Back button.
3. **Each page** is a `UserControl` under `Pages/`. Most pages are a `SmoothScrollViewer` over a
   `StackPanel` of `DemoSampleControl` cards. (A few direct reference pages, such as Typography,
   render catalog content without a trailing source expander.)
4. **`DemoSampleControl`** is the reusable "sample card": a description, the live control(s), an
   optional options rail, and an expandable XAML/C# source viewer.

## How a page wires its samples (the one piece of "magic")

Named controls cannot be declared *inside* `DemoSampleControl` property elements (WPF raises
`MC3093`). So each page declares hidden `ContentControl` "slots" named
`DemoSampleSlotNNDemoContentHost` / `...OutputContentHost` / `...RightRailContentHost` (NN =
01-based sample index). In the page constructor, `DemoSamplePageWiring.Apply(...)` moves each
slot's content into the matching `DemoSampleControl` and attaches the source-code strings. See
`Pages/DemoSamplePageWiring.cs` for the full contract and `Pages/GalleryButtonsPage.xaml(.cs)`
for a complete example.

## Theming at design time

`Properties/DesignTimeResources.xaml` merges the library's `DesignTime.Light.xaml` (the complete
computed colors + brushes for the default `#0078D4` accent) so the XAML designer renders controls
correctly. It is design-time only and never merged at runtime.

## Where to look next

| You want to... | Open |
| --- | --- |
| See app startup + theme setup | `App.xaml.cs` |
| See the shell (window, nav, search) | `MainWindow.xaml` / `MainWindow.xaml.cs` |
| See the sample-card control | `Pages/DemoSampleControl.xaml(.cs)` |
| See a typical page | `Pages/GalleryButtonsPage.xaml(.cs)` |
| See the navigation catalog | `DemoNavigationCatalog.cs` |
| See shared spacing/styles | `Resources/DemoSharedStyles.xaml` |

## Using the gallery to verify changes

After a visual or interaction change, exercise Light, Dark, High Contrast, an accent swap,
Mica/Acrylic/Tabbed/None backdrops, keyboard focus, and at least one control per page.

## Maintenance notes

The gallery intentionally owns navigation through `NavigationView` selection and
`DemoNavigationCatalog` metadata; `MainWindow` maps each route to a concrete
`Pages/Gallery*Page.xaml` control and keeps only the lightweight Back history. `DemoSampleControl`
owns the reusable sample chrome, source tabs, and copy action; `DemoSamplePageWiring` owns
page-local slot discovery, content transfer, and typed `DemoSampleSource` registration.
