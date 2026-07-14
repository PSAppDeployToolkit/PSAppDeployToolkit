# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project follows [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.10-Preview] - 2026-07-14

### Added

- CI: pushing a `v*` tag now creates the GitHub release automatically, attaching the `net472`, `net8.0-windows`, and `net10.0-windows` library binaries, the demo app, and the NuGet package (tags containing `-pre` are marked prerelease). The .NET 8 binaries are also uploaded as workflow artifacts on every build.
- Controls respect the Windows "Show animations in Windows" accessibility setting, read via `SystemParameters.ClientAreaAnimation`. With the setting off, code-driven animations are skipped and controls jump to their final state. Covered: `ProgressRing` and `ProgressBar` indeterminate motion, `FontIcon` spin, `ContentDialog` open and close, `NavigationView` pane and indicator motion, the `Flyout`, `TeachingTip`, and `ComboBox` reveals, the `Expander` slide, `ToggleSwitch` knob and thumb motion, `ListView` insert/remove, and `SmoothScrollViewer` wheel scrolling. Hover/press micro-feedback and other XAML template storyboards are not gated; toggling the OS setting mid-session applies at each animation's next start.

### Changed

- Demo: the home page hero now shows the Fluence header lockup drawn for the active theme (`FluenceHeaderLightDrawingImage` on light themes, `FluenceHeaderDarkDrawingImage` on dark, chosen by system window luminance under high contrast), replacing the square brand mark and separate title text. The unused lockup PNGs are removed from the demo resources.
- ScrollBar: hover expand/contract now runs 167 ms and the track fade 83 ms, matching WinUI's `ScrollBarExpandDuration`, `ScrollBarContractDuration`, and `ScrollBarOpacityChangeDuration`; previously 120 ms and 150 ms.
- `ListView`: item removal eases out like the insert path, so a deleted item no longer lingers at full opacity before snapping away.
- `ComboBox`: the dropdown opens with the standard reveal, an 8 px slide from the control edge (upward-opening dropdowns slide up) plus fade over 167 ms on the `0.8,0,0,1` spline, instead of unfolding from zero height. The reveal runs from code and honors the reduced-motion setting.
- `Flyout`: the open reveal slides 8 px in from the requested placement side (`Top` up, `Bottom` down, `Left`/`Right` horizontally) instead of always sliding down. Timing is unchanged: 167 ms with fade on the `0.8,0,0,1` spline.
- `TeachingTip`: the reveal slides 8 px in from the side the tip actually opened on (the resolved `ActualPlacement`, beak leading); untargeted and Center tips fade only. The 83 ms fade and 167 ms slide are unchanged, and the reveal honors the reduced-motion setting.
- `ContentDialog`: closing plays the WinUI DialogHidden exit, a 167 ms scale back to 1.05 with an 83 ms linear fade, instead of a hard cut; input stops the moment the close starts. `ShowAsync` now completes when the exit finishes, about 167 ms later than before. With animations off, or while the owner window is closing, teardown stays immediate.
- `ToolTip`: fades in over 83 ms on the `0.8,0,0,1` spline (WinUI `FadeInThemeAnimation` parity); close stays instant. The theme suppresses the OS tooltip popup fade so only one fade plays.
- `CheckBox`: checking animates the glyph in with a 100 ms fade and a 167 ms scale settle from 0.7; unchecking stays instant.
- Demo only: page navigation keeps just the 167 ms fade (the 20 px slide is removed), and the color-swatch hover scale drops from 1.08 over 120 ms to 1.05 over 100 ms.
- `ProgressBar`: determinate value changes animate the fill's `ScaleTransform.ScaleX` instead of its layout `Width`, so each frame is composite-only. The 367 ms duration and (0.1,0.9,0.2,1.0) spline are unchanged; the template gains a `PART_FillScale` part and the indicator host's geometry clip owns the corner rounding.
- `ToggleSwitch`: the thumb grow/shrink (12x12 rest, 14x14 hover, 17x14 pressed) animates a render-thread `ScaleTransform` instead of layout `Width`/`Height`, removing a layout pass per frame. Sizes, 83 ms timing, and easing are unchanged.
- `Expander`: expand and collapse slide the content behind its clip by the measured height (WinUI parity), 333 ms in on the `0,0,0,1` spline and 167 ms out on `1,1,0,1`; a mid-flight re-toggle continues from the current offset. The chevron rotation is unchanged.
- Motion consistency: stray animation timings moved onto the token scale across ToggleButton, RepeatButton, ListBox, ListView, RadioButton, Slider, TabControl, TabView, InfoBar, and the demo: 120 ms press/exit values to 100 ms, 150 and 180 ms transitions to the 167 ms Fast token, and 50 ms indicator fades to the 83 ms Faster token. The shared 100 ms press value is now the `ControlPressAnimationDuration` token in `Typography.xaml`.

### Fixed

- `InfoBar`: the close button now has a proper Fluent template: a `ControlCornerRadius` rounded plate, transparent at rest with `SubtleFillColorSecondary` hover and `SubtleFillColorTertiary` press, and the glyph at `TextFillColorPrimary`. It previously fell back to the OS default button chrome, with square corners and legacy hover colors.
- `ProgressRing` / `ProgressBar`: indeterminate animations park while the control is collapsed or hidden and restart when it is shown, instead of ticking while nothing paints.
- `FontIcon`: the `IsSpinning` rotation stops while the icon is unloaded or not visible and resumes when shown, instead of ticking forever.
- `NavigationView`: selecting items faster than the indicator animates no longer snaps the indicator back and replays from zero; a mid-flight retarget continues from the current position, scale, and opacity. The 90 ms and 140 ms timings are unchanged.

## [0.8.9-Preview] - 2026-07-07

### Changed

- Raised static analysis to its strictest settings and resolved every resulting warning across the library, tests, and demos. Roslynator.Analyzers and Meziantou.Analyzer now run at their maximum rule sets (RCS1046, RCS1056, MA0032, MA0148, MA0162, MA0171, MA1049, and related rules addressed), with analyzer configuration consolidated in the project files.
- Updated analyzer and polyfill dependencies: Microsoft.CodeAnalysis.BannedApiAnalyzers 3.3.4 to 5.6.0, Meziantou.Analyzer 3.0.117 to 3.0.121, and Meziantou.Polyfill 1.0.152 to 1.0.157.

There are no public API or behavioural changes in this release; it is internal code-quality hardening only.

## [0.8.8-Preview] - 2026-07-06

### Added

- `net8.0-windows` target added to `Fluence.Wpf` to enable PowerShell 7 (.NET 8) in-process consumption alongside the existing `net472` and `net10.0-windows10.0.26100.0` targets.
- `FluenceWindow` now defaults its `Icon` to the Fluence brand icon embedded in `Fluence.Wpf.dll`, so every window (the gallery and MVVM demos, and the PowerShell demo scripts) shows the Fluence icon in its title bar and taskbar with no caller setup. A consumer-assigned `Icon` still overrides the default. The brand icon now ships as resolution-independent vector `DrawingImage` resources (`FluenceIconBrandDrawingImage`, `FluenceIconLightDrawingImage`, `FluenceIconDarkDrawingImage` in `Themes/Icons/FluenceIcons.xaml`, merged into `Generic.xaml`); the window and taskbar icon itself is the square, no-background brand mark embedded as a 256x256 PNG (`Themes/Icons/Fluence_Icon_NoBackground_256.png`), which the Win32 HICON renders crisply without the aspect distortion a non-square source would introduce. Dropping the embedded multi-resolution `Fluence.ico` slims `Fluence.Wpf.dll` by roughly 540 KB (about 40% of the binary). The gallery home page shows the large brand vector as its hero.
- `FluenceWindow.DefaultIcon` is now a public static property exposing the embedded square Fluence brand icon (a `BitmapSource`), so a consumer can apply the same Win32-HICON-compatible mark to its own windows; a vector `DrawingImage` does not reliably drive a taskbar HICON.
- `InfoBar.GetSeverityGlyph(InfoBarSeverity)` and `InfoBar.GetSeverityBrushKey(InfoBarSeverity)` - public helpers returning the canonical Segoe Fluent glyph and theme brush key for each severity, so a consumer can render the severity icon outside an InfoBar without hardcoding codepoints.

