# Known issues and follow-ups

This file tracks optional follow-ups and deliberate non-features. Filed bugs with
reproductions live on the issue tracker; this is the consolidated view for
maintainers.

## Current follow-ups (not defects)

- **`TabView` drag-to-reorder** - `TabView` / `TabViewItem` ship with closable
  tabs, an add-tab button, per-tab icons, overflow scroll, and width / overlay
  modes. Drag-and-drop tab reordering (including cross-window tear-off) is **not**
  implemented; consumers that need it should handle `PreviewMouseMove` / drag-drop
  themselves. This is the main remaining gap vs. WinUI 3 `TabView`.
- **Navigation back-stack** - `NavigationView.IsBackButtonVisible` +
  `IsBackEnabled` + `BackRequested` are exposed, but the library does **not**
  track page history. The demo does not use the back button; consumers are
  expected to own their own back stack and route `BackRequested`.
- **`RenderTargetBitmap` vs DWM backdrop** - DWM Mica / Acrylic is composed by
  the window manager and is **not** visible to `RenderTargetBitmap`. The
  screenshot harness hosts the gallery inside a plain `Window` with a solid
  `SolidBackgroundFillColorBaseBrush`. Automated capture of the full
  `FluenceWindow` chrome needs a different approach (e.g. `PrintWindow` /
  GDI screen capture).
- **`DatePicker` / `TimePicker` selector flyouts** - the flyouts present plain
  scrollable selector lists. They do **not** implement WinUI's infinitely
  looping selectors, nor the WinUI centered accent highlight band with the
  foreground flip over the selected row (`DatePicker_themeresources.xaml`
  `HighlightRect` / `MonochromaticOverlayPresenter`). The highlight band is
  coupled to the looping-selector interaction model, so both are deferred
  together; the looping omission is already noted in code at `DatePicker.cs`
  (around line 606) and `TimePicker.cs` (around line 511).
- **`ColorPicker` spectrum permutations and layout options** - the picker now
  carries the WinUI gallery-default option surface (preview, color slider, hex,
  More/Less toggle, alpha slider/text, and the RGB/HSV channel text inputs),
  but `ColorSpectrumShape` (the Ring spectrum), the `ColorSpectrumComponents`
  permutations, `Orientation`, and the Min/Max channel range properties remain
  deliberately omitted; the spectrum is fixed to saturation (x) by value (y)
  with hue as the third-dimension slider. Two deviations from WinUI: the hex
  input commits on Enter / focus loss rather than live per keystroke, and the
  hue text input accepts 0-360 (WinUI caps at 359) because the picker's model
  and slider use 360 inclusive.
- **`ContentDialog` smoke layer and motion** - the dialog always paints its
  smoke (dimming) layer; there is **no** WinUI `DialogShowingWithoutSmokeLayer`
  state. It also has **no** `FullDialogSizing` stretch mode and **no** exit
  (`DialogHidden`) reverse animation; the entrance motion is implemented.
- **`BreadcrumbBar` ellipsis overflow** - the bar does **not** collapse leading
  crumbs into an ellipsis (WinUI collapses them into an `E712` ellipsis item
  with a flyout). Long trails extend to their natural width and clip when
  constrained.
- **`PipsPager` scrolling and nav-button scale** - the pager uses a centered
  re-rendering window (already noted in code at `PipsPager.cs` around lines
  65-70). It does **not** implement WinUI's edge-pip scale-down or the
  stationary edge-scrolling viewport, and the navigation buttons do **not** use
  WinUI's pressed `0.875` scale.
- **`NavigationView` Top-overflow synchronous `UpdateLayout()`** - `NavigationView.cs`
  (`UpdateTopOverflow`, around line 1796) calls `UpdateLayout()` synchronously to
  force a fresh measure/arrange pass before measuring `_topItemsHost.ActualWidth`.
  In `PaneDisplayMode="Top"` with a large item set this forces a full layout pass
  on every resize. This is a jank-only cost, not a correctness defect, and is
  deferred because reworking the layout path to avoid the forced pass risks
  regressions in overflow placement for a gain that only shows up under heavy
  resize with many top-level items.

## net472 accessibility API gaps

The following Windows Presentation Foundation accessibility APIs were introduced
in .NET Framework 4.8 and are **not available on the `net472` TFM** this library
supports. Each entry documents the chosen fallback and why the gap is acceptable.
Reference: <https://learn.microsoft.com/dotnet/framework/whats-new/whats-new-in-accessibility>

