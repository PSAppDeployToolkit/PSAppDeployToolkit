---
name: demo-sample-page
description: Scaffold or extend a Fluence.Wpf.Demo gallery sample page against the AGENTS.md section 14 DemoSampleControl contract. Use when adding a demo page or a discrete control sample to the gallery. Produces a page that uses DemoSampleControl correctly, the five surface-token brushes, the 8,8,0,0 plus 0,0,8,8 corner pattern, page-owned hidden ContentControl slots with DemoSamplePageWiring.Apply, and copy-enabled XAML and C# source tabs, with no inline literals.
disable-model-invocation: true
---

# Demo Sample Page

Scaffold a `Fluence.Wpf.Demo` sample page so it conforms to AGENTS.md section 14 the first time. This avoids the `MC3093` named-control trap and the inline-literal violations the definition of done forbids.

## Inputs

- **Page name and route tag** (for example `GalleryInputsPage`, tag `Inputs`).
- The **controls** to demonstrate and, per sample, the live XAML and the C# behind it.
- Whether the page is a discrete-sample page (uses `DemoSampleControl`) or a direct WinUI Gallery-style catalog/reference page (Typography, Icons, Accessibility), which may render directly and document the exception.

## Page skeleton

```
ScrollViewer
  StackPanel (page root)
    TextBlock  - Page name           [Title typography]
    TextBlock  - Page description    [Body, secondary foreground]
    for each sample:
      DemoSampleControl
        SampleDescription            [Body Strong]
        DemoContent                  [live sample]
        OutputContent                [optional interaction result]
        RightRailContent             [optional options pane]
        Source expander              [XAML and C# tabs]
```

## DemoSampleControl contract

Use only this public surface: `SampleDescription` (string), `XamlSource` (string), `CSharpSource` (string), `DemoContent` (object), `OutputContent` (object), `RightRailContent` (object). Do not use or reintroduce legacy `Title`, `Description`, `SampleContent`, `ReplaceSourceLink(...)`, obsolete forwarding members, or source-link placeholder buttons.

Composition:
- Outer card: sample card brush + card stroke brush, `CornerRadius="8,8,0,0"`.
- Demo region: `*, Auto` layout; output content lives inside the demo region, not the right rail.
- Right rail: collapses when empty, right-rail brush, `CornerRadius="0,8,0,0"`.
- Source expander: attached below the card, `CornerRadius="0,0,8,8"`, header `Source code`, source-header brush when collapsed, source-content brush when expanded, with a `TabControl` of `XAML` and `C#` tabs hosting the copy-enabled highlighted viewer.

## Named live controls (MC3093 avoidance)

Do not declare named live controls directly inside `DemoSampleControl` property elements. Instead:
1. Declare page-owned hidden `ContentControl` slots holding the live, named controls.
2. From code-behind, call `DemoSamplePageWiring.Apply(...)` with typed `DemoSampleSource` registrations. The helper owns slot discovery, content transfer, source assignment, duplicate-slot detection, missing-source detection, and clearing the hidden slots after handoff.

## Surface tokens (all via DynamicResource)

| Layer | Brush |
| --- | --- |
| Page background | leave to NavigationView / SmoothScrollViewer defaults unless the page has no host surface |
| Sample card surface | `CardBackgroundFillColorDefaultBrush` |
| Right rail / options pane | `CardBackgroundFillColorSecondaryBrush` |
| Expander header | `ControlFillColorDefaultBrush` |
| Expander expanded content | `SolidBackgroundFillColorBaseBrush` |
| Secondary labels | `TextFillColorSecondaryBrush` |

Do not add demo-only brush aliases, shadow color-key names with brush resources, or reintroduce a demo refresh layer.

## No inline literals

Page heading, description, sample description, card, and source spacing use the centralized demo resources. No inline `Margin`, `Padding`, `CornerRadius`, hex color, or font-size literals in sample page XAML. Right-rail options mutate the demo control through binding where the target property allows it; code-behind is acceptable only for command-style results such as click counters.

## Definition of done

- Every discrete control demonstration uses `DemoSampleControl`; any direct catalog/reference exception is documented in tests and docs.
- All five surface tokens resolve in Light, Dark, and High Contrast after runtime theme changes.
- Card and source expander corners follow `8,8,0,0` plus `0,0,8,8` with no visible seam.
- Source tabs show copy-enabled XAML and C# that match the visible sample.
- The page renders without binding errors or resource-resolution warnings in Light and Dark.
- `dotnet build Fluence.Wpf.sln -c Debug` and focused tests for the affected area pass with zero warnings.
- All XAML and C# saved UTF-8 with BOM, LF line endings. Stage, show diffs, wait for explicit commit instruction.