### Changed

- Demo: the gallery Menus page is now found when searching "dialog" or "message" (its ContentDialog sample lives there), and the PowerShell `03-ControlsTour.ps1` demo script tours common Fluence controls (Button, ToggleSwitch, CheckBox, RadioButton, TextBox, NumberBox) inside scrolling `Card` panels.
- `Fluence.Wpf` NuGet `PackageIcon` repointed from the retired `Fluence_Logo_128.png` to `Fluence_Icon_Light_128.png`. The demo executables set their Windows `ApplicationIcon` to `assets/Fluence_Icon_Light.ico`, so the `.exe` shows the Fluence brand mark in Explorer and on a pre-launch taskbar pin; the runtime window and title-bar icon come from the `FluenceWindow` embedded square brand icon.

### Removed

- `assets/Fluence.ico`, now superseded by the embedded brand icons (vector `DrawingImage` resources and the window-icon PNG).

### Fixed

- `FluenceWindow`: the default brand icon now degrades to no icon on any load or render failure. `CreateDefaultIcon` runs in a static field initializer and previously caught only `IOException`, so any other failure (a missing or renamed resource key, or a COM, GPU, or memory failure loading or decoding the embedded icon under a headless or session-0 host such as PSADT running as SYSTEM) faulted the `FluenceWindow` type with a `TypeInitializationException` and broke every window construction in the process.
- Gallery demo: re-enabling the window icon on the Settings page restores the embedded brand icon (`FluenceWindow.DefaultIcon`, so the Win32 taskbar HICON renders) instead of assigning the raw vector `DrawingImage`.

## [0.8.7-preview] - 2026-06-23

### Fixed

- `ContentDialog` is now announced by screen readers when it opens. The dialog
  declares an assertive UI Automation live region and raises `LiveRegionChanged`
  as it appears, so Windows Narrator, NVDA, and JAWS read the dialog `Title` on
  open instead of staying silent. Because the dialog is hosted in an overlay (not
  a separate window), nothing previously prompted assistive technologies to read
  it. This is the net472-safe substitute for `AutomationProperties.IsDialog` (a
  .NET Framework 4.8 API absent on the `net472` target) and complements the
  existing `ControlType.Window` automation peer and Tab focus trapping.
- NavigationView pane-toggle (hamburger) and back buttons now expose accessible
  names ("Navigation" and "Back") so Windows Narrator and NVDA announce their
  purpose instead of a bare "Button".
- Decorative FontIcon glyphs are now excluded from the UI Automation tree
  (AccessibilityView=Raw equivalent), so screen readers no longer stop on
  unnamed icon nodes; the labelled parent control is announced instead.
- `TextBlock` now correctly reports `ControlType.Text` to UI Automation via a
  new `TextBlockAutomationPeer`. Previously, because `Fluence.Wpf.Controls.TextBlock`
  is a `ContentControl` subclass rather than a `System.Windows.Controls.TextBlock`
  subclass, WPF assigned it a generic peer that reported `ControlType.Pane`,
  placing a spurious container node in the UIA tree and breaking
  `AutomationProperties.LabeledBy` relationships. Only instances with an explicit
  `AutomationProperties.Name` are visible in the control view; decorative body-copy
  instances without a name are excluded so Narrator does not announce them.

## [0.8.6-preview] - 2026-06-22

### Added

- `ApplicationThemeManager.ResolvedTheme` - a new read-only public property that returns the concrete theme (`Light`, `Dark`, or `HighContrast`) resolved during the most recent theme pipeline run. The pipeline is triggered by `ApplicationThemeManager.Apply` and also by `ApplicationAccentColorManager.ApplySystemAccent`, `ApplyApplicationAccent`, and `ApplyCustomAccent`, so an accent change alone can update `ResolvedTheme`. When `CurrentTheme` is `Auto`, it reflects the OS theme at the time of the last pipeline run rather than `Auto` itself. Defaults to `Light` before the first pipeline run.

## [0.8.5-preview] - 2026-06-19

### Changed

- Refreshed brand assets across the project to the updated Fluence logo. The NuGet
  package icon now uses the dual-use `Fluence_Logo_128.png`; the gallery and MVVM
  demos share an updated multi-resolution `Fluence.ico` app icon; the gallery
  home-page hero banner uses the side-by-side lockup (`Fluence_Lockup_SideBySide_*`),
  selected by ink color so the wordmark stays legible in every theme; and the README
  header uses the theme-aware Open Graph card (`Fluence_OGImage_*`) via a
  `prefers-color-scheme` `<picture>` element. Documentation screenshots regenerated.
- Polished the gallery Accessibility page demo layout: consistent inter-control
  spacing via `Spacing` on `StackPanel` and tightened margins in the focusable-input,
  icon-only-button, and live-region samples.
- Relocated the XAML formatter script from `eng/Format-Xaml.ps1` to
  `.claude/hooks/Format-Xaml.ps1`, co-located with the formatting hook that wraps it;
  updated the CI workflow, PR template, hook, and contributor docs to match.

### Fixed

- Gallery home-page hero banner stays legible under High Contrast. The new lockups
  are transparent (the previous banners carried an opaque background), so the wordmark
  ink is now selected from the live surface luminance instead of assuming a light
  surface, keeping it readable on a High-Contrast-Dark scheme.

### Removed

- Retired the previous brand asset family (`fluence-wpf-banner-*`,
  `fluence-wpf-appicon-*`, `fluence-wpf-nuget-icon-128`, `fluence-wpf-og-card-*`,
  `fluence-wpf-square-*`, `fluence-wpf-twitter-header-*`, `fluence-wpf-github-header`)
  and the generated banner vector XAML.

## [0.8.2-preview] - 2026-06-17

### Added

- Accessibility pass across the full control library:
  - Named glyph buttons throughout the shell and controls: window caption buttons
    (minimize, maximize, restore, close), `TitleBar` back and pane-toggle buttons,
    `DatePicker` and `TimePicker` field and column-selector buttons, `NumberBox`
    increment and decrement spin buttons, `AutoSuggestBox` query button,
    `TabView` close, add-tab, and scroll buttons, `InfoBar` close button,
    `TeachingTip` close and alternate-close buttons, and `PipsPager` previous and
    next navigation buttons. Every icon-only interactive element now has a
    non-empty accessible name exposed through `AutomationProperties.Name` in XAML
    or the automation peer.
- New automation peers in `Fluence.Wpf.Automation`: `RatingControlAutomationPeer`
  (exposes the `RangeValue` pattern), `PasswordBoxAutomationPeer` (reports a
  password edit field via `IsPassword=true`), `PersonPictureAutomationPeer` (reports
  display name or initials as name, Image control type), `HyperlinkButtonAutomationPeer`
  (derives from `ButtonAutomationPeer`, overrides control type to `Hyperlink`), and
  `CardAutomationPeer` (exposes the `Invoke` pattern when `IsClickable` is true).
  - Header and `Label` accessible names for `NumberBox`, `AutoSuggestBox`, `ToggleSwitch`,
    and `AppBarButton` exposed as the accessible name via the control's automation peer
    (`GetNameCore` override) and `AutomationProperties.Name`, so screen readers announce
    the field label alongside the control name.
  - `CheckBox` and `RadioButton` description text is now exposed through the
    `AutomationProperties.HelpText` property rather than being lost in the
    visual-only description `TextBlock`, so Narrator announces it as supplemental
    help text.
  - net472-safe live regions using `AutomationProperties.LiveSetting` plus
    `RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)` for `InfoBar`
    (severity and message announced on `IsOpen` change), `ProgressBar` and
    `ProgressRing` (error and paused state changes announced), `TeachingTip`
    (title and subtitle announced on open), and `TextBox` validation (validation
    message announced when `ValidationState` changes to an error state).
  - `NumberBox` automation peer `LargeChange` value fixed: the peer now reports
    `LargeChange` from `NumberBox.LargeChange` instead of the inherited
    `RangeBase.LargeChange`, so assistive technologies request the correct
    increment when invoking the large-change action.
  - `ColorPicker` color spectrum keyboard operability: arrow keys adjust hue,
    saturation, and value on the spectrum canvas, matching WinUI behavior and
    enabling full keyboard-only color selection.
  - `RatingControl` keyboard operability: left and right arrow keys change the
    rating value, Home and End jump to minimum and maximum, and the automation
    peer reports the current value and maximum via the `RangeValue` pattern.
  - `PasswordBox` reveal button keyboard operability: Space and Enter toggle the
    reveal state when the reveal button has keyboard focus, and the automation
    peer reports the current `IsPasswordRevealed` state.

