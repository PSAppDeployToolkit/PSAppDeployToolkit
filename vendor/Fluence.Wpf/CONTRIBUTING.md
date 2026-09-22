# Contributing

Thanks for helping improve Fluence.Wpf, a Windows 11 Fluent / WinUI 3 control library for WPF on .NET Framework 4.7.2, .NET 8, and .NET 10.

This file is the contributor guide. The full engineering handbook (conventions, theme architecture, reference authority, and quality gates) is [AGENTS.md](AGENTS.md); read it before a non-trivial change. For AI-assisted work, read [AGENTS.md](AGENTS.md) first.

## Build and test

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build   Fluence.Wpf.sln -c Debug
```

The suite runs on Microsoft Testing Platform, so run the built executable rather than `dotnet test` (the SDK 10 VSTest bridge is gone). A single-process whole-assembly run on `net472` aborts non-deterministically, so `net472` runs locally as two complementary lanes whose union is the whole assembly. `net10.0-windows10.0.26100.0` does not abort and runs locally as one combined invocation; CI additionally splits it into the same two lanes, purely so the two per-TFM case counts can be summed and checked. See AGENTS.md section 6 and `KNOWN_ISSUES.md`.

```powershell
# net472 runs as two complementary lanes because the whole-assembly run aborts; their union is the whole assembly.
# net10.0-windows10.0.26100.0 does not abort, so a local run uses one combined invocation.
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net472\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-trait "Category=Screenshots" --no-ansi --progress off
```

```powershell
# A single class or method while iterating.
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Control.ButtonTests
Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-method "*Button_Rest*"
```

Both target frameworks (`net472` and `net10.0-windows10.0.26100.0`) must build and test green. The library builds with `TreatWarningsAsErrors=true`, `WarningLevel=9999`, and `AnalysisLevel=latest-all`: fix warnings at the root cause, do not suppress them. `string.IsNullOrEmpty()` is banned (use `string.IsNullOrWhiteSpace()`); public API needs `///` XML docs or the build fails.

## PowerShell module

The `Fluence.Wpf.PowerShell` script module under `Fluence.Wpf.PowerShell.Module/` is not a project in the solution and has its own gate. Stage the Release assemblies the solution build produced, then run PSScriptAnalyzer plus the Pester logic lane on both PowerShell editions:

```powershell
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1 -Configuration Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
pwsh -NoProfile -MTA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -SkipAnalyzer
powershell.exe -NoProfile -STA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
```

Install the exact Pester and PSScriptAnalyzer versions listed in [docs/release.md](docs/release.md) before running the gate. CI runs the PowerShell checks and module packaging in a separate job after consuming the library artifacts.

The render lane (`-IncludeUi`) opens real windows and runs locally only; [docs/release.md](docs/release.md) lists it with the expected case counts per lane. The module follows PSADT PowerShell conventions, not the C# ones above: 5.1 and 7 compatible syntax, one function per file with comment-based help and `[OutputType()]`, fully qualified .NET type names, Allman braces, UTF-8 with BOM. Cmdlet reference pages under `docs/powershell/reference/` are generated from the comment-based help, so change the help in the function and re-run `build/Export-ModuleReference.ps1` rather than editing the page.

## Formatting and text policy

XAML style (4-space indent, UTF-8 with BOM, LF line endings, final newline) is governed by `.editorconfig`, applied to `.xaml` like every other source file; there is no separate XAML formatter tool. Encoding and text policy are enforced by a repo hook and in CI:

```powershell
pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll   # CI gate: UTF-8 BOM, LF, banned APIs, hard-coded hex, em/en dashes
```

All source and text files are UTF-8 with BOM and LF; `string.IsNullOrEmpty()` is banned (use `string.IsNullOrWhiteSpace()`); do not inline hex colors in `Themes/Controls/**` or use em/en dashes in `.cs` / `.md`. Generated XAML (`Properties/DesignTime.*.xaml`) is excluded from the repo-wide check.

## Adding or changing a control

Subclass the closest WPF type, override `DefaultStyleKeyProperty` metadata in the static constructor, put the template in `Fluence.Wpf/Themes/Controls/<ControlName>.xaml` and merge it from `Themes/Generic.xaml`, mark template parts with `[TemplatePart]` and visual states with `[TemplateVisualState]`, reuse canonical WinUI resource keys through `DynamicResource`, add a design-time entry in `Fluence.Wpf/Properties/DesignTimeResources.xaml`, extend the gallery demo, and add xUnit test coverage. [AGENTS.md](AGENTS.md) section 5 is the full checklist.

## Reference authority

Every visual or behavioral decision must be grounded, in this order (see [AGENTS.md](AGENTS.md) Section 4):

1. In-tree precedent (existing XAML, controls, tests).
2. Per-domain authority - WinUI 3 CommonStyles for visual tokens and control templates; .NET 10 WPF Themes for WPF-native chrome, accent ramp math, theme detection, and backdrops.
3. Published Windows 11 design guidance on Microsoft Learn (tie-breaker only).

"Looks right" is not an acceptable justification.

## Tests

- One sealed class per subject, not a partial. Test files live under `Fluence.Wpf.Tests/` in `Control/`, `Control/Rules/`, `Theming/`, `Windowing/`, `Gallery/`, `Gallery/Pages/`, `Infrastructure/` and `Tools/`, each in the namespace that matches its folder. `Control/Rules/FluentStrokeTests.cs` is the reference pattern for a small template or behaviour probe.
- A class whose tests touch `Application`, application resources or a `Window` implements `IAsyncLifetime` and calls `TestApp.EnsureLibraryTheme()` from `InitializeAsync` through `WpfTestSta.RunOnStaAsync`. Do not merge the demo dictionary into a library control test; `TestApp.EnsureDemoTheme()` is the explicit opt-in and belongs to the `Gallery/` tests. Pure logic classes stay free of `IAsyncLifetime`.
- Cover at minimum: default style applies, key template parts resolve, critical dependency property and state transitions, and one theme cycle (`ThemeTestHelpers.ApplyStandardThemeCycle`) for theme-sensitive controls.
- The HEAD-of-branch test count is the floor. Add tests; do not weaken the baseline. If a test is legitimately obsoleted, remove the whole file in the same change and record the rationale in `CHANGELOG.md`.

## Visual verification

Run `Fluence.Wpf.Demo` and exercise Light / Dark / High Contrast / Auto, a couple of accent swatches, Mica / Acrylic / Tabbed / None backdrops, and at least one control per gallery page. Capture 100% and 150% DPI screenshots when visuals change materially.

## Documentation

- Public guides live in `docs/*.md`. Prefer relative links such as `[text](theming.md)` so they resolve on GitHub and in any future generated site.
- There is no hosted documentation site yet. The Markdown under `docs/` and at the repository root is the documentation; a site is a roadmap item.

## Pull requests

- Keep changes focused; no unrelated refactors or renames.
- Update [CHANGELOG.md](CHANGELOG.md) under **Unreleased** (Keep a Changelog format, SemVer).
- Update public docs ([README.md](README.md), [docs/controls.md](docs/controls.md), [docs/theming.md](docs/theming.md)) when consumer-visible behavior changes.
- The PR template encodes the build / test / visual / docs gates; fill it in.
- Maintainers follow [docs/release.md](docs/release.md) when publishing a package or tagging a release.
