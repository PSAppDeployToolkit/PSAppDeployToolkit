# Theme engine guidance

Read this file when changing theme resolution, system theme observation, accent selection, palette calculation, color publication, theme resources, or backdrop-related theme state.

## Publication pipeline

FluenceThemeEngine owns the pipeline. ApplicationThemeManager and ApplicationAccentColorManager are public facades.

1. ThemeResolver resolves Light, Dark, or High Contrast from the request and Windows settings.
2. AccentResolver resolves the active system palette or the pinned custom intent. A custom seed matching the Windows base can snapshot its seven system shades; other seeds use the in-tree generated ramp.
3. ColorMap combines the theme color table, accent-derived colors, theme-independent brand colors, and window title-bar colors.
4. PublishFingerprint compares the resolved theme and complete color map with the last published state. It also includes the live SystemColors inputs used by High Contrast brushes and the Settings transparency-effects flag.
5. BrushFactory builds a fresh dictionary containing every color token and its brush twin. SpecialBrushes adds gradient, High Contrast, and brush-only exceptions.
6. The engine replaces MergedDictionaries slot zero with the new dictionary, then raises the publication event.

The gate suppresses duplicate Windows broadcasts. If an input changes published output, add it to the fingerprint. A failed publish with no Application.Current must not record a fingerprint.

## Resource slots and invariants

After Apply, three Fluence-owned dictionaries occupy slots 0-2 in this order. Foreign dictionaries may follow at slot 3 and beyond; repair preserves their relative order and identity:

| Slot | Content | Lifetime |
| --- | --- | --- |
| 0 | Computed colors and brushes | Rebuilt and replaced on theme or accent changes |
| 1 | Themes/Typography/Typography.xaml | Loaded once |
| 2 | Themes/Generic.xaml | Loaded once |

Never mutate, promote, or copy computed tokens into other dictionaries. A stale computed dictionary later in merge order can shadow the newly published values. DictionaryStabilityTests owns the slot invariant; update that test when the contract changes. Theme color tables are color-only. BrushFactory supplies ordinary brush twins; add special brushes in SpecialBrushes.

## Accent intent

- System accent is the default. Apply(theme) resolves the current OS palette without requiring a separate ApplySystemAccent call.
- ApplyCustomAccent(Color) and ApplyCustomAccent(Color light, Color dark) use the calculated WinUI-style palette by default. A matching Windows base seed snapshots the Windows shades; an arbitrary seed uses the in-tree approximation.
- ApplyCustomAccent(Color color, bool exact = false) with exact: true pins the visible default fill to the supplied Light value and derives Dark from the resolved Dark palette's Light2 shade.
- ApplyCustomAccent(Color light, Color dark, bool exact = false) with exact: true pins the visible default fill to each supplied value for its theme, even when those values are equal. The ApplyCustomAccentExact overloads remain compatibility aliases.
- Other accent roles continue to come from the resolved palette. High Contrast control roles use live SystemColors. ApplySystemAccent resets the sticky intent to System.

Do not change these intent distinctions when refactoring palette resolution. Add tests that assert the published resources, not only the raw seed or helper result.

## High Contrast and transitions

High Contrast brushes are built from live SystemColors on each apply. Those SystemColors inputs participate in the publish fingerprint, so a real High Contrast variant change publishes and duplicate broadcasts do not.

After initialization, PreparingPublish lets realized FluenceWindow instances capture the WPF surface before slot zero is replaced. The snapshot fades out over 167 ms after synchronous publication handlers complete. High Contrast entry or exit, reduced motion, minimized or unrealized windows, failed captures, and snapshots above 16 million pixels skip the transition. Native DWM backdrop pixels are not present in a WPF snapshot.

## API and documentation

ApplicationThemeManager.Apply records the caller's requested theme and backdrop even when the publication gate skips work. Changed fires when resources publish or the requested theme or backdrop changes. A backdrop-only change can therefore be observable without rebuilding the dictionary.

AccentColorChanged fires only when an accent apply publishes a changed map. Reapplying an identical intent is a no-op; event tests must cause an actual transition.

SystemThemeWatcher debounces Windows settings messages. Tests should expect one logical change, not multiple notifications from one OS action.

Keep the canonical color and brush families in docs/theming.md. Update DictionaryStabilityTests and the relevant resource tests whenever publication structure or token behavior changes.