## [0.8.1-preview] - 2026-06-17

### Added

- Gallery Data page: a dedicated `ListBox` sample showing the default single-selection list next to an `Extended` multi-selection list, closing the last demo coverage gap in the control catalog. Covered by `ControlTests.DemoParity.cs`.
- Gallery Buttons page: a `ToggleButton` sample with an on/off state label, a three-state (`IsThreeState`) example showing the indeterminate visuals, and a disabled-checked example. Covered by `ControlTests.DemoParity.cs`.
- `ToggleSplitButton` - a `SplitButton` subclass mirroring the WinUI 3 control: the primary half toggles the new `IsChecked` property (two-way bindable) and then raises `Click`, so handlers observe the already-toggled state, while the chevron half keeps opening the flyout. `IsCheckedChanged` carries the new state via `ToggleSplitButtonIsCheckedChangedEventArgs`; checked renders both halves in the accent palette with the WinUI checked divider stroke (`ControlStrokeColorOnAccentTertiaryBrush`), including the CheckedFlyoutOpen both-halves-pressed tint and the disabled-checked palette. The automation peer exposes the Toggle and ExpandCollapse patterns (no Invoke, per WinUI). `SplitButton.OnPrimaryButtonClick` became `protected virtual` to host the toggle hook. Shown on the gallery Buttons page; covered by `ControlTests.ToggleSplitButton.cs`.

- `ColorPicker` gallery-default option surface mirroring WinUI 3: new `IsColorPreviewVisible`, `IsColorSliderVisible`, `IsHexInputVisible`, `IsMoreButtonVisible` (default `false`; `true` collapses the text-entry area behind a More/Less toggle), `IsAlphaSliderVisible`, and `IsAlphaTextInputVisible` properties, plus a text-entry area with an RGB/HSV representation selector, per-channel text inputs (`PART_RedTextBox` through `PART_ValueTextBox`), and an alpha percentage input (`PART_AlphaTextBox`). Channel and alpha inputs commit live on every valid keystroke with the typed value preserved exactly (HSV edits replace only the edited component, so untouched fractional components are never quantized); Enter or focus loss normalizes the text and restores it after invalid input. The Forms gallery sample gains a right-rail option panel mirroring the WinUI Gallery. Covered by the expanded `ControlTests.ColorPicker.cs`.

### Changed

