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
