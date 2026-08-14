# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed

- Tests: `Fluence.Wpf.Tests` migrated from MSTest 4.x to xunit.v3 3.2.2 (with `xunit.runner.visualstudio` 3.1.5). Fixtures use constructor/`IDisposable` lifecycle instead of `[TestInitialize]`/`[TestCleanup]`, diagnostic probes log through `ITestOutputHelper` instead of `TestContext`, and the non-parallel execution model is preserved via `xunit.runner.json` and an assembly-level `CollectionBehavior(DisableTestParallelization = true)`. The shared `WpfTestSta` STA dispatcher harness is unchanged. Both `net472` and `net10.0-windows10.0.26100.0` pass the full suite.
- Tests: the xunit.v3 port now leverages v3-native features. Repeated per-theme and per-accent `[Fact]` families collapsed into `[Theory]` rows (`ThemeManagerTests`, `ThemeEngineUnitTests`, `AccentRampTests`); environment-gated tests use declarative `SkipUnless` conditions instead of body-level `Assert.Skip` (`AccentRampTests`, `GalleryScreenshotHarness`), letting the `xUnit1004` suppression be removed; manual/maintainer probes are `[Fact(Explicit = true)]`; the shared `WaitUntil` polling helper honours `TestContext.Current.CancellationToken`; and the project runs natively on Microsoft Testing Platform (`UseMicrosoftTestingPlatformRunner` + `TestingPlatformDotnetTestSupport`).
- Tests: nullable `as`-cast + `Assert.NotNull` pairs (493 sites) replaced with `Assert.IsType<T>(...)` / `Assert.IsAssignableFrom<T>(...)` assignments, so variables are declared non-nullable and CodeQL's "may be null at this access" findings on the old pattern are resolved. `IsAssignableFrom` is used where the runtime type is a subtype of the declared target (base classes, interfaces, and the Fluence `TextBox` template parts).

## [0.8.13-Preview] - 2026-08-13

### Added

- `{fluence:ThemeResource Key}` markup extension (`Fluence.Wpf.Markup.ThemeResourceExtension`) - WinUI 3 markup parity for theme-reactive resource references; derives from `DynamicResourceExtension` and works everywhere it does, including `Setter.Value`.
- `Fluence.Wpf.Markup.ThemeDictionary` - the WinUI `ResourceDictionary.ThemeDictionaries` equivalent: per-theme `ThemeResourceDictionary` tables (`ThemeKey` of `Light` / `Dark` / `HighContrast` / `Default`, plus the WinUI high-contrast polarity keys `HighContrastBlack` / `HighContrastWhite` selected by live system window luminance) that swap automatically on theme changes; usable from XAML and code, weakly tracked so discarded instances never leak. Demonstrated on the gallery Colors page.
- `ApplicationAccentColorManager.ApplyCustomAccent(Color light, Color dark)` - sticky per-theme accent seeds resolved inside the engine on every apply; high contrast follows the dark seed. Consumers that previously re-applied a custom accent from a `Changed` handler can delete the subscription and the duplicate palette rebuild it caused.

### Changed

- Demo: the home page hero lockup swap is fully declarative through a page-scoped `ThemeDictionary`; the `HighContrastBlack` / `HighContrastWhite` tables replace the code-behind luminance pick, and the `ApplicationThemeManager.Changed` subscription is gone.
- Demo: `Card` samples set `Variant` with plain enum names (`Variant="Outlined"`) instead of `x:Static` references.
- Light theme: `NavigationViewContentBackground` raised from 50% to 65% white so the content area reads slightly more solid over Mica.
- Demo: both demo apps consume Fluence through the single `xmlns:fluence="http://schemas.fluencewpf.com"` declaration instead of separate `ui` / `uicore` clr-namespace prefixes, and the displayed sample snippets teach the same form. Demo-local namespaces (converters, view models, pages) keep `clr-namespace` - WPF cannot resolve a URI namespace for types in the assembly being compiled.
- Demo: sample pages build their displayed XAML source through `DemoSampleXaml.UserControl`, so the canonical root element and xmlns preamble live in one place instead of sixty-five hand-written copies.
- Demo: monospace sample text unified behind the `DemoMonospaceFontFamily` token (Cascadia Mono with Consolas fallback) and the Colors page's inline size, margin, and font literals moved to shared demo tokens and the new `DemoCodeSampleTextStyle`; the source-code viewers pick up Cascadia Mono where installed.
- Build: analyzer and test packages refreshed (Meziantou.Analyzer 3.0.150, Microsoft.Extensions.StaticAnalysis 10.9.0, Roslynator.Analyzers 4.16.0, Meziantou.Polyfill 1.0.159, Microsoft.NET.Test.Sdk 18.8.1). The Meziantou.Polyfill opt-in allowlist is centralized in `Directory.Build.props`, and the former net472 index/range (`IDE0056` / `IDE0057`) and string-comparison (`CA1307` / `CA1310` / `CA1847` / `CA1866`) suppressions are removed now that the polyfills cover them.

