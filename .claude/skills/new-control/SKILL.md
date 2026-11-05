---
name: new-control
description: Scaffold a new Fluence.Wpf public control end to end, from the CLR type and template to the gallery page and its catalog registrations, the xUnit tests with baselines and CI floors, the PowerShell module (controls tour example, plus a prompt input type or public function when scripts need one), screenshots, and the docs and CHANGELOG updates. Use when adding a new control to the library.
disable-model-invocation: true
---

# New Control

Scaffold a new `Fluence.Wpf` control so it lands complete in one change. The last control added, `DataGrid`, needed three commits: review asked for the PowerShell example and more tests, and a later docs pass added the known issues and the screenshot capture entries. Every step below names a place that a new control touches. A step that does not apply still gets a sentence in the final report.

This skill orders the work and names the files. The rules themselves live in the owning guidance files. Read each one before its step instead of relying on a summary here:

| Step | Owning guidance |
| --- | --- |
| C# source, references, cross-area changes | [Fluence.Wpf/AGENTS.md](../../../Fluence.Wpf/AGENTS.md) |
| Control contract, events, window behavior | [Fluence.Wpf/Controls/AGENTS.md](../../../Fluence.Wpf/Controls/AGENTS.md) |
| Templates, color tables, design-time XAML | [Fluence.Wpf/Themes/AGENTS.md](../../../Fluence.Wpf/Themes/AGENTS.md), plus [Theming/AGENTS.md](../../../Fluence.Wpf/Theming/AGENTS.md) for a new token |
| Gallery page | [Fluence.Wpf.Demo/AGENTS.md](../../../Fluence.Wpf.Demo/AGENTS.md) and the [demo-sample-page SPEC](../demo-sample-page/SPEC.md) |
| Tests, lanes, baselines | [Fluence.Wpf.Tests/AGENTS.md](../../../Fluence.Wpf.Tests/AGENTS.md) and [Fluence.Wpf.Tests/README.md](../../../Fluence.Wpf.Tests/README.md) |
| PowerShell module | [Fluence.Wpf.PowerShell.Module/AGENTS.md](../../../Fluence.Wpf.PowerShell.Module/AGENTS.md) and [references/powershell-module.md](references/powershell-module.md) |
| Documentation | [docs/AGENTS.md](../../../docs/AGENTS.md) |

## Inputs

Ask the user, or infer from the request, before generating:

- **Control name** in PascalCase, for example `RatingControl`, and the WinUI counterpart if one exists.
- **Base type**: the closest WPF control. When the framework control is sealed, style the native type instead (see Controls/AGENTS.md).
- **Dependency properties**, and which are read-only state properties.
- **Template parts and visual state groups.**
- **Theme sensitivity**: whether the control needs new color tokens or a theme-cycle test.
- **PowerShell surface**: example only (the default), a new `New-FluencePrompt -InputType`, or a new public function. A new type or function is public module API, so confirm it with the user. [references/powershell-module.md](references/powershell-module.md) explains how to choose.

## Reference authority

Resolve visual and behavioral choices in the order given in Fluence.Wpf/AGENTS.md: in-tree precedent, then the domain authority in its table (WinUI 3 CommonStyles for tokens, states, and templates), then Microsoft guidance. If no reference covers a decision, say so and ask. Check every runtime API against .NET Framework 4.7.2; the `net472-feasibility-checker` agent does this.

## Steps

1. **CLR type** at `Fluence.Wpf/Controls/<Name>.cs`.
   - Copy the 27-line BSD header from an existing library file without changing the year.
   - Follow the control contract in Controls/AGENTS.md: `DefaultStyleKey` metadata in the static constructor, `DependencyProperty.Register` or `RegisterReadOnly`, a `PART_Name` constant with a matching `TemplatePart` attribute, `TemplateVisualState` attributes, and typed events.
   - Write XML documentation for every public member. Missing documentation fails the build.
   - Add every new public symbol to `Fluence.Wpf/PublicAPI/PublicAPI.Unshipped.txt`. The public API analyzer fails the build on a missing entry, and the analyzer's message gives the exact line to add.
   - Put each new public event-argument type in its own file.

