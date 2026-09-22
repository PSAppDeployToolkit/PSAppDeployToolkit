# Demo Sample Pages

Referenced from `AGENTS.md` Section 14. Lifted out of the handbook to keep the always-loaded file lean. This is the standard the `demo-sample-page` skill and the demo-page tests enforce.

Control samples in `Fluence.Wpf.Demo` render through `DemoSampleControl`. Design reference pages that mirror WinUI Gallery catalog surfaces, such as Icons, may render directly when a trailing source expander would diverge from the reference. Typography is a `DemoSampleControl` sample because the Gallery's own type ramp is a `ControlExample` with source.

## Page skeleton

Every gallery page is a `Page`, as the WinUI Gallery's own pages are. WPF only lets a `Window` or a
`Frame` parent a `Page`, so the shell hosts them in the `Frame` named `PageFrame` inside
`NavigationView.Content`; `MainWindow` still owns the back stack and clears the frame's journal after
each navigation.

```text
Page
└── ScrollViewer
    └── StackPanel (page root)
        ├── GalleryPageHeader - Page name, Documentation/Toggle theme/Favorite actions [Title typography]
        ├── TextBlock        - Page description       [Body, secondary foreground]
        └── for each sample:
            └── DemoSampleControl
                ├── SampleDescription                [Body Strong]
                ├── DemoContent                      [live sample]
                ├── OutputContent                    [optional interaction result]
                ├── RightRailContent                 [optional options pane]
                └── Source expander                  [XAML and C# tabs]
```

Two pages depart from the scrolling stack, each following the Gallery page it mirrors. Icons and
Colors are Grids on `GalleryPageContentGridStyle`: the rows above the last one stay locked and only
the final star row scrolls. On Colors that means the header, the intro line, the brush snippet, and
the `SelectorBar` hold their place while the section content moves, as `ColorPage.xaml` does with its
own `ScrollViewer` in the last row.

`GalleryPageHeader` (`Fluence.Wpf.Demo/Pages/GalleryPageHeader.xaml(.cs)`) is the shared page header for every gallery page except Home, modelled on the WinUI 3 Gallery `Controls/PageHeader.xaml`. Home mirrors the WinUI Gallery home page, which has no page title header, and keeps its hero lockup instead. Set `Title` for the page name; set `DocsAnchor` to the matching `docs/controls.md` heading slug to show the Documentation button, or leave it empty to hide it (Colors and Settings have no matching section).

## Color layering

Demo sample surfaces use the native Fluence brush resources and control defaults directly. Do not add demo-only brush aliases, do not shadow color-key names with brush resources, and do not reintroduce a demo refresh layer for surface promotion.

| Layer                     | Brush resource                                                                                                |
| ------------------------- | ------------------------------------------------------------------------------------------------------------- |
| Page background           | Leave to `NavigationView` / `SmoothScrollViewer` control defaults unless a specific page has no host surface. |
| Sample card surface       | `SolidBackgroundFillColorBaseBrush` (WinUI Gallery `ControlExampleDisplayBrush`; opaque, so samples composite at token brightness) |
| Right rail / options pane | `CardBackgroundFillColorDefaultBrush` with a `DividerStrokeColorDefaultBrush` left divider (Gallery `OptionsPresenter`) |
| Expander header           | `CardBackgroundFillColorDefaultBrush`, one layer over the page (WinUI `ExpanderHeaderBackground`); the root paints no fill under it |
| Expander expanded content | `CardBackgroundFillColorSecondaryBrush` as the Expander `Background`, painted by the content tier (WinUI `ExpanderContent`); the code viewer is transparent so the code sits directly on that translucent layer, as in the Gallery |
| Secondary labels          | `TextFillColorSecondaryBrush`                                                                                 |

The page background has no dedicated brush (it uses the host control defaults), so the other five rows are the surface-token brushes that the Definition of done checks resolve across themes. The roles were measured against the installed WinUI 3 Gallery (2.9.3) on the Button page: plate 243 Light / 32 Dark, options rail 251 / 43, header 54 Dark over a 43 page, all reproduced by the brushes above.

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
- Source content uses a `SelectorBar` with `XAML` and `C#` items above a content host, as the WinUI Gallery `ControlExample` does. Each item carries its syntax-highlighted, copy-enabled RichTextBox viewer, owned by `DemoSampleControl`, on its `Tag`.
- Do not use or reintroduce legacy `Title`, `Description`, `SampleContent`, `ReplaceSourceLink(...)`, obsolete forwarding members, or source-link placeholder buttons.

Named live controls must not be declared directly inside `DemoSampleControl` property elements because WPF raises `MC3093`. Prefer page-owned hidden `ContentControl` slots plus `DemoSamplePageWiring.Apply(...)` from code-behind with typed `DemoSampleSource` registrations. The helper owns slot discovery, content transfer, source assignment, duplicate-slot detection, missing-source detection, and clearing the hidden slots after handoff. Catalog pages may stay outside `DemoSampleControl` when the WinUI Gallery reference itself is a direct catalog or guidance surface.

## Catalog surfaces

Icons and Accessibility are part of this standard for discrete demonstrations. Icons renders its catalog directly without a trailing source expander. Typography hosts its type ramp table in a `DemoSampleControl` with the ramp XAML as source, as the WinUI Gallery does.

Colors renders directly without `DemoSampleControl`, like Icons, because the WinUI Gallery Color page has no source expander. Its one-line brush snippet is a `DemoCodePresenter` (the Gallery `SampleCodePresenter` inline state: highlighted code, no card, optional copy button) and its section switcher is a `SelectorBar` whose sections are swapped through a `SlideNavigationPresenter`, as the Gallery switches its own Color sections, and the page is a Grid whose header, intro, snippet and switcher rows stay locked while only the section row scrolls. Its sections are `ColorPageExample` cards (`SolidBackgroundFillColorQuarternaryBrush` inside a 1 px `CardStrokeColorDefaultBrush` outline at `OverlayCornerRadius`, with the accent group on `AccentFillColorDefaultBrush` and the smoke group on `SmokeFillColorDefaultBrush`) followed by `ColorTile` rows on the Gallery `GalleryTileGridStyle` surface: `SolidBackgroundFillColorBaseBrush` with the same outline, each tile painted in the brush it names. Both controls live in `Fluence.Wpf.Demo/Pages` and are driven from the page code-behind data table; their sizes come from the `DemoColor*` tokens in `DemoSharedStyles.xaml`.

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