## [0.8.12-Preview] - 2026-08-09

### Added

- `Fluence.Wpf.Controls.Image` - a Fluent image presenter with a theme-aware 1px card stroke and rounded-corner clip, an `ImageAutomationPeer`, and a gallery sample. Decorative images stay out of the UI Automation tree until given an accessible name, matching WinUI.

### Changed

- `ColorPicker`, the demo shell, and the demo tests fully qualify `System.Windows.Controls.Image` references now that `Fluence.Wpf.Controls.Image` occupies the simple name. Behavior unchanged.

### Fixed

- `ProgressBar`: an indeterminate bar keeps animating with the Windows reduced-motion setting off, since motion is its only status signal; a determinate bar still honours the setting. Fixes dialogs that looked hung for screen-reader users.
- `PasswordBox` forwards its accessible name and `AutomationProperties.LabeledBy` to the inner focusable password and reveal fields, so screen readers announce the caller's prompt instead of a bare protected edit field.

## [0.8.11-Preview] - 2026-07-14

### Fixed

- Tests: `TextBox_TextViewAlignsWithPlaceholder_WhenIconIsShown` asserts placeholder and caret-host alignment to the nearest device pixel instead of a fixed 0.5 DIP, fixing false failures on fractional DPI scales.

## [0.8.10-Preview] - 2026-07-14

### Added

- CI: pushing a `v*` tag creates the GitHub release automatically, attaching the per-TFM library binaries, the demo app, and the NuGet package (`-pre` tags marked prerelease).
- Controls honour the Windows "Show animations in Windows" setting (`SystemParameters.ClientAreaAnimation`): with it off, code-driven animations jump to their final state across ProgressRing/ProgressBar, FontIcon spin, ContentDialog, NavigationView, Flyout, TeachingTip, ComboBox, Expander, ToggleSwitch, ListView, and SmoothScrollViewer.

### Changed

- Demo: the home page hero shows the theme-aware Fluence header lockup, replacing the square brand mark and separate title text.
- `ScrollBar`: hover expand/contract now runs 167 ms and the track fade 83 ms, matching WinUI durations.
- `ListView`: item removal eases out like the insert path instead of lingering and snapping away.
- `ComboBox`: the dropdown opens with the standard 8 px slide-plus-fade reveal over 167 ms instead of unfolding from zero height; honors reduced motion.
- `Flyout`: the open reveal slides 8 px in from the requested placement side instead of always sliding down.
- `TeachingTip`: the reveal slides 8 px in from the resolved placement side; untargeted and Center tips fade only.
- `ContentDialog`: closing plays the WinUI exit (167 ms scale, 83 ms fade) instead of a hard cut; `ShowAsync` completes when the exit finishes.
- `ToolTip`: fades in over 83 ms (WinUI parity); the OS tooltip popup fade is suppressed so only one fade plays.
- `CheckBox`: checking animates the glyph with a 100 ms fade and a 167 ms scale settle; unchecking stays instant.
- Demo: page navigation keeps just the 167 ms fade, and the color-swatch hover scale drops to 1.05 over 100 ms.
- `ProgressBar`: determinate value changes animate the fill's `ScaleTransform` instead of layout `Width`, making each frame composite-only.
- `ToggleSwitch`: the thumb grow/shrink animates a render-thread `ScaleTransform` instead of layout size; sizes, timing, and easing unchanged.
- `Expander`: expand and collapse slide the content behind its clip by the measured height (WinUI parity); a mid-flight re-toggle continues from the current offset.
- Motion consistency: stray animation timings across ten controls and the demo moved onto the shared token scale; the 100 ms press value is now the `ControlPressAnimationDuration` token.

