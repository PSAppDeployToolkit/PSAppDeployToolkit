---
name: new-control
description: Scaffold a new Fluence.Wpf custom control end to end against the AGENTS.md section 5 control authoring checklist. Use when adding a new control to the library. Produces the CLR type with dependency-property boilerplate, the Themes/Controls template wired into Generic.xaml, design-time and demo entries, an MSTest partial using the STA harness, and the docs/CHANGELOG updates.
disable-model-invocation: true
---

# New Control

Scaffold a new `Fluence.Wpf` control so it starts conformant instead of being retrofitted. This skill encodes AGENTS.md section 5 (Control authoring checklist). Follow every step; do not skip tests or docs.

## Inputs

Ask the user (or infer from the request) before generating:
- **Control name** (PascalCase, for example `RatingControl`).
- **Base type** (`Control`, `ContentControl`, or the closest `System.Windows.Controls.*`).
- **Dependency properties** and which are read-only state DPs (`IsPressed`, `IsValid`).
- **Visual state groups** needed (`CommonStates`, `FocusStates`, `CheckStates`, ...).
- Whether it is **theme-sensitive** (needs a theme-cycle test).

## Reference authority

Resolve every visual or behavioural decision through AGENTS.md section 4: in-tree precedent first, then WinUI 3 CommonStyles for tokens/states/visuals, then .NET 10 WPF Themes for WPF-native chrome, then Microsoft Learn as a tie-breaker. Do not invent Fluent semantics. Verify any runtime API is available on `net472` (section 4.3).

## Steps

1. **CLR type** at `Fluence.Wpf/Controls/<Name>.cs`.
   - Copy the 27-line BSD header verbatim from an existing library file. Do not change the year.
   - Subclass the chosen base. In the static constructor: `DefaultStyleKeyProperty.OverrideMetadata(typeof(<Name>), new FrameworkPropertyMetadata(typeof(<Name>)));`.
   - Register dependency properties with CLR wrappers and `OnFooChanged` callbacks where relevant. Use `RegisterReadOnly` for state-only DPs, with the `...PropertyKey` private field plus public `...Property = ...PropertyKey.DependencyProperty`.
   - Annotate template parts: `const string PART_Whatever = "PART_Whatever";` plus `[TemplatePart(Name = PART_..., Type = typeof(T))]`, and wire them in `OnApplyTemplate`.
   - Annotate visual states: `[TemplateVisualState(GroupName = "CommonStates", Name = "Normal|PointerOver|Pressed|Disabled")]`.
   - Nullable-clean, explicit types over `var`, target-typed `new()`, discard ignored returns with `_`, `??` throw expressions. XML `///` docs on every public member (the build fails on missing docs).
   - Never use `string.IsNullOrEmpty` (use `string.IsNullOrWhiteSpace`). Wrap any Win32 bit-mask math in `unchecked { }`.

2. **Template** at `Fluence.Wpf/Themes/Controls/<Name>.xaml`.
   - Standalone `ResourceDictionary`; merge it from `Themes/Generic.xaml`.
   - Use `DynamicResource` for every brush, color, corner radius, or typography value that reacts to theme, accent, or high contrast. `StaticResource` only for immutable assets. Never inline hard-coded hex; bind canonical WinUI key names.
   - Wire `VisualStateManager` groups with Fluent timings (~100 to 167 ms), reusing existing easing key splines.
   - Default WPF focus rectangles off; use the FluentControl focus brush tokens as in the Button / Card templates.

3. **Resources.** Reuse canonical keys. If a concept is genuinely new, add the **color** to all three `Themes/Colors/Theme.{Light|Dark|HighContrast}.xaml` tables; `BrushFactory` auto-emits a frozen `SolidColorBrush` twin (`key + "Brush"`) for every color key, so a plain color needs no hand-written brush. Only touch `SpecialBrushes.cs` when the brush needs a non-standard twin name, a gradient, or a high-contrast override (HC brushes are rebuilt there from live `SystemColors`, with no promotion list). Add a design-time preview entry to `Fluence.Wpf/Properties/DesignTimeResources.xaml` assuming Light + `#0078D4`.

4. **Demo.** Add or extend a gallery page under `Fluence.Wpf.Demo/Pages/Gallery*.xaml`, following the section 14 DemoSampleControl contract (use the `demo-sample-page` skill if it is a discrete sample). Register the page in `MainWindow.NavigateTo(string tag)` if it should be navigable.

5. **Tests (mandatory)** in a partial `Fluence.Wpf.Tests/ControlTests.<Area>.cs`.
   - Copy the BSD header. Use `RunOnStaThread` / `WpfTestSta.Invoke`, `EnsureApplication`, and start the test body with `MergeGenericDictionary(Application.Current.Resources)`.
   - Cover at minimum: default style applies, key template parts are found after `Window.Show()`, critical DP and visual-state transitions, and (if theme-sensitive) one cycle via `ThemeTestHelpers.ApplyStandardThemeCycle`. Drive protected `OnMouse*`/`OnKey*` members via a small probe subclass if needed (see `ClickableCardProbe`). Drain with `DrainDispatcher()` and close the window.
   - Do not weaken the HEAD-of-branch test count.

6. **Docs.** Append the control to `docs/controls.md`, note any new brush family in `docs/theming.md`, and add a one-line entry under the current CHANGELOG section.

## Verify before reporting done

- `dotnet build Fluence.Wpf.sln -c Debug`: zero errors, zero warnings (TreatWarningsAsErrors) on both TFMs.
- `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build` first (fast), then `-f net472 --no-build`. Both green; net count up by the tests you added.
- All `.cs` and `.xaml` saved UTF-8 with BOM, LF line endings.
- Stage changes, show diffs, and wait for the user's explicit commit instruction.
