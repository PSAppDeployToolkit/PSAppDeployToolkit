---
name: demo-sample-page
description: Scaffold or extend a Fluence.Wpf.Demo gallery sample page against the DemoSampleControl contract in this skill's SPEC.md. Use when adding a demo page or a discrete control sample to the gallery. Produces a page with GalleryPageHeader, DemoSampleControl samples wired through page-owned hidden ContentControl slots and DemoSamplePageWiring.Apply, the surface-token brushes and corner pattern, copy-enabled XAML and C# source, no inline literals, and every catalog and test registration.
disable-model-invocation: true
---

# Demo Sample Page

Scaffold a `Fluence.Wpf.Demo` sample page that meets the gallery standard on the first pass. [SPEC.md](SPEC.md) is the single source for the page skeleton, surface brushes, the `DemoSampleControl` contract, catalog integration, and the definition of done. This file orders the work and does not repeat those values, so read SPEC.md before you write XAML. A copied token table drifted from SPEC.md once already.

Also read [Fluence.Wpf.Demo/AGENTS.md](../../../Fluence.Wpf.Demo/AGENTS.md) for the shell and route contract.

## Inputs

- **Page name and route tag**, for example `GalleryInputsPage` with the tag `inputs`.
- The **controls** to demonstrate and, for each sample, the live XAML and the C# behind it.
- Whether the page uses `DemoSampleControl` samples, or is a direct catalog or reference page such as Icons or Colors. A direct page must document its exception, as SPEC.md describes.
- The `docs/controls.md` heading that the page header's Documentation button should open.

## Steps

1. Copy the closest existing `Fluence.Wpf.Demo/Pages/Gallery*Page.xaml(.cs)` that matches the page shape. Then apply the skeleton and the `DemoSampleControl` contract from SPEC.md.
2. Do not declare named live controls inside `DemoSampleControl` property elements, because WPF raises `MC3093`. Put them in page-owned hidden `ContentControl` slots, and call `DemoSamplePageWiring.Apply(...)` from code-behind with typed `DemoSampleSource` registrations.
3. Use only the brushes in the SPEC.md color layering table, through `DynamicResource`. Take spacing, corner radius, and typography from the centralized demo resources in `Resources/DemoSharedStyles.xaml`. Do not add inline `Margin`, `Padding`, `CornerRadius`, hex color, or font-size literals.
4. Make each XAML and C# source string match the visible sample.
5. Register the page in every place that the SPEC.md catalog integration section lists.
6. Meet the SPEC.md definition of done. Then run `dotnet build Fluence.Wpf.sln -c Debug`, the gallery tests for the page, and `pwsh .claude/hooks/post-tool-util.ps1 -CheckAll`.

Keep XAML and C# in UTF-8 with BOM and LF line endings. Show the diff and wait for the user's explicit commit instruction.