- Gallery Forms page: the TimePicker, DatePicker, and ColorPicker samples now lead the page, ahead of the sign-in, checkout, and settings form compositions.
- `TextBox` and `PasswordBox`: the helper text, validation message, caps lock indicator, and password strength meter now sit 2px further below the input box.
- Gallery sample cards: the source-code expander now mirrors the WinUI Gallery `ControlExample` - hover and press feedback render on the chevron backplate only (the header surface no longer lights up); the collapsed header uses `CardBackgroundFillColorSecondaryBrush` (the WinUI Gallery source-code surface) and the expanded source area uses `SystemFillColorSolidAttentionBackgroundBrush` (#F7F7F7 light / #2E2E2E dark). Expanding and collapsing now use the WinUI Gallery show/hide transition instead of a full-height slide: the source content slides up 24px and fades in on expand (slide 0.4s, fade 0.2s, ease-out) and slides down 24px and fades out on collapse (slide 0.2s, fade 0.1s, ease-out) before the row releases its space. The copy-source button keeps its 1px `ControlStrokeColorDefaultBrush` outline.
- `NavigationView`: the pane/content seam in the Left, LeftCompact, and Top templates now uses a dedicated `NavigationViewContentSeparator` color token (`NavigationViewContentSeparatorBrush`). In Light it keeps the prominent stroke (`#29000000`, unchanged from the previous `ControlStrokeColorSecondaryBrush`); in Dark it is intentionally fainter (`#0FFFFFFF`) so the divider does not over-assert over Mica; High Contrast keeps the system control-dark stroke. In Top mode the outer template drops its top border and top corner rounding so the nav strip sits flush under the window title bar with no seam.
- Gallery Icons page redesigned as an Iconography catalog matching the WinUI 3 Gallery: live search over icon name, code, and derived tags, a width-adaptive virtualized tile grid with an accent selection ring, and a details sidebar with copyable Icon name, Text glyph, Code glyph, FontIcon XAML, and FontIcon C# values plus tag pills.
- `TreeView` multi-select: checking an item's selection checkbox no longer tints the row background; the checkbox alone conveys the checked state, matching WinUI TreeView multi-select. Row selection (`IsSelected`) keeps its background.
- `ColorPicker.IsColorChannelTextInputVisible` now governs the RGB/HSV representation selector and the per-channel text inputs, matching WinUI semantics. It previously collapsed the hex input row; hex visibility is now governed by the new `IsHexInputVisible` property (migration: replace `IsColorChannelTextInputVisible="False"` with `IsHexInputVisible="False"` if you used it to hide the hex box).

### Fixed

- `ColorPicker` More/Less button no longer renders as a distorted accent-filled toggle. It used the default `ToggleButton` whose checked state fills with the accent brush and applies a press scale; it now uses a flat subtle toggle (transparent rest, `SubtleFillColorSecondary` / `SubtleFillColorTertiary` hover and press, no accent or scale), matching the WinUI ColorPicker, with the checked state conveyed by the "Less" label and flipped chevron.
- `Card` `Filled` variant is now visible. It used `CardBackgroundFillColorSecondaryBrush`, which composites to roughly the host surface color and disappeared; it now uses `ControlAltFillColorQuarternaryBrush` for a clear tonal fill that reads as a filled container in both Light and Dark.

- `ListBoxItem` selection indicator now renders at the canonical 3x16 size, vertically centered, matching `ListViewItem` (previously a `ScaleTransform` with an absolute `CenterY` left the bar at 60 percent height anchored near its top edge). The indicator also adopts the same 167 ms slide-in / 120 ms slide-out animation as `ListViewItem`, and a disabled selected item now shows the indicator with `AccentFillColorDisabledBrush` instead of hiding it. Covered by `ControlTests.ListBox.cs`.
- `ToggleButton` checked hover and pressed tints now render. The unconditional checked-rest trigger was declared after the checked hover and pressed triggers, so WPF's last-active-trigger precedence kept the rest fill winning whenever the pointer hovered or pressed a checked toggle; the triggers are now ordered rest, hover, pressed within each state family. Covered by `ControlTests.ToggleButton.cs`.
- `ToggleButton` indeterminate state (`IsThreeState`, `IsChecked` of `null`) now renders the WinUI parity visuals for hover, pressed, and disabled using the neutral control palette; previously no state change rendered at all.
- `ToggleButton` checked-pressed state now matches WinUI: the foreground softens to `TextOnAccentFillColorSecondaryBrush` and the border becomes transparent (previously primary on-accent text with the accent elevation border).
- Setting `ToggleButton.Appearance` to a value other than `Standard` no longer disables every state visual. Each state trigger was conditioned on `Appearance=Standard`, so `Appearance="Accent"` rendered checked and unchecked identically; the template now applies the single canonical WinUI toggle visual for all appearance values.
- `SplitButton` and `ToggleSplitButton` focus rings now appear only during keyboard navigation (Tab), not when a half is clicked. The per-half in-template focus borders were driven by `IsKeyboardFocused`, which mouse clicks also set; each half now carries the standard `DefaultControlFocusVisualStyle` adorner instead, which WPF renders only for keyboard focus, matching `DropDownButton` and the rest of the library. Covered by `ControlTests.SplitButton.cs` and `ControlTests.ToggleSplitButton.cs`.

## [0.8.0-preview] - 2026-06-10

### Added

- `Flyout`, `FlyoutBase`, and `FlyoutPresenter` - a light-dismiss popup family mirroring the WinUI 3 flyout contract. `FlyoutBase` owns the popup lifecycle (`ShowAt` / `Hide`, `Opening` / `Opened` / cancelable `Closing` / `Closed`, and `Placement` via the new `FlyoutPlacementMode` enum) plus the `FlyoutBase.AttachedFlyout` attached property and `ShowAttachedFlyout` so any element can carry a flyout; `Flyout` adds `Content` and `FlyoutPresenterStyle`, rendered inside the themed `FlyoutPresenter` chrome. The popup centers on the facing edge of its placement target like WinUI, Escape dismisses it, and the presenter inherits the placement target's `DataContext` while open. `FlyoutBaseClosingEventArgs` lives in the root `Fluence.Wpf` namespace alongside the other event-args types (it briefly sat in `Fluence.Wpf.Controls` during this release's development). Shown on the gallery Menus page; covered by `ControlTests.Flyout.cs`.
- `ContentDialog` - a modal dialog mirroring the WinUI 3 control: `Title` / `TitleTemplate`, body `Content`, up to three command buttons (`PrimaryButtonText` / `SecondaryButtonText` / `CloseButtonText` with `IsPrimaryButtonEnabled` / `IsSecondaryButtonEnabled`, per-button commands, and cancelable `PrimaryButtonClick` / `SecondaryButtonClick` / `CloseButtonClick` events), `DefaultButton`, `Opened` / `Closed`, and `Task<ContentDialogResult> ShowAsync()` / `Hide()`. While open, a smoke layer dims and blocks the owner window - the full window, title bar included, over a `FluenceWindow` (whose template exposes `PART_DialogOverlayHost`), or the content adorner layer on a plain `Window` - with Escape dismissing, Enter invoking the default button, and Tab navigation trapped inside the dialog. Shown on the gallery Menus page; covered by `ControlTests.ContentDialog.cs`.
- `TeachingTip` - a targeted coaching callout mirroring the WinUI 3 control: `Title`, `Subtitle`, body `Content`, `Target`, `IsOpen`, `PreferredPlacement` (via the new `TeachingTipPlacementMode` enum), `IsLightDismissEnabled`, `ActionButtonContent` / `ActionButtonCommand` / `ActionButtonCommandParameter`, `CloseButtonContent`, and `ActionButtonClick` / `CloseButtonClick` / `Closed` events. The tip anchors to `Target` with a beak pointing at it; untargeted tips center over the window content and hide the beak. Shown on the gallery Menus page; covered by `ControlTests.TeachingTip.cs`.
- `AutoSuggestBox` - a search / autocomplete text input mirroring the WinUI 3 control: `Text`, `ItemsSource`, `IsSuggestionListOpen`, `TextMemberPath`, `PlaceholderText`, `UpdateTextOnSelect`, `QueryIcon`, and `Header`, with `TextChanged` (carrying an `AutoSuggestionBoxTextChangeReason` and a `CheckCurrent()` staleness probe), `SuggestionChosen`, and `QuerySubmitted` events. The application drives filtering by handling `TextChanged` and updating `ItemsSource`; Up / Down move through the suggestion list, Enter submits, and Escape dismisses. Shown on the gallery Inputs page; covered by `ControlTests.AutoSuggestBox.cs`.
- `DatePicker` - a date selector mirroring the WinUI 3 control: `SelectedDate` (`DateTime?`), `MinYear` / `MaxYear`, `DayVisible` / `MonthVisible` / `YearVisible`, `Header`, `PlaceholderText`, and a `SelectedDateChanged` event. The field orders its day, month, and year segments by the current culture's short date pattern; the flyout's selector columns commit through an accept button and discard through cancel, with the day column rebuilt for the pending month and year so leap days are offered only when valid. Shown on the gallery Forms page; covered by `ControlTests.DatePicker.cs`.
- `TimePicker` - a time-of-day selector mirroring the WinUI 3 control: `SelectedTime` (`TimeSpan?`), `ClockIdentifier` (`12HourClock` or `24HourClock`; the default follows the current culture's short time pattern, and empty culture AM/PM designators fall back to invariant AM/PM), `MinuteIncrement`, `Header`, `PlaceholderText`, and a `SelectedTimeChanged` event. The flyout offers hour, minute, and (in 12-hour mode) AM/PM columns with the same accept / cancel commit semantics as `DatePicker`. Shown on the gallery Forms page; covered by `ControlTests.TimePicker.cs`.
- `ColorPicker` - a color selector mirroring the WinUI 3 essentials: `Color` (defaults to opaque red, `#FFFF0000`), `PreviousColor`, `IsAlphaEnabled`, `IsColorSpectrumVisible`, `IsColorChannelTextInputVisible`, and a `ColorChanged` event carrying the old and new values. The surface combines a saturation/value spectrum at the selected hue, a hue slider, an optional alpha slider, current/previous swatches, and an optional hex input; hue, saturation, value, and alpha are the internal source of truth, so spectrum drags do not accumulate RGB round-trip drift. Shown on the gallery Forms page; covered by `ControlTests.ColorPicker.cs`.
- `CommandBarFlyout` and `AppBarButton` - a compact command strip mirroring the WinUI 3 control: a horizontal bar of primary `AppBarButton` commands (icon-only with the `Label` as a tooltip) plus an expandable overflow menu of secondary commands behind a "see more" button. It derives from `FlyoutBase`, so `ShowAt`, `Hide`, and the attached `FlyoutBase.AttachedFlyout` pattern apply, and invoking any command dismisses the flyout. Shown on the gallery Menus page; covered by `ControlTests.CommandBarFlyout.cs`.
- `BreadcrumbBar` and `BreadcrumbBarItem` - a navigation trail mirroring the WinUI 3 control: an `ItemsControl` of clickable crumbs separated by chevron glyphs, with the last crumb emphasized (primary brush, SemiBold, no trailing chevron) and updated automatically as the items collection changes. `ItemClicked` carries the clicked `Item` and `Index`; crumbs are keyboard-activatable tab stops. Width-constrained ellipsis collapse is a noted v1 omission. Shown on the gallery Navigation page; covered by `ControlTests.BreadcrumbBar.cs`.
- `PipsPager` - a compact page indicator mirroring the WinUI 3 control: clickable pip dots (neutral `ControlStrongFillColorDefault` fill per WinUI, the selected pip emphasized by its larger size), `NumberOfPages`, two-way `SelectedPageIndex` with bounds coercion, a sliding window of at most `MaxVisiblePips` dots, horizontal or vertical `Orientation`, previous/next chevron buttons governed by `PipsPagerButtonVisibility` (`Collapsed` default, `Visible`, or `VisibleOnPointerOver`), arrow-key navigation, and a `SelectedIndexChanged` event with old/new indices. Edge-scrolling viewport and pip scale animation are noted v1 omissions. Shown on the gallery Navigation page; covered by `ControlTests.PipsPager.cs`.

### Changed

- `ProgressBar` and `ProgressRing` were re-aligned to the WinUI 3 look, feel, and animation so they read as the platform controls. `ProgressBar` adopts the thin-track metrics (`MinHeight` 3 over a 1 px `TrackHeight`, indicator `CornerRadius` 1.5, track corner radius 0.5; the indicator host is pinned to `MinHeight` and centered so `Height` no longer thickens the bar), and its indeterminate state uses the canonical two-segment storyboard (40 percent and 60 percent segments on a shared 2.0 s forever cycle with KeySpline 0.4 0.0 0.6 1.0, the second segment delayed 0.75 s) with the determinate fill easing over 367 ms. `ProgressRing` replaces the previous caterpillar animation with the WinUI pulsing-arc-plus-rotation model (arc sweep 0 to 0.5 to 0 and a 90 to 1170 degree rotation, both over 2.0 s, forever), with the determinate tween over 367 ms and `MinHeight` / `MinWidth` set to 16.
- `ProgressBar` and `ProgressRing` now lead with the WinUI-orthogonal API: `ProgressBar.IsIndeterminate` (inherited) plus new `ShowError` / `ShowPaused`, and `ProgressRing.IsActive` / `IsIndeterminate` / `Value` plus new `ShowError` / `ShowPaused`. The previous `ProgressBar.ProgressMode` and `ProgressRing.ProgressState` enums are retained as backward-compatible one-way aliases that map onto the flags, so existing consumers keep working; `ProgressBar.StepProgress` (with `Steps` / `CurrentStep`) stays as a Fluence-only determinate sub-mode. Covered by the expanded `ControlTests.ProgressBar` and `ControlTests.ProgressRing` suites.
- `PasswordBox` indicators are now opt-in: `ShowCapsLockIndicator` and `ShowPasswordStrength` default to `false`, so a plain `PasswordBox` renders as a standard Fluent password field with no extra chrome. Set either property to `true` to restore the previous behavior; the gallery's Inputs sample and Forms sign-in sample opt in explicitly.

### Fixed

- Icons hosted in a control's `Icon` slot now render in the same color as the control's text and stay matched through visual states and theme switches (previously a `Button` icon kept its theme-primary color while accent button text stayed white, so switching between light and dark moved the icon but not the text). `Button`, `HyperlinkButton`, `ComboBox`, `TextBox`, `Card`, `TabViewItem`, `NavigationViewItem`, `MenuItem`, `InfoBar` (custom icons; the default severity icon stays severity-colored), and `AppBarButton` now forward the host's `Foreground` into the icon presenter through a presenter-scoped `FontIcon` style plus `TextElement.Foreground`, so icons track accent appearance, hover, pressed, selected, and disabled states as well as theme changes in lockstep with the label, while an explicit `Foreground` set directly on the icon element still wins. The disabled-state icon `Opacity` dim was removed in favor of the real disabled foreground brush, and semantic glyphs (check marks, chevrons, severity icons) keep their dedicated colors. Covered by `ControlTests.IconForeground.cs`.
- Icon-only `Button` and `HyperlinkButton` instances (an `Icon` with no `Content`) now center the glyph instead of reserving the 8 px icon-to-text gap, so glyph buttons such as the gallery's copy-to-clipboard buttons sit centered in their container. The gallery's source-code expander copy button also renders the copy glyph through the `Icon` slot like the Colors page (previously it pushed a raw glyph string through the text pipeline, which displayed the wrong character). Covered by `ControlTests.Button.cs`.

## [0.7.0-preview] - 2026-06-02

### Added

- PowerShell examples: `Fluence.Wpf.Demo.PowerShell` now ships four self-contained Windows PowerShell 5.1 scripts (`01-HelloWorld.ps1` backdrop cycle + rotating greeting, `02-ThemeAndAccent.ps1`, `03-ControlsTour.ps1`, `04-LoadXamlFile.ps1` loading XAML from disk), replacing the previous three demo scripts, plus a new `docs/powershell.md` guide (linked from the README, getting-started, and the docs map).
- Beginner documentation for the gallery demo: `Fluence.Wpf.Demo` gains a beginner-oriented README and XML-doc / inline comments across the shell and sample infrastructure (`App`, `MainWindow`, `DemoSampleControl`, `DemoSamplePageWiring`, `DemoNavigationCatalog`, and the reference pages), and its design-time resources now merge the computed `DesignTime.Light.xaml` so the XAML designer renders controls correctly.
- `Fluence.Wpf.Demo.Mvvm` now ships `Properties/DesignTimeResources.xaml` (the designer defaults to Dark via `DesignTime.Dark.xaml`), a design-time-creatable `d:DataContext`, and a seeded sample task list, so both the running app and the designer show realistic data.
- Design-time color + brush dictionaries for the XAML designer / Blend. Two generated, design-time-only files, `Properties/DesignTime.Light.xaml` and `Properties/DesignTime.Dark.xaml`, hold the complete computed palette (colors + brushes) for the default `#0078D4` accent, so control templates resolve their `*Brush` keys and render correctly at design time (previously only the raw Light *color* table was available, so brushes fell back to defaults and only Light was covered). `Properties/DesignTimeResources.xaml` now merges `DesignTime.Light.xaml` at slot [0] (mirroring the runtime [0] computed / [1] Typography / [2] Generic model); opt into Dark preview by merging `DesignTime.Dark.xaml` under the `d:` namespace on a specific window/page. The files are a serialized snapshot of `FluenceThemeEngine.BuildStandalone` output, guarded by `DesignTimeResources_AreCurrent` (CI fails if they drift from the engine) and regenerated via the maintainer-only `RegenerateDesignTimeResources` test. The snapshot is deterministic and machine-independent (default accent via the HSV ramp with no registry/DWM read, default-theme chrome colors, and no live-`SystemColors` aliases). Runtime behavior, `ApplicationThemeManager.Apply`, and the merge slots are unchanged; nothing merges these files at runtime. Covered by `Theming/DesignTimeResourceTests.cs`.
- `NavigationView.FooterMenuItems` - a pinned, selectable footer region mirroring WinUI 3 `NavigationView.FooterMenuItems`. Footer entries are full `NavigationViewItem`s that share the single-selection model and the sliding pill indicator with the main menu: they stretch to the pane width (full-width hover/selection) in the open `Left` pane, collapse to a centred icon in the closed `Left` / `LeftCompact` rail, and dock to the right in `Top` mode. Each pane template gains a `PART_FooterItemsHost` and a dedicated `PART_FooterSelectionIndicator`; selecting a footer item clears the main `SelectedItem` (and vice versa) so exactly one region is selected, and `SelectFooterMenuItem` selects one programmatically. `NavigationViewItem` now resolves its owning `NavigationView` by ancestor walk (`NavigationView.FromItemContainer`) so footer-hosted items invoke from mouse and keyboard. The automation peer reports the footer item via `ISelectionProvider.GetSelection`. The pre-existing `PaneFooter` free-content slot is unchanged. Covered by `ControlTests.NavigationViewFooter.cs`.
- Repo-owned XAML formatting. XAML Styler is pinned in `.config/dotnet-tools.json` and run against a committed reference style `Settings.XamlStyler` (attribute-per-line beyond two attributes, first attribute on a new line, 4-space indent, LF + single UTF-8 BOM). `eng/Format-Xaml.ps1` is the single entry point (format, `-Path` one file, or `-Check` for a non-destructive CI gate); a repo hook `.claude/hooks/post-tool-format-xaml.ps1` formats each edited `.xaml`, and `.github/workflows/build.yml` runs the check. Generated XAML is excluded: `Fluence.Wpf/Properties/DesignTime.*.xaml` and `**/Resources/fluence-wpf-banner-*.xaml`. This replaces reliance on an editor/plugin formatter and makes XAML style reproducible and enforced.

### Changed

- `FluenceWindow` and `TitleBar` were re-authored from scratch to WinUI-canonical metrics and internals. The public API surface (dependency properties, events, template parts, and CLR properties) is **unchanged** -- this is a drop-in re-author; existing XAML and code-behind compile and run without modification.
- `FluenceWindow.TitleBarHeight` default changed from 68 to 48, the WinUI 3 canonical expanded title-bar height (the compact variant is 32). The previous 68 added extra caption padding that diverged from the Fluent reference; consumers that relied on the old default get a tighter, on-spec title bar, and any consumer needing the old height can set `TitleBarHeight` explicitly. The gallery demo already sets `TitleBarHeight="42"` explicitly, so it is unaffected. `FluenceWindowTitleBarTests.TitleBarHeight_DefaultIs68` renamed to `TitleBarHeight_DefaultIs48` and asserts 48.
- `FluenceWindow` caption-button width changed from 64 px (minimize/maximize/restore) and 46 px (close) to a uniform 46 px for all four buttons, matching the WinUI 3 canonical hit-area width. The `CaptionButtonPanel` total width decreases from 158 px (3 x 54 px columns) to 138 px (3 x 46 px columns). Caption buttons now stretch to the full title-bar height instead of a fixed 32 px with `VerticalAlignment="Top"`, so the hover region fills the entire caption strip. No colour or glyph changes.
- `WindowButtonStyle` (minimize / maximize / restore caption buttons) hover and press fills switched to WinUI-canonical subtle fills. IsMouseOver: Background `SubtleFillColorSecondaryBrush`, Foreground `TextFillColorPrimaryBrush` (was `ControlStrongFillColorDefaultBrush` / `TextFillColorInverseBrush`). IsPressed: Background `SubtleFillColorTertiaryBrush`, Foreground `TextFillColorPrimaryBrush` (was `ControlStrongFillColorDisabledBrush` / `TextFillColorInverseBrush`). `WindowCloseButtonStyle` is unchanged.
- `TitleBar` glyph button (`TitleBarGlyphButtonStyle`) width changed from 42 px to 40 px, matching the WinUI 3 canonical back/pane-toggle button hit-area width. The 36 px explicit override on `PART_BackButton` is unchanged, so the back button total slot (button 36 px + left margin 6 px = 42 px) is unaffected. The pane-toggle button, which had no element-level override, now measures 40 px; `DemoMainWindowTests.MainWindow_ExtendedTitleBar_UsesHorizontalNavigationChrome` updated to assert 40 px. Corner radius (`CornerRadius={DynamicResource ControlCornerRadius}`) on the hover/press background was already present and is preserved. No colour, glyph, or token changes.
- `TitleBar` code-behind re-authored: clean BSD header, `#region` structure (DP declarations, static constructor, instance constructor, CLR events, CLR property wrappers, template application, protected virtual event raisers, DP changed callbacks, lifecycle, button click handlers, command state helpers, private fields), full `///` XML doc on every public member. No behaviour change; public contract is byte-for-byte identical.
- `NavigationView`: removed the divider line above the pane footer in the `Left` and `LeftCompact` templates, so the footer (e.g. the Settings item) sits directly in the pane. Also dropped the now-unused `BorderBrush` from `PaneFooterHost`.
- `WindowPolicy.CreateWindowChrome` no longer takes a `captionHeight` parameter. The caller always reset `CaptionHeight` to 0 immediately afterward, so the parameter never had effect; `CaptionHeight` is now hard-coded to 0 in the helper.
- One-time XAML normalization: 9 demo/PowerShell XAML files reformatted to the new `Settings.XamlStyler` reference style (`GalleryStatusPage`, `GalleryFormsPage`, `GalleryNavigationPage`, `GalleryTreesPage`, `GallerySettingsPage`, the PowerShell `MainWindow.xaml`, and small touches to `GalleryIconsPage` / `GalleryColorsPage` / `DemoSampleControl`). Formatting-only, behaviour-neutral; the control-template library and theme files were already conformant.
- Gallery demo: the **Settings** entry moved from `NavigationView.PaneFooter` into `NavigationView.FooterMenuItems`, so it now gets full-width hover, the shared selection indicator, and unified keyboard/mouse selection for free. This retired the demo's bespoke Settings handling (the `PreviewMouseLeftButtonDown` / `PreviewKeyDown` handlers, `NavigateToSettings`, manual `IsSelected` juggling, and the compact-width `UpdateSettingsNavigationItemDisplay`); footer navigation now flows through `NavigationView.ItemInvoked`. The empty space above the pinned footer is the gap, so no separator element is needed.
- Gallery demo: the `DemoControlGroupItemMargin` vertical spacing is now split top/bottom (`0,6,12,6`) instead of bottom-only (`0,0,12,12`), so a control group centered in a `DemoSampleControl` card (e.g. the Buttons page samples) sits equidistant from the card's top and bottom edges. Total spacing is unchanged.
- Gallery demo: decorative/content glyphs now use the accent *foreground* brush `AccentTextFillColorPrimaryBrush` for a consistent accent pop - Home page tile icons (previously the accent *fill* brush `AccentFillColorDefaultBrush`, a control-background role), the Data list leading person glyphs, the Tabs tab icons, the Menus context-menu item icons, and the ComboBox/TextBox leading icons on the Selection/Inputs pages. The accent-text brush is the correct foreground role for glyphs (it carries the per-theme contrast treatment, matching the Colors page guidance and the existing `GalleryDataBindingPage` icons). Button content icons are deliberately left to inherit their button's text brush (neutral, or accent on `HyperlinkButton`), per the existing `MainWindow_StandardDemoButtonIcons_UsePrimaryTextBrush` contract; stateful NavigationView item icons, the Settings config UI, the Icons font-reference catalog, and semantic status icons are likewise left as-is.
- Test-suite reliability and speed (no behavior change to the shipping library):
  - `ThemeParityTests` is now hermetic. `CaptureResolved` routes the rebuild through the deterministic-chrome path (`FluenceThemeEngine.SetDeterministicChromeForTesting`), so `Rebuilt_MatchesGoldenResolvedValues` no longer drifts on machines with the OS "show accent color on title bars" setting on (which would otherwise change the four machine-dependent title-bar / window-border chrome keys read from `HKCU` DWM `ColorPrevalence` / `AccentColor`). The golden snapshot already holds the machine-independent chrome values and is unchanged; the same values are covered by `DesignTimeResourceTests`.
  - The screenshot harness is now opt-in and trimmed to the documentation set. `GalleryScreenshotHarness` writes exactly ten PNGs under `docs/screenshots/`: the gallery shell in its three navigation modes (`gallery-home` / Left, `gallery-buttons` / LeftCompact, `gallery-status` / Top) plus the MVVM (`mvvm`) and PowerShell controls-tour (`powershell`) apps, each in Light and Dark. The PowerShell capture renders the inline XAML extracted from `03-ControlsTour.ps1`, so it stays in lock-step with the script. The three `Screenshots`-category capture tests skip (inconclusive) unless the `FLUENCE_CAPTURE_SCREENSHOTS` environment variable is set, so an ordinary `dotnet test` no longer overwrites the committed images; `.github/workflows/build.yml` also keeps `--filter "TestCategory!=Screenshots"` on both TFM test steps (TRX logging intact). Set `FLUENCE_CAPTURE_SCREENSHOTS=1` to regenerate. The previous banner-scale and per-gallery-route captures were removed.
  - `NavigationView` tests prefer condition-based waits. Eight fixed-duration `WaitForAnimationAndDrain` calls were replaced with the existing `WaitUntil` helper, polling the exact settled value the test then asserts (pane width, selection-indicator offset, footer visibility/width, or animation completion) with a generous timeout so they return as soon as the condition holds; sites with no cleanly observable settled value keep their fixed delay.
  - Test helpers consolidated into `WpfTestSta`. The single canonical `RunOnSta`, `DrainDispatcher`, and two explicitly named visual-tree walkers - `FindVisualDescendants` (visual only) and `FindLogicalAndVisualDescendants` (visual + logical, cycle-guarded) - now live in `WpfTestSta`; the per-fixture copies forward to them, preserving exact behavior (including `ExceptionDispatchInfo` rethrow semantics). The screenshot harness settle delay dropped from 260 ms to 150 ms, and the shared per-test teardown drain collapsed from three dispatcher pumps to one `ApplicationIdle` drain (which subsumes the higher priorities).

### Fixed

- `FluenceWindow` Windows 11 snap-layout flyout hover over the maximize/restore caption button now matches the normal mouse-hover fill. The snap-flyout hover is driven by `SetSnapHover` (the `WM_NCHITTEST`/`HTMAXBUTTON` path bypasses the XAML `IsMouseOver` trigger), which still referenced the pre-re-author strong-inverted tokens (`ControlStrongFillColorDefaultBrush` background / `TextFillColorInverseBrush` glyph) while `WindowButtonStyle` had migrated its PointerOver state to the WinUI subtle fills. `SetSnapHover` now references `SubtleFillColorSecondaryBrush` / `TextFillColorPrimaryBrush`, so snap-flyout hover and normal hover are visually identical. Covered by `FluenceWindowTitleBarTests.SetSnapHover_UsesSubtleFillTokens_MatchingTemplatePointerOver`.
- `NavigationView`: switching `PaneDisplayMode` between `Left` and `LeftCompact` (e.g. from the gallery Settings navigation-style picker) now animates the pane width with the same `GridLength` flight as the pane collapse/expand toggle, instead of snapping. `Left` and `LeftCompact` use different pane templates, so the switch swaps the template and its `PaneColumn`; `OnPaneDisplayModeChanged` captures the pre-swap width and the new template's `OnApplyTemplate` animates its fresh column from that width to the target (toggle easing/duration). Transitions to and from `Top` swap to a template with no pane column and continue to snap (there is no meaningful width flight). Covered by `NavigationView_PaneDisplayModeChange_AnimatesPaneWidth_LeftAndLeftCompact`.
- `ScrollBar` hover/expanded thickness reduced from 10 to 8 px (`ScrollBarSize`), so the conscious-scroll bar in the `NavigationView` pane item list and page content is thinner when hovered. The collapsed 6 px compact indicator (`ScrollBarCompactThumbSize`) is unchanged. `ScrollBar_VSM_MouseIndicator_*` and `ScrollBar_DefaultLayout_ReservesExpandedSlotWithCompactIndicator` updated to the 8 px expanded width.
- `NavigationView` `Top` pane mode: footer items (e.g. **Settings**) now render gear-only, the footer selection indicator centres under the selected footer item instead of snapping to the left edge, and the footer indicator animates in/out instead of hard-snapping. (1) Gear-only: a new internal inheritable attached flag `NavigationView.IsFooterItem` is set on `PART_FooterItemsHost` in the `Top` template only and inherits onto the footer items, where a template `Trigger` collapses the label column - so the rule is scoped to `Top` and leaves `Left` / `LeftCompact` footer labels (and all top-level item labels) untouched. (2) Indicator position: the footer indicator's coordinate host is now resolved as the footer `Grid` (parent of `PART_FooterItemsHost`), a common ancestor of both the indicator and the footer items, so `TransformToAncestor` succeeds; previously it resolved to the zero-size overlay `Canvas`, which is not an ancestor of the items, so the transform threw and the indicator fell back to offset `(0,0)`. (3) Animation: `PositionFooterIndicator` now fades and scales the footer indicator in on selection and out on deselection in `Top` mode (mirroring the main indicator's arrive/depart easing); `Left` / `LeftCompact` keep their historical snap. Covered by `ControlTests.NavigationViewTopFooter.cs`.
- `NavigationView`: the selection indicator now keeps tracking the selected item after the control is unloaded and reloaded (for example when a cached page is navigated away from and back to). `OnUnloaded` used to null the resolved template parts, and WPF does not re-run `OnApplyTemplate` on a plain reparent, so the indicator (and the back / pane-toggle / overflow chrome handlers) went dead on the second visit. `OnUnloaded` now preserves the template parts and only stops animations / detaches the external window watcher, and `OnLoaded` re-snaps the indicator to the current selection. Covered by `NavigationView_AfterUnloadReload_SelectionIndicatorStillUpdates`.
- `ProgressBar` default `TrackHeight` reduced from 6 to 4 px so the bar sits closer to the WinUI 3 reference (`ProgressBarMinHeight` = 3 over a 1 px track) and matches the control's own `TrackHeight` dependency-property default. The previous 6 px track read as 2 px too tall against the reference.
- `NavigationView`: the Settings (pane-footer) icon no longer drifts sideways while the left pane opens or collapses. The footer sits in a `ContentPresenter`, which sizes its child to content and centres it, whereas the menu items use a `StackPanel`, which stretches them. So the collapsed icon-only item slid across the pane as its width animated. Setting `HorizontalAlignment="Left"` on the footer `ContentPresenter` in the `Left` and `LeftCompact` templates keeps the icon in the same column as the menu icons. Covered by `DemoMainWindow_LeftPaneFooterIcon_StaysLeftAnchored_WhileCollapsed`.
- `FluenceWindow` immersive dark-mode now picks the right DWM attribute by OS build: attribute 19 (`DWMWA_USE_IMMERSIVE_DARK_MODE_OLD`) on Windows 10 builds 17763-18361 (1809), attribute 20 on build 18362+ (1903 and later, including Windows 11). The previous code used attribute 20 unconditionally, so the caption stayed light on early Windows 10 builds. Selection lives in the pure, testable `NativeMethods.GetImmersiveDarkModeAttribute(osBuild)`.
- `FluenceWindow` maximized windows now respect an auto-hidden taskbar. When a monitor's work area covers the full monitor (no reserved taskbar space) and a taskbar edge is set to auto-hide, the maximized rectangle is shifted 2 px on that edge in `WM_GETMINMAXINFO` so the taskbar still reveals on hover. New `NativeMethods.GetAutoHideTaskbarEdge` (via `SHAppBarMessage`) and the pure `NativeMethods.ApplyAutoHideTaskbarShift` back this.
- `FluenceWindow` no longer pins itself to the static theme managers when constructed but never shown. The `ApplicationThemeManager.Changed` and `ApplicationAccentColorManager.AccentColorChanged` subscriptions moved from the instance constructor to `OnSourceInitialized`, paired with the existing `OnClosed` unsubscribe. Only realised windows subscribe, and every subscription is released on close.
- `TreeView` outer border now clips to a `ControlCornerRadius` rounded corner (`ClipToBounds="True"`), so item hover highlights no longer paint past the rounded edge.
- `ProgressBar` indeterminate mode no longer renders square ends. The track now installs a rounded `RectangleGeometry` clip matching `CornerRadius`, kept in sync on size and layout changes, instead of relying on `ClipToBounds`, which clips only to the rectangular bounds. The translating indeterminate bars deliberately overshoot the track edges, so without the rounded clip their square mid-sections showed at the track boundary. They now follow the rounded track on every animation frame, matching the determinate fill.
- `NavigationView` left-pane item icons no longer shift horizontally when the pane collapses. The open-state `PART_PaneItemsScrollViewer` and `PaneFooterHost` left padding is now 0 (matching the collapsed state), so icons stay on a single vertical column - centered in the 48px compact rail - across open, collapsed, and compact, and are no longer clipped in the compact rail.
- `NavigationView` shared selection indicator now sits just inside the selected item's rounded border (horizontal offset 9, was 4) instead of floating in the pane to the left of the item, with no padding gap, in both expanded and compact states.
- `TitleBar` glyph buttons (back / pane toggle) no longer apply a legacy 4px rightward nudge to their glyph, so the extended-title-bar navigation chrome aligns with the NavigationView icon rail.
- Theme engine hardening from a code-review pass over the `FluenceThemeEngine` / `FluenceWindow` rewrite:
  - `AccentResolver` no longer lets a malformed accent source escape into the `Apply` hot path: the OS-palette branch now requires a length-7+ registry array before indexing it, and `GetDwmAccentOrDefault` additionally catches `EntryPointNotFoundException` / `DllNotFoundException` (raised when the undocumented `DwmGetColorizationParameters` ordinal `#127` is absent), falling back to default blue.
  - `FluenceThemeEngine.Publish` now `Insert`s the static Typography and Generic dictionaries at slots `[1]` and `[2]` (rather than appending them), so foreign dictionaries an application merges into `Application.Resources` are pushed to index 3+ and the `[0]/[1]/[2]` slot contract holds regardless of pre-existing entries.
  - `FluenceThemeEngine.ResetForTesting` re-seeds `CurrentPalette` with the default-blue ramp instead of the zero `AccentPalette`, so `ApplicationAccentColorManager.SystemAccentColor` (read by the `FluenceWindow` DWM border) can no longer be observed as `#00000000` between a reset and the next `Apply`.
  - `ColorMap` Windows 10/11 detection now uses the `RtlGetVersion`-based `OsVersionHelper` instead of `Environment.OSVersion`, which is shimmed/version-capped without a `supportedOS` manifest entry and would mis-detect Windows 11 as pre-22000 (applying the Win10 border-blend path incorrectly).
  - High-contrast `AccentFillBackdrop` Color token now derives from the live `SystemColors` window color (matching its brush twin) instead of a frozen `#202020` Dark-theme constant.