2. **Template and resources.**
   - Create `Fluence.Wpf/Themes/Controls/<Name>.xaml` and merge it from `Themes/Generic.xaml`.
   - Follow Themes/AGENTS.md: `DynamicResource` for theme-bound values, canonical WinUI keys, no inline hex, the Fluent focus tokens, and the existing state timings.
   - Add a genuinely new color to all three `Themes/Colors/Theme.{Light|Dark|HighContrast}.xaml` tables. `BrushFactory` emits the brush twin. Use `SpecialBrushes` only for the exceptions that Themes/AGENTS.md lists.
   - A new color changes the generated `Fluence.Wpf/Properties/DesignTime.{Light,Dark}.xaml` snapshots. Do not edit them, or `DesignTimeResources.xaml`, by hand. Run the explicit `DesignTimeResourceTests.RegenerateDesignTimeResourcesAsync` test to rewrite them. `DesignTimeResources_AreCurrentAsync` fails until you do.

3. **Gallery page.** Build the page with the `demo-sample-page` skill. Then register it in `DemoNavigationCatalog`, the only gallery page list, as the SPEC's catalog integration section describes; the gallery shell and sample-contract tests pick it up from there. Add a page test. Set the page header's `DocsAnchor` to the control's heading slug in `docs/controls.md`.

4. **Tests** in `Fluence.Wpf.Tests/Control/<Name>Tests.cs`, with one sealed class.
   - Follow Fluence.Wpf.Tests/AGENTS.md for isolation, fixtures, and the shared helpers: `WpfTestSta`, `TestApp`, `VisualTree.CloseWindowAndDrain`, and `ThemeTestHelpers.ApplyStandardThemeCycle`.
   - Cover these cases at minimum. The default style applies. The template parts resolve after `Window.Show()`. The critical dependency properties and visual-state transitions work. A theme-sensitive control gets one theme cycle. To drive protected input members, use a small probe subclass, as `ClickableCardProbe` does in `Control/Rules/FluentStrokeTests.cs`.
   - Add the new cases to both baselines: `Fluence.Wpf.Tests/Baselines/baseline.<tfm>.txt` holds the `--list-tests` output, and `baseline.<tfm>.methods.txt` holds the method names, for `net10` and `net472`. Raise the `$floor` values in `.github/workflows/build.yml` by the same count. CI fails when the lanes sum below the floor.

5. **PowerShell module.** Follow [references/powershell-module.md](references/powershell-module.md). At minimum, add a card for the control to `examples/06-ControlsTour.ps1` and update `examples/README.md`. Add a prompt input type or a public function only when the user agreed to it in Inputs.

6. **Documentation and screenshots.**
   - `docs/controls.md`: add the control to the catalog, which is written to help readers choose a control.
   - `docs/winui-parity.md`: record the WinUI counterpart and any intentional differences.
   - `docs/theming.md`: document any new token family.
   - `KNOWN_ISSUES.md`: record native behavior the control keeps, and any accepted net472 gap.
   - Screenshot capture: add the control to `Fluence.Wpf.Tests/Tools/GalleryScreenshotHarness.cs`, and add Light and Dark entries to `tools/Docs/VisualCaptureMap.psd1`. Run the `tools/Docs` tests that cover the map. Published images need separate visual review, and the website repository owns them.

7. **CHANGELOG.** Add one entry to the `### Added` list under `## [Unreleased]`. Follow the existing style: past tense, user-visible behavior first, then the gallery page and the PowerShell example, and end with "These changes add N test cases on each full test target framework."

## Verify before reporting done

- `dotnet build Fluence.Wpf.sln -c Debug` finishes with zero warnings. Warnings are errors.
- Run the new test classes with `--filter-class` on `net10.0-windows10.0.26100.0` and `net472`. Then run the full lanes from Fluence.Wpf.Tests/README.md, and check that `--list-tests` matches the updated baselines.
- Run the PowerShell checks listed in references/powershell-module.md for the surface you changed.
- Run `pwsh .claude/hooks/post-tool-util.ps1 -CheckAll` and `git diff --check`.
- Run the review agents in a separate pass, because a scaffold does not review itself: `winui-parity-reviewer`, `theme-slot-auditor` when resources changed, and `net472-feasibility-checker`.
- Visual changes: check Light, Dark, and High Contrast in the gallery, with 100% and 150% DPI captures when the change is material.

## Report

List the changed files by step, the number of test cases added on each target, the commands you ran with their results, and every step you skipped or could not verify, such as the PowerShell `-IncludeUi` lane on a machine without a desktop. Show the diff and wait for the user's explicit commit instruction.
