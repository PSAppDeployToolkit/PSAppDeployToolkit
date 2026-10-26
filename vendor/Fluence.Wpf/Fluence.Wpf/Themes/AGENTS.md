# Theme resource and template guidance

Read this file when editing XAML themes, color tables, typography resources, or control templates. For changes to palette calculation or resource publication, also read [Theming/AGENTS.md](../Theming/AGENTS.md).

## Resource structure

- Keep each control template in Themes/Controls/<ControlName>.xaml and merge it from Themes/Generic.xaml.
- The per-theme files under Themes/Colors/Theme.*.xaml contain color tokens only. BrushFactory creates the ordinary SolidColorBrush twin for each color. Use SpecialBrushes for gradients, High Contrast overrides, or brush-only exceptions.
- Use canonical WinUI-style resource keys. Keep the published key families and aliases documented in docs/theming.md.
- Use DynamicResource for values that must follow theme, accent, High Contrast, or runtime typography changes. Use StaticResource for immutable assets such as glyphs and fixed geometries.
- Production templates use palette resource keys instead of inline hexadecimal colors.

## Template behavior

- Turn off the default WPF focus rectangle and use the established Fluent focus tokens.
- Typical state changes use the existing 100 to 167 ms timing range and established easing resources. NavigationView selection travel and pane motion use their existing longer timings; preserve those contracts.
- Check the WPF visual tree and data bindings when translating a WinUI template. A string binding path that names an attached property with an XML namespace prefix can fail when loaded from Generic.xaml. Prefer TemplateBinding, a normal property trigger, or a code-built PropertyPath using the DependencyProperty object.
- Generated design-time XAML under Fluence.Wpf/Properties is emitted by DesignTimeResourceWriter. Do not edit it by hand; use the source dictionary and DesignTimeResources_AreCurrent test.

## Verification

Use .editorconfig for indentation, encoding, line endings, and whitespace. Run the repository text-policy check from CONTRIBUTING.md after editing text or XAML. For a visual change, verify Light, Dark, High Contrast, accent changes, and a supported backdrop in Fluence.Wpf.Demo. Include 100% and 150% DPI captures for material visual changes.