- `FluenceWindow` snap-layout caption hover now tracks theme/accent/high-contrast changes: `SetSnapHover` uses `SetResourceReference` for the hover background/foreground (instead of a one-time `TryFindResource` snapshot), and `ClearSnapHover` restores the template default with `ClearValue(BackgroundProperty)` rather than forcing a local `Transparent` brush.
- `FluenceWindow` no longer runs `ApplyFrame` twice per theme change. `OnThemeChanged` now only re-applies the backdrop; the frame (border brush + DWM border color) is already refreshed by `OnAccentColorChanged`, which the engine raises ahead of `ApplicationThemeManager.Changed` on every apply. Accent-only changes still drive `ApplyFrame`. The `Window.Background` brush built in `ApplyBackdrop` is now frozen, and a runtime flip of `ExtendsContentIntoTitleBar` now refreshes the caption-button layout.
- `FluenceWindow.HitTestTitleBar` skips the per-`WM_NCHITTEST` `InputHitTest` tree-walk when there is no custom `TitleBar` slot and content does not extend into the caption band (nothing interactive can be under the cursor there).
- Renamed `NativeMethods.ColorToAbgr` to `ColorToColorRef` and documented that it emits the `0x00BBGGRR` COLORREF layout DWM border attributes expect (the previous name implied ABGR).