### Fixed

- `InfoBar`: the close button has a proper Fluent subtle template instead of falling back to the OS default button chrome.
- `ProgressRing` / `ProgressBar`: indeterminate animations park while the control is collapsed or hidden and restart when shown.
- `FontIcon`: the `IsSpinning` rotation stops while unloaded or not visible and resumes when shown.
- `NavigationView`: selecting items faster than the indicator animates retargets it mid-flight instead of snapping back and replaying from zero.

## [0.8.9-Preview] - 2026-07-07

### Changed

- Raised static analysis to its strictest settings (Roslynator and Meziantou at maximum rule sets) and resolved every resulting warning; internal code-quality hardening only, no public API or behavior changes.
- Updated analyzer and polyfill dependencies: BannedApiAnalyzers 5.6.0, Meziantou.Analyzer 3.0.121, Meziantou.Polyfill 1.0.157.

## [0.8.8-Preview] - 2026-07-06

### Added

- `net8.0-windows` target added to `Fluence.Wpf` for PowerShell 7 in-process consumption alongside `net472` and `net10.0-windows10.0.26100.0`.
- `FluenceWindow` defaults its `Icon` to the embedded Fluence brand icon; brand icons ship as vector `DrawingImage` resources plus a square 256px window-icon PNG, slimming `Fluence.Wpf.dll` by roughly 540 KB.
- `FluenceWindow.DefaultIcon` - public static `BitmapSource` exposing the embedded brand icon for consumer windows.
- `InfoBar.GetSeverityGlyph` and `InfoBar.GetSeverityBrushKey` - public helpers returning the canonical severity glyph and theme brush key.

### Changed

- Demo: the gallery Menus page is found when searching "dialog" or "message", and the PowerShell `03-ControlsTour.ps1` script tours common Fluence controls in scrolling `Card` panels.
- NuGet `PackageIcon` repointed to `Fluence_Icon_Light_128.png`; the demo executables set their `ApplicationIcon` to the brand `.ico`.

### Removed

- `assets/Fluence.ico`, superseded by the embedded brand icons.

### Fixed

- `FluenceWindow`: the default brand icon degrades to no icon on any load or render failure instead of faulting the type with a `TypeInitializationException` that broke every window construction.
- Gallery demo: re-enabling the window icon on the Settings page restores `FluenceWindow.DefaultIcon` instead of assigning the raw vector `DrawingImage`.

## [0.8.7-preview] - 2026-06-23

### Fixed

- `ContentDialog` is announced by screen readers on open via an assertive UI Automation live region raising `LiveRegionChanged`, the net472-safe substitute for `AutomationProperties.IsDialog`.
- NavigationView pane-toggle and back buttons expose accessible names ("Navigation" and "Back") instead of a bare "Button".
- Decorative `FontIcon` glyphs are excluded from the UI Automation tree so screen readers announce the labelled parent control instead of unnamed icon nodes.
- `TextBlock` reports `ControlType.Text` via a new `TextBlockAutomationPeer`; only instances with an explicit `AutomationProperties.Name` appear in the control view.

## [0.8.6-preview] - 2026-06-22

### Added

- `ApplicationThemeManager.ResolvedTheme` - read-only property returning the concrete theme (`Light`, `Dark`, or `HighContrast`) resolved by the most recent theme pipeline run.

## [0.8.5-preview] - 2026-06-19

### Changed

- Refreshed brand assets to the updated Fluence logo across the NuGet icon, demo app icons, gallery hero lockup, and README Open Graph card; documentation screenshots regenerated.
- Polished the gallery Accessibility page layout with consistent inter-control spacing.
- Relocated the XAML formatter script to `.claude/hooks/Format-Xaml.ps1`, co-located with the formatting hook that wraps it.

### Fixed

- The gallery hero banner stays legible under High Contrast: wordmark ink is selected from live surface luminance instead of assuming a light surface.

### Removed

- Retired the previous brand asset family and the generated banner vector XAML.

## [0.8.2-preview] - 2026-06-17

### Added

