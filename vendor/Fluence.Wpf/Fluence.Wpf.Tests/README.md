# Fluence.Wpf.Tests

This folder contains the xunit.v3 suite for the Fluence.Wpf library and demo shell.

## What Lives Here

- `WpfTestSta.cs` - single STA-thread dispatcher used by UI-touching tests.
- `ThemeTestHelpers.cs` - application/resource setup helpers and standard theme-cycle assertions.
- `ControlTests*.cs` - control template, behavior, focus, and theme tests.
- `DemoMainWindowTests.cs` - gallery navigation, source sample, and shell behavior tests.
- `GalleryScreenshotHarness.cs` - screenshot regeneration for documentation banners during full test runs.
- `Properties/AssemblyInfo.cs` - carries `[assembly: CollectionBehavior(DisableTestParallelization = true)]` so WPF resource/template work stays serial (the project also ships `xunit.runner.json` and sets `<TestTfmsInParallel>false</TestTfmsInParallel>`).

## Run

The project runs natively on Microsoft Testing Platform (`UseMicrosoftTestingPlatformRunner`), so `dotnet test` launches the test executable directly instead of going through VSTest. From the repository root:

```powershell
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug
```

To regenerate documentation screenshots, opt in via the environment variable that gates the declaratively skipped harness facts:

```powershell
$env:FLUENCE_CAPTURE_SCREENSHOTS = "1"
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug --filter "FullyQualifiedName~GalleryScreenshotHarness"
```

## Maintenance Notes

Control tests should call the shared application/resource helpers before creating WPF elements. Keep tests non-parallel and route UI work through `WpfTestSta`; WPF resource dictionaries, storyboards, and template application are not safe to exercise from parallel worker threads.

Environment-dependent tests use xunit.v3 declarative skips (`[Fact(SkipUnless = ...)]` with a static condition property) rather than body-level `Assert.Skip`. Manual/maintainer-only probes (registry accent experiments, design-time resource regeneration) are marked `[Fact(Explicit = true)]` and never run in normal passes. Polling helpers honour `TestContext.Current.CancellationToken` so cancelled runs terminate promptly.