### Removed

- `FluenceWindow` first-paint hold removed; the window now shows immediately on open. The gated software-rendering / no-composition layered-alpha guard (held the window invisible via `WS_EX_LAYERED` + `SetLayeredWindowAttributes(alpha 0)` once the chrome had collapsed the frame, then revealed it through a `CompositionTarget.Rendering` tick, the `OnContentRendered` override, and a 750 ms `DispatcherTimer` fallback, re-arming on restore-from-minimize) is gone, along with its `ShouldGuardFirstPaint` / `ArmFirstPaintReveal` / `RevealFirstPaint` members, the `OnContentRendered` override, the `WM_SYSCOMMAND`/`SC_RESTORE` and restore-from-minimize re-arm branches, and the now-unused `NativeMethods.SetWindowLayeredAlpha` helper with its `SetLayeredWindowAttributes` P/Invoke and the `GWL_EXSTYLE` / `WS_EX_LAYERED` / `LWA_ALPHA` / `SC_RESTORE` constants. The other first-paint protections stay in place: the redirection-surface clear (`HwndSource.CompositionTarget.BackgroundColor` matched to the content background), the glass frame following the effective backdrop, and the backdrop-composition gating; the window still never DWM-cloaks. `ShowThenDrain_LeavesFirstPaintGuardDisarmed` removed; `ShowThenDrain_NeverCloaksWindow` retained.
- `Themes/Shared.xaml` removed. Its three theme-independent Windows close-button brand colors (`WindowCloseButtonBackgroundPointerOver`, `...Pressed`, `WindowCloseButtonForegroundPointerOver`) are now seeded directly in C# by `BaseColorTables.AddSharedColors` instead of being read transiently from a standalone XAML table. Computed output is unchanged, so design-time snapshots and golden theme tests are unaffected; `FluenceWindowCloseButtonThemeTokens_*` updated to assert the in-code values.

