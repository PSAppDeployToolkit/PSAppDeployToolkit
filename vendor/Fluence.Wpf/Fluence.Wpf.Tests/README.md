# Test suite

`Fluence.Wpf.Tests` is the xunit.v3 suite for the library and gallery. It targets `net472` and `net10.0-windows10.0.26100.0`. The separate `Fluence.Wpf.Tests.Smoke` project provides a .NET 8 smoke lane.

Build first from the repository root:

```powershell
dotnet build Fluence.Wpf.sln -c Debug
```

The suite uses Microsoft Testing Platform. Run its built executable directly. The screenshot harness is opt-in and excluded from ordinary runs.

```powershell
Fluence.Wpf.Tests/bin/Debug/net10.0-windows10.0.26100.0/Fluence.Wpf.Tests.exe --filter-not-trait "Category=Screenshots" --no-ansi --progress off
```

A whole-assembly `net472` process can abort. Run the two complementary lanes below; together they cover the ordinary suite:

```powershell
$exe = 'Fluence.Wpf.Tests/bin/Debug/net472/Fluence.Wpf.Tests.exe'
$classes = @(
    'Fluence.Wpf.Tests.Gallery.DemoShellTests',
    'Fluence.Wpf.Tests.Gallery.DemoSampleContractTests',
    'Fluence.Wpf.Tests.Control.NavigationViewTests',
    'Fluence.Wpf.Tests.Control.ProgressBarTests',
    'Fluence.Wpf.Tests.Control.ContentDialogTests',
    'Fluence.Wpf.Tests.Control.ColorPickerTests',
    'Fluence.Wpf.Tests.Control.TimePickerTests'
)
& $exe --filter-class $classes --filter-not-trait 'Category=Screenshots' --no-ansi --progress off
& $exe --filter-not-class $classes --filter-not-trait 'Category=Screenshots' --no-ansi --progress off
```

`Infrastructure/` provides the STA dispatcher, application setup, visual tree helpers, and theme fixtures. `Control/`, `Theming/`, `Windowing/`, and `Gallery/` group tests by behavior. `Tools/GalleryScreenshotHarness.cs` regenerates documentation screenshots when explicitly enabled.

Read [CONTRIBUTING.md](../CONTRIBUTING.md) for project gates and the [developer handbook](../AGENTS.md) for test conventions. The [documentation website](https://fluencewpf.com) is the entry point for user-facing guides.
