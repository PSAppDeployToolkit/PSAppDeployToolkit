# Fluence.Wpf.Tests

This folder contains the MSTest suite for the Fluence.Wpf library and demo shell. The tests target both `net472` and `net10.0-windows10.0.26100.0` and run WPF code through a shared STA dispatcher.

## What Lives Here

- `WpfTestSta.cs` - single STA-thread dispatcher used by UI-touching tests.
- `ThemeTestHelpers.cs` - application/resource setup helpers and standard theme-cycle assertions.
- `ControlTests*.cs` - control template, behavior, focus, and theme tests.
- `DemoMainWindowTests.cs` - gallery navigation, source sample, and shell behavior tests.
- `GalleryScreenshotHarness.cs` - screenshot regeneration for documentation banners during full test runs.
- `Properties/AssemblyInfo.cs` - carries `[assembly: DoNotParallelize]` so WPF resource/template work stays serial (the project also sets `<TestTfmsInParallel>false</TestTfmsInParallel>`).

## Run

From the repository root:

```powershell
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug
```

To regenerate documentation screenshots directly:

```powershell
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug --filter "FullyQualifiedName~GalleryScreenshotHarness"
```

## Maintenance Notes

Control tests should call the shared application/resource helpers before creating WPF elements. Keep tests non-parallel and route UI work through `WpfTestSta`; WPF resource dictionaries, storyboards, and template application are not safe to exercise from parallel worker threads.