- Accessibility pass across the full control library: every icon-only interactive element (caption, picker, spin, query, tab, close, and pager buttons) now has a non-empty accessible name.
- New automation peers: `RatingControl` (RangeValue), `PasswordBox` (IsPassword), `PersonPicture`, `HyperlinkButton`, and `Card` (Invoke when clickable).
- Header and `Label` text exposed as the accessible name for `NumberBox`, `AutoSuggestBox`, `ToggleSwitch`, and `AppBarButton`.
- `CheckBox` and `RadioButton` description text exposed through `AutomationProperties.HelpText`.
- net472-safe live regions for `InfoBar`, `ProgressBar` / `ProgressRing` state changes, `TeachingTip` open, and `TextBox` validation errors.
- `NumberBox` automation peer reports `LargeChange` from `NumberBox.LargeChange` instead of the inherited `RangeBase` value.
- `ColorPicker` spectrum keyboard operability: arrow keys adjust hue, saturation, and value.
- `RatingControl` keyboard operability: arrows change the value, Home/End jump to min/max; the peer exposes the `RangeValue` pattern.
- `PasswordBox` reveal button toggles with Space and Enter; the peer reports the reveal state.

## [0.8.1-preview] - 2026-06-17

### Added

- Gallery Data page: a `ListBox` sample showing single-selection and `Extended` multi-selection lists.
- Gallery Buttons page: a `ToggleButton` sample with on/off, three-state, and disabled-checked examples.
- `ToggleSplitButton` - WinUI-parity toggle split button: the primary half toggles two-way `IsChecked` then raises `Click`; the peer exposes Toggle and ExpandCollapse.
- `ColorPicker` WinUI option surface: `IsColorPreviewVisible`, `IsColorSliderVisible`, `IsHexInputVisible`, `IsMoreButtonVisible`, `IsAlphaSliderVisible`, and `IsAlphaTextInputVisible`, plus RGB/HSV per-channel and alpha text inputs that commit live on every valid keystroke.

### Changed

- Gallery Forms page: the TimePicker, DatePicker, and ColorPicker samples now lead the page.
- `TextBox` and `PasswordBox`: helper text, validation message, caps lock indicator, and strength meter sit 2px further below the input box.
- Gallery sample cards: the source-code expander mirrors the WinUI Gallery `ControlExample` chrome and show/hide transition.
- `NavigationView`: the pane/content seam uses a dedicated `NavigationViewContentSeparatorBrush` token, fainter in Dark; Top mode sits flush under the title bar.
- Gallery Icons page redesigned as a WinUI-style Iconography catalog: live search, virtualized tile grid, and a details sidebar with copyable values.
- `TreeView` multi-select: checking a selection checkbox no longer tints the row background; row selection keeps its background.
- `ColorPicker.IsColorChannelTextInputVisible` now governs the representation selector and channel inputs; hex visibility moved to the new `IsHexInputVisible`.

### Fixed

- `ColorPicker` More/Less button renders as a flat subtle toggle instead of a distorted accent-filled `ToggleButton`.
- `Card` `Filled` variant is visible again: it uses `ControlAltFillColorQuarternaryBrush` instead of a brush that composited to the surface color.
- `ListBoxItem` selection indicator renders at the canonical 3x16 size with the same slide animations and disabled brush as `ListViewItem`.
- `ToggleButton` checked hover and pressed tints render; state triggers reordered rest, hover, pressed.
- `ToggleButton` indeterminate state renders the WinUI parity visuals for hover, pressed, and disabled.
- `ToggleButton` checked-pressed matches WinUI: softened on-accent foreground and transparent border.
- `ToggleButton.Appearance` values other than `Standard` no longer disable every state visual.
- `SplitButton` and `ToggleSplitButton` focus rings appear only during keyboard navigation, not when a half is clicked.

## [0.8.0-preview] - 2026-06-10

### Added