- **`AutomationPeer.RaiseNotificationEvent`** (available from .NET Framework 4.8) - this
  API pushes an ad-hoc text announcement to assistive technologies without a
  corresponding UI Automation element. All live-region controls in this library
  (`InfoBar`, `ProgressBar`, `ProgressRing`, `TeachingTip`, and `TextBox`
  validation) use the net472-safe substitute: the element sets
  `AutomationProperties.LiveSetting` to `Polite` or `Assertive` in its template
  or peer constructor, and the peer calls
  `RaiseAutomationEvent(AutomationEvents.LiveRegionChanged)` when state changes.
  Screen readers that honour `LiveRegionChanged` (Narrator, NVDA, JAWS) announce
  the current `GetNameCore` text of the peer on that event, which is equivalent
  for the controlled-status use cases in this library.

- **`AutomationProperties.IsDialog`** (available from .NET Framework 4.8) - this
  property marks an element as a modal dialog surface so screen readers announce
  it as such when focus enters. `ContentDialog` does not set this property on
  net472. The fallback used is: the `ContentDialogAutomationPeer` returns
  `AutomationControlType.Window` from `GetAutomationControlTypeCore`, the dialog
  traps Tab focus inside its bounds during `ShowAsync`, and on open it declares
  an assertive UI Automation live region (`AutomationProperties.LiveSetting`) and
  raises `LiveRegionChanged` so Narrator, NVDA, and JAWS read the dialog `Title`
  as it appears. Assistive technologies therefore observe a Window-role boundary,
  focus containment, and an explicit open announcement, which together characterise
  a modal dialog. The behaviour gap is limited to the literal "dialog" role phrase
  that Narrator and JAWS emit when `IsDialog=true`; the structural, focus, and
  announcement semantics are present. Without the live region the overlay-hosted
  dialog (not a separate HWND) raised no event for assistive technologies to act
  on, so it was not read on open.

- **`AutomationProperties.HeadingLevel`** (available from .NET Framework 4.8) - this
  property allows elements to be reported as heading levels H1-H9 to assistive
  technologies, enabling document-style navigation with Narrator's heading-scan
  mode. Fluence controls do not use heading levels internally; applications
  consuming the library on net10.0-windows10.0.26100.0 may set this property
  freely. On net472 the property is absent and any XAML that references it will
  fail to compile unless guarded. The gap is acceptable because Fluence is a
  controls library, not a document renderer; section headings in consuming
  applications are app-layer concerns.

- **Automatic `PositionInSet` and `SizeOfSet` for `ItemsControl`** (available from
  .NET Framework 4.8) - on 4.8+ WPF automatically computes and exposes
  `PositionInSet` and `SizeOfSet` UI Automation properties for items inside an
  `ItemsControl`, so screen readers can announce "item 2 of 5" without explicit
  annotation. On net472 these values are not computed automatically. Fluence's
  automation peers do not currently override `GetPositionInSetCore` /
  `GetSizeOfSetCore`, so set position is not annotated explicitly on either TFM;
  on net472, controls such as `NavigationViewItem` inside a `NavigationView`,
  `TabViewItem` inside a `TabView`, and `PipsPager` dots therefore do not
  announce set position, and the application-item controls (`ListBox`,
  `ListView`, `TreeView`, `ComboBox`) rely solely on the 4.8+ automatic
  computation. Applications that require position announcements on net472 (or for
  any control) should set `AutomationProperties.PositionInSet` and
  `AutomationProperties.SizeOfSet` explicitly on each item in XAML or code.

## Deferred runtime test coverage

The following accessibility items are XAML-verified (the names and parts exist in
the committed templates) but do not have automated runtime interaction tests
because their rendering depends on host shell state that is difficult to
reproduce in the headless test harness:

- **`TeachingTip` `PART_AlternateCloseButton`** - the alternate close button lives
  inside a `Popup` subtree that is only in the visual tree while the tip is
  open and the primary close button is hidden. Its `AutomationProperties.Name`
  is verified by inspection of `TeachingTip.xaml`; an automated test would
  require the popup to be open, the primary close hidden, and Narrator focus
  routed into the popup subtree.

- **`TabView` scroll buttons** (`PART_ScrollDecreaseButton`, `PART_ScrollIncreaseButton`) -
  these buttons appear only when the tab strip overflows its container. Their
  `AutomationProperties.Name` values are verified by inspection of `TabView.xaml`;
  an automated test would require a `TabView` with enough tabs to trigger
  overflow in a measured layout pass, which the current STA test infrastructure
  does not size windows to guarantee.