## [0.6.0-preview] - 2026-05-24

### Changed

- Widened the accent ramp spread in `HsvColorHelper.GenerateAccentRampWinaccent`. The previous Candidate F calibration produced near-base stops only 4-7 % away from the base on the L axis, so adjacent ramp rungs were hard to tell apart in control templates. New deltas are ~10-12 % per adjacent step (Light1 +12 %, Light2 +24 %, Light3 +36 %, Dark1 -10 %, Dark2 -20 %, Dark3 -30 %), so controls that reference different rungs for hover / pressed / focus states now read as distinct. The decision to use the user-supplied base verbatim instead of mirroring the Windows perceptual projection still stands; this is purely a spread adjustment, not an OS-transform model.
- Demo `DemoSampleSourceExpanderStyle` background switched to `SolidBackgroundFillColorQuarternaryBrush` so the collapsed "Source code" header strip reads as a distinct dark band beneath the sample card (matches the WinUI Gallery visual reference).

### Added

- `Themes/Shared.xaml` - a new merge slot (`[5]`, loaded once, never replaced) holding theme-independent Color tokens that are identical across Light, Dark, and HighContrast. Currently holds the three Windows close-button brand reds (`WindowCloseButtonBackgroundPointerOver`, `...Pressed`, `WindowCloseButtonForegroundPointerOver`). Per-theme dictionaries no longer carry these keys. Slot count in `ApplicationThemeManager` is now 6; `DictionaryStabilityTests` updated accordingly. (Superseded since: the `FluenceThemeEngine` rebuild reduced the merge stack to 3 slots and made `Shared.xaml` a transient data read rather than a merge slot; `Shared.xaml` was later removed entirely, its tokens seeded in C# by `BaseColorTables`. See the `[Unreleased]` section.)

### Changed

- `NavigationView` sizing brought in line with WinUI 3: open pane width 280 -> 320 px (the WinUI 3 `NavigationViewOpenPaneLength`); `NavigationViewItem` `FontSize` 13 -> 14 (the body type-ramp value); `PaneFooter` slot gains a `DividerStrokeColorDefaultBrush` separator above its content in both Left and LeftCompact templates.
- `NavigationView` surface roles realigned to WinUI 3: the pane uses `AcrylicInAppFillColorDefaultBrush` (the `NavigationViewDefaultPaneBackground` value); the content host uses `LayerFillColorDefault` (dark `#4C3A3A3A`, light `#80FFFFFF`), a translucent layer brush over the DWM backdrop instead of the previous flat 65-69%-opaque Fluence-only tint. Mica still passes through both as the translucent layer they are meant to be, so cards composing on top sit above the surface as Fluent intends.
- `TitleBar` sizing: app-title text moved from `CaptionTextBlockStyle` (12 pt) to `BodyTextBlockStyle` (14 pt); app icon shrunk from 24 x 24 to 20 x 20 with 8 / 12 px margins (was 4 / 20).
- Extended the `AccentFillBackdrop` opaque sub-layer pattern from `ToggleSwitch` to every other control whose template applies an accent fill with sub-1.0 alpha (`AccentFillColorSecondary` 0.9, `AccentFillColorTertiary` 0.8, `AccentFillColorDisabled`): `Button`, `DropDownButton`, `ToggleButton`, `SplitButton` (per-half), `CheckBox`, `RadioButton`, and the `Slider` thumb. Hover / press / disabled accent fills now composite against a surface-matched solid (`AccentFillBackdropBrush`) instead of whatever translucent card or Mica surface sits beneath the control. This matches how Notepad and other native Windows 11 surfaces render.
- Demo gallery home page (`GalleryHomePage.xaml`) cards rewritten to the standard `Card.Header` / `Card.Icon` contract (matching `GalleryDataPage`'s `CardVariant` samples) instead of the previous nested-`StackPanel` reimplementation; card glyphs use `AccentFillColorDefaultBrush` (the saturated solid-accent role). The page-level `Background` setter on the hosting `SmoothScrollViewer` is removed so the `NavigationView` layer / Mica composition reaches the page.
- `SettingsRowTitleStyle` -> `BodyStrongTextBlockStyle` (14 pt SemiBold); `SettingsRowDescriptionStyle` -> `CaptionTextBlockStyle` (12 pt). Matches WinUI 3 `SettingsCard` text sizing.

### Fixed

- `ProgressBar` template: removed the dead `BorderThickness` style setter that did not affect the template; corrected the unfilled-track `Background` from `ControlStrokeColorDefaultBrush` (a stroke role) to `ControlStrongStrokeColorDefaultBrush` (the WinUI 3 fill role); changed the default `TrackHeight` from 4 px to 6 px and `CornerRadius` to 3 (a full pill at the new track height, matching the WinUI 3 Gallery). Resolves the two pre-existing failing `ProgressBar_*` tests; `ProgressBar_DefaultStyle_UsesThreePixelTrackHeight` renamed to `ProgressBar_DefaultStyle_UsesSixPixelTrackHeight`.
- `FluenceWindow` no longer forces `RenderOptions.ClearTypeHint=Enabled` at the window root. The WPF default (`Auto`) lets the renderer pick per surface: ClearType subpixel anti-aliasing on opaque surfaces, grayscale anti-aliasing on translucent ones (Mica / Acrylic, the `AccentFillBackdrop` layer, any other translucent compositing layer). Forcing `Enabled` overrode that fallback and produced visibly soft text at body / caption sizes whenever the parent surface was non-opaque, because ClearType subpixel rendering cannot blend correctly against a DWM-composited backdrop. The `.NET 10` WPF Fluent theme also leaves this at the default. `FluenceWindow_DefaultStyleOwnsCrispRootRenderingPolicy` updated to assert `ClearTypeHint.Auto`.

## [0.5.0] - 2026-05-21

- Initial release.