- `Flyout`, `FlyoutBase`, and `FlyoutPresenter` - a WinUI-parity light-dismiss popup family with `ShowAt` / `Hide`, lifecycle events, `Placement`, and `FlyoutBase.AttachedFlyout`.
- `ContentDialog` - a WinUI-parity modal dialog with up to three command buttons, `DefaultButton`, `Task<ContentDialogResult> ShowAsync()` / `Hide()`, a smoke layer, Escape/Enter handling, and Tab focus trapping.
- `TeachingTip` - a targeted coaching callout with `Target`, `PreferredPlacement`, light dismiss, action and close buttons, and a beak pointing at the target.
- `AutoSuggestBox` - a search/autocomplete input with `TextChanged`, `SuggestionChosen`, and `QuerySubmitted`; the application drives filtering, with full keyboard navigation.
- `DatePicker` - a culture-ordered day/month/year field with flyout selector columns and leap-day-aware day rebuilds.
- `TimePicker` - a time-of-day selector with `ClockIdentifier`, `MinuteIncrement`, and hour/minute/AM-PM flyout columns.
- `ColorPicker` - an HSV-source-of-truth color selector with a saturation/value spectrum, hue and alpha sliders, swatches, and hex input.
- `CommandBarFlyout` and `AppBarButton` - a compact command strip of primary icon buttons with an expandable overflow menu of secondary commands.
- `BreadcrumbBar` and `BreadcrumbBarItem` - a navigation trail of clickable crumbs with chevron separators and an emphasized last crumb.
- `PipsPager` - a compact page indicator with clickable pips, a sliding visibility window, orientation, and optional nav buttons.

### Changed

- `ProgressBar` and `ProgressRing` re-aligned to the WinUI 3 look and animation: thin-track metrics, the canonical two-segment indeterminate storyboard, and the pulsing-arc-plus-rotation ring.
- `ProgressBar` / `ProgressRing` lead with the WinUI-orthogonal API (`ShowError` / `ShowPaused`, `IsActive`); the previous mode enums remain as one-way aliases.
- `PasswordBox` indicators are opt-in: `ShowCapsLockIndicator` and `ShowPasswordStrength` default to `false`.

### Fixed

- Icons in a control's `Icon` slot render in the host's text color and stay matched through visual states and theme switches; an explicit icon `Foreground` still wins.
- Icon-only `Button` and `HyperlinkButton` instances center the glyph instead of reserving the icon-to-text gap.

## [0.7.0-preview] - 2026-06-02

### Added

- PowerShell examples: four self-contained Windows PowerShell 5.1 scripts plus a new `docs/powershell.md` guide.
- Beginner documentation for the gallery demo: README, XML docs and inline comments, and designer-ready design-time resources.
- `Fluence.Wpf.Demo.Mvvm` ships design-time resources, a design-time `d:DataContext`, and a seeded sample task list.
- Design-time color and brush dictionaries (`DesignTime.Light.xaml` / `DesignTime.Dark.xaml`) holding the complete computed palette, drift-guarded by CI.
- `NavigationView.FooterMenuItems` - a pinned, selectable footer region sharing the selection model and sliding pill indicator with the main menu.
- Repo-owned XAML formatting: XAML Styler pinned as a dotnet tool with a committed reference style, a format script, an edit hook, and a CI check.

### Changed

- `FluenceWindow` and `TitleBar` re-authored from scratch to WinUI-canonical metrics and internals; the public API surface is unchanged (drop-in).
- `FluenceWindow.TitleBarHeight` default changed from 68 to 48 (the WinUI canonical height); caption buttons are a uniform 46 px wide and stretch to the full title-bar height.
- `WindowButtonStyle` hover and press fills switched to the WinUI subtle fills; `WindowCloseButtonStyle` unchanged.
- `TitleBar` glyph button width changed from 42 to 40 px, matching the WinUI back/pane-toggle hit area.
- `TitleBar` code-behind re-authored for structure and docs; behavior and public contract identical.
- `NavigationView`: removed the divider line above the pane footer.
- `WindowPolicy.CreateWindowChrome` drops its never-effective `captionHeight` parameter.
- One-time XAML normalization of 9 demo/PowerShell files to the reference style; formatting-only.
- Gallery demo: Settings moved from `PaneFooter` into `FooterMenuItems`, retiring the bespoke Settings handling.
- Gallery demo: control-group vertical spacing split top/bottom so centered groups sit equidistant from card edges.
- Gallery demo: decorative glyphs use `AccentTextFillColorPrimaryBrush` (the accent foreground role) for a consistent accent pop.
- Test-suite reliability and speed: hermetic `ThemeParityTests`, the screenshot harness made opt-in and trimmed to ten documentation PNGs, condition-based waits, and helpers consolidated into `WpfTestSta`.

### Fixed

