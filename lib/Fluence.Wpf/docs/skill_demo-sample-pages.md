# Demo Sample Pages

Referenced from `AGENTS.md` Section 14. Lifted out of the handbook to keep the always-loaded file lean. This is the standard the `demo-sample-page` skill and the demo-page tests enforce.

Control samples in `Fluence.Wpf.Demo` render through `DemoSampleControl`. Design reference pages that mirror WinUI Gallery catalog surfaces, such as Typography, may render directly when a trailing source expander would diverge from the reference.

## Page skeleton

```text
ScrollViewer
└── StackPanel (page root)
    ├── TextBlock        - Page name              [Title typography]
    ├── TextBlock        - Page description       [Body, secondary foreground]
    └── for each sample:
        └── DemoSampleControl
            ├── SampleDescription                [Body Strong]
            ├── DemoContent                      [live sample]
            ├── OutputContent                    [optional interaction result]
            ├── RightRailContent                 [optional options pane]
            └── Source expander                  [XAML and C# tabs]
```

## Color layering

Demo sample surfaces use the native Fluence brush resources and control defaults directly. Do not add demo-only brush aliases, do not shadow color-key names with brush resources, and do not reintroduce a demo refresh layer for surface promotion.

| Layer | Brush resource |
| --- | --- |
| Page background | Leave to `NavigationView` / `SmoothScrollViewer` control defaults unless a specific page has no host surface. |
| Sample card surface | `CardBackgroundFillColorDefaultBrush` |
| Right rail / options pane | `CardBackgroundFillColorSecondaryBrush` |
| Expander header | `ControlFillColorDefaultBrush` |
| Expander expanded content | `SolidBackgroundFillColorBaseBrush` |
| Secondary labels | `TextFillColorSecondaryBrush` |

The page background has no dedicated brush (it uses the host control defaults), so the other five rows are the surface-token brushes that the Definition of done checks resolve across themes.

Use `DynamicResource` for these role brushes so theme, accent, and high-contrast changes flow through the standard `ApplicationThemeManager` slots.

## DemoSampleControl contract

`DemoSampleControl` is the only reusable surface for demo samples. Its public surface is intentionally small:

- `SampleDescription` (`string`) renders bold text above the sample card.
- `XamlSource` (`string`) supplies the XAML source tab.
- `CSharpSource` (`string`) supplies the C# source tab.
- `DemoContent` (`object`) hosts the live control region.
- `OutputContent` (`object`) optionally hosts interaction results.
- `RightRailContent` (`object`) optionally hosts property toggles and options.

Composition requirements:

- Outer card uses the sample card brush, card stroke brush, and `CornerRadius="8,8,0,0"`.
- Demo region uses a `*, Auto` layout. Output content lives inside the demo region, not the right rail.
- Right rail collapses when empty, uses the right-rail brush, and keeps `CornerRadius="0,8,0,0"`.
- Source expander is attached below the card with `CornerRadius="0,0,8,8"`, header text `Source code`, the source-header brush when collapsed, and the source-content brush when expanded.
- Source content uses a `TabControl` with `XAML` and `C#` tabs. Each tab hosts the syntax-highlighted, copy-enabled RichTextBox viewer owned by `DemoSampleControl`.
- Do not use or reintroduce legacy `Title`, `Description`, `SampleContent`, `ReplaceSourceLink(...)`, obsolete forwarding members, or source-link placeholder buttons.

Named live controls must not be declared directly inside `DemoSampleControl` property elements because WPF raises `MC3093`. Prefer page-owned hidden `ContentControl` slots plus `DemoSamplePageWiring.Apply(...)` from code-behind with typed `DemoSampleSource` registrations. The helper owns slot discovery, content transfer, source assignment, duplicate-slot detection, missing-source detection, and clearing the hidden slots after handoff. Catalog pages may stay outside `DemoSampleControl` when the WinUI Gallery reference itself is a direct catalog or guidance surface.

## Catalog surfaces

Icons and Accessibility are part of this standard for discrete demonstrations. Typography is a direct WinUI Gallery-style reference page and does not add a trailing source expander.

## Definition of done

A new or updated sample page is done only when:

- Every discrete control demonstration uses `DemoSampleControl`; direct catalog/reference pages document their exception in tests and docs.
- All five surface-token brushes (the brush rows in the Color layering table) resolve in Light, Dark, and High Contrast after runtime theme changes.
- Card and source expander corners follow the `8,8,0,0` plus `0,0,8,8` pattern with no visible seam artifact.
- The source expander shows copy-enabled XAML and C# tabs that match the visible sample.
- Page heading, description, sample description, card, and source spacing use centralized demo resources. No inline `Margin`, `Padding`, `CornerRadius`, hex color, or font-size literals in sample page XAML.
- Right-rail options mutate the demo control through binding where the target property allows it. Code-behind is acceptable for command-style results such as click counters.
- The page renders without binding errors or resource-resolution warnings in Light and Dark.
- `dotnet build Fluence.Wpf.sln -c Debug` and focused tests for the affected area pass with zero warnings.
