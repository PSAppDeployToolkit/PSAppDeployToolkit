# Fluence.Wpf.Tests

This folder contains the xunit.v3 suite for the Fluence.Wpf library and demo shell.

## What lives here

- `Infrastructure/WpfTestSta.cs` - the single STA-thread dispatcher used by every UI-touching test.
- `Infrastructure/TestApp.cs` - the single application and theme reset. `EnsureLibraryTheme` for library tests, `EnsureDemoTheme` for the demo opt-in.
- `Infrastructure/VisualTree.cs` - the tree walkers and `CloseWindowAndDrain`, brought into scope with `using static Fluence.Wpf.Tests.Infrastructure.VisualTree;`.
- `Infrastructure/BrushAssert.cs` - resolve a theme brush key and compare colours.
- `Infrastructure/LightThemeFixture.cs` - one reset and one Light apply per class, for classes that do not mutate the theme.
- `Infrastructure/ThemeTestHelpers.cs`, `DemoTestHost.cs`, `SlopwatchSuppressAttribute.cs` - theme cycle assertions, demo hosting, and the one analyzer-suppression marker.
- `Control/` - one sealed class per control.
- `Control/Rules/` - the rules asserted across many controls at once: icon foreground, focus visuals, reduced motion, background parity, accessibility names, automation peers, stroke compositing, popup corner radius, crisp rendering.
- `Theming/` - theme engine, dictionary stability, accent, markup, metrics, WinUI parity, design-time drift.
- `Windowing/` - window policy, `FluenceWindow`, title bar, caption buttons, native structs, snap layout, window icon.
- `Gallery/` and `Gallery/Pages/` - the gallery shell, the sample contracts, and one class per gallery page.
- `Tools/GalleryScreenshotHarness.cs` - documentation screenshot regeneration. Not a test.
- `Baselines/` - the committed `--list-tests` capture per TFM and the name-change allowlist.
- Namespaces match folders: `Fluence.Wpf.Tests.Infrastructure`, `.Control`, `.Control.Rules`, `.Theming`, `.Windowing`, `.Gallery`, `.Gallery.Pages`, `.Tools`. IDE0130 is an error, so a file's namespace and its folder never disagree.
- `Properties/AssemblyInfo.cs` - carries `[assembly: Parallelization(Mode = ParallelMode.None)]` so WPF resource and template work stays serial (the project also ships `xunit.runner.json` and sets `<TestTfmsInParallel>false</TestTfmsInParallel>`).

## Run

The project runs on Microsoft Testing Platform (`UseMicrosoftTestingPlatformRunner`). Run the built executable directly; the SDK 10 VSTest bridge is gone.

```powershell
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-trait "Category=Screenshots" --no-ansi --progress off
```

A single-process whole-assembly run on `net472` aborts non-deterministically, so run that TFM as two complementary lanes. Lane A is `--filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests`; lane B is `--filter-not-class` over the same seven. Sum the two case counts.

To regenerate documentation screenshots, opt in through the environment variable that gates the declaratively skipped harness facts:

```powershell
$env:FLUENCE_CAPTURE_SCREENSHOTS = "1"
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Tools.GalleryScreenshotHarness --no-ansi --progress off
```

## Maintenance notes

New classes, and everything under `Control/`, `Control/Rules/` and `Gallery/Pages/`, own the reset through `IAsyncLifetime` or `IClassFixture<LightThemeFixture>`; test bodies do not call a setup helper. A small set of `Theming/` and `Windowing/` classes, plus `Gallery/DemoResourceCleanupTests.cs` and `Gallery/DemoSampleContractTests.cs`, predate this rule and still reset in the test body; see `AGENTS.md` section 6 for the list. Keep tests non-parallel and route UI work through `WpfTestSta`; WPF resource dictionaries, storyboards, and template application are not safe to exercise from parallel worker threads.

The rule above is written in terms of a test applying a theme, changing the accent, or toggling reduced motion, but two more calls disqualify a class from the shared fixture just as surely: `TestApp.EnsureLibraryTheme(theme, backdrop)` called with a theme or backdrop argument, as `Theming/ResourceAliasTests.cs` does from a test body, and `TestApp.EnsureDemoTheme()` called at all, as every `Gallery/Pages/` class does. Either call mutates the one shared `Application`, so a class that makes one owns its own reset through `IAsyncLifetime`; `IClassFixture<LightThemeFixture>` is only for a class whose tests need one Light apply and nothing else.

Environment-dependent tests use xunit.v3 declarative skips (`[Fact(SkipUnless = ...)]` with a static condition property) rather than body-level `Assert.Skip`. Manual/maintainer-only probes (registry accent experiments, design-time resource regeneration) are marked `[Fact(Explicit = true)]` and never run in normal passes. Polling helpers honour `TestContext.Current.CancellationToken` so cancelled runs terminate promptly.