- `FluenceWindow` snap-layout flyout hover matches the normal hover fill and tracks theme/accent/high-contrast changes via `SetResourceReference`.
- `NavigationView`: switching `PaneDisplayMode` between `Left` and `LeftCompact` animates the pane width instead of snapping.
- `ScrollBar` hover/expanded thickness reduced from 10 to 8 px; the collapsed 6 px indicator unchanged.
- `NavigationView` Top mode: footer items render gear-only, and the footer indicator centers under the selected item and animates in and out.
- `NavigationView`: the selection indicator keeps tracking after the control is unloaded and reloaded; template parts are preserved across reparenting.
- `ProgressBar` default `TrackHeight` reduced from 6 to 4 px, matching the DP default and the WinUI reference.
- `NavigationView`: the Settings footer icon no longer drifts sideways while the pane opens or collapses.
- `FluenceWindow` immersive dark mode picks DWM attribute 19 vs 20 by OS build, fixing light captions on early Windows 10.
- `FluenceWindow` maximized windows respect an auto-hidden taskbar via a 2 px edge shift in `WM_GETMINMAXINFO`.
- `FluenceWindow` subscribes to the static theme managers in `OnSourceInitialized` and unsubscribes in `OnClosed`, so constructed-but-never-shown windows no longer leak.
- `TreeView` outer border clips item hover highlights to its rounded corner.
- `ProgressBar` indeterminate mode installs a rounded geometry clip so the translating bars follow the rounded track instead of showing square ends.
- `NavigationView` left-pane item icons stay on a single vertical column across open, collapsed, and compact states.
- `NavigationView` selection indicator sits just inside the selected item's rounded border instead of floating in the pane.
- `TitleBar` glyph buttons drop a legacy 4px rightward glyph nudge, aligning with the NavigationView icon rail.
- Theme engine hardening: the accent resolver guards malformed registry data and missing DWM ordinals, slot insertion keeps the `[0]/[1]/[2]` contract with foreign dictionaries present, `ResetForTesting` re-seeds the default ramp, Windows 11 detection uses `RtlGetVersion`, and the high-contrast `AccentFillBackdrop` derives from live `SystemColors`.
- `FluenceWindow` no longer runs `ApplyFrame` twice per theme change; the backdrop background brush is frozen, and flipping `ExtendsContentIntoTitleBar` at runtime refreshes the caption layout.
- `FluenceWindow.HitTestTitleBar` skips the per-message hit test when nothing interactive can be under the cursor.
- `NativeMethods.ColorToAbgr` renamed to `ColorToColorRef`, documenting the `0x00BBGGRR` COLORREF layout it emits.

### Removed

- The `FluenceWindow` first-paint hold: windows show immediately on open, keeping the redirection-surface clear and backdrop-composition gating.
- `Themes/Shared.xaml`: its three close-button brand colors are seeded in C# by `BaseColorTables.AddSharedColors`.

## [0.6.0-preview] - 2026-05-24

### Added

- `Themes/Shared.xaml` - theme-independent Color tokens (the close-button brand reds). Superseded since: the engine rebuild removed the file and seeds the tokens in C#.

### Changed

- Widened the accent ramp spread in `HsvColorHelper.GenerateAccentRampWinaccent` to roughly 10-12% per step so adjacent rungs read as distinct in control templates.
- Demo source-code expander header uses `SolidBackgroundFillColorQuarternaryBrush` so it reads as a distinct band.
- `NavigationView` sizing aligned with WinUI 3: open pane 320 px, item font size 14, and a divider above the `PaneFooter` slot.
- `NavigationView` surface roles realigned to WinUI 3: the pane uses `AcrylicInAppFillColorDefaultBrush` and the content host uses `LayerFillColorDefault`, letting Mica pass through.
- `TitleBar` sizing: app-title text moved to `BodyTextBlockStyle` (14 pt) and the app icon shrunk to 20x20.
- Extended the `AccentFillBackdrop` opaque sub-layer pattern to every control whose template applies a sub-1.0-alpha accent fill.
- Gallery home page cards rewritten to the standard `Card.Header` / `Card.Icon` contract.
- Settings row text styles matched to WinUI 3 `SettingsCard` sizing.

### Fixed

- `ProgressBar` template: removed a dead `BorderThickness` setter, corrected the unfilled track to the strong-stroke fill role, and made the track a 6 px full pill.
- `FluenceWindow` no longer forces `ClearTypeHint=Enabled` at the window root, restoring correct per-surface text anti-aliasing over translucent surfaces.

## [0.5.0] - 2026-05-21

- Initial release.
