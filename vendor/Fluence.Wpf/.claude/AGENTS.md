# In-repository AI tooling

This file applies to changes under .claude/. Use only the specialist lane whose stated trigger matches the work.

## Review and scaffold agents

| Agent | Trigger |
| --- | --- |
| theme-slot-auditor | After a theme, brush, color, accent, or ApplicationThemeManager change |
| winui-parity-reviewer | When comparing a control template or behavior with WinUI references |
| net472-feasibility-checker | After adding an API, language feature, or dependency that may affect net472 |
| documentation-updater | After code-driven documentation drift or edits to repository README, CHANGELOG, generated references, or authored control/theming/parity guides |

## Scaffolding skills

- new-control scaffolds a public control against [Fluence.Wpf/Controls/AGENTS.md](../Fluence.Wpf/Controls/AGENTS.md) and the required tests, baselines, gallery registrations, PowerShell module surface, screenshots, and documentation. Its references/powershell-module.md covers the controls tour example, new prompt input types, and new public functions.
- demo-sample-page scaffolds a gallery page. Its SPEC.md defines the sample control contract, page layout, catalog integration, and completion checks.

Skills and agents order the work and name files; they link to the owning AGENTS.md, SPEC.md, or README for rules and values instead of copying them. A copied value drifts from its owner.

## Hooks

- pre-tool-theme-slot.ps1 gives a non-blocking reminder for theme-slot-critical edits.
- post-tool-util.ps1 enforces text policy when invoked by the editor and supports the repository-wide CI check with -CheckAll. For agent guidance (.claude/, every AGENTS.md, skills/, .github/copilot-instructions.md, and the pull request template) it also checks skill and agent frontmatter and rejects numbered handbook section references. In every Markdown file except CHANGELOG.md, a reference to a numbered section of another Markdown file must match a heading with that number.

Keep authoring and review in separate passes. A scaffold is not an independent review.

## Rules and their enforcement

When a correction repeats a rule below, move its enforcement up a row: from guidance to a test or check. Remove a rule once its mistake can no longer happen.

| Rule | Enforced by |
| --- | --- |
| Skill entrypoints and agent definitions start with frontmatter that has `name:` and `description:` | post-tool-util.ps1 (edit hook and CI) |
| Guidance names the owning file, never a numbered handbook section | post-tool-util.ps1 (edit hook and CI) |
| A numbered section reference to another Markdown file matches a numbered heading there | post-tool-util.ps1 (edit hook and CI) |
| UTF-8 BOM, LF, no em or en dashes, banned APIs, no hex in control templates | post-tool-util.ps1 (edit hook and CI) |
| Generated design-time XAML is regenerated, not edited | DesignTimeResourceTests.DesignTimeResources_AreCurrentAsync |
| New public symbols are declared | Public API analyzer through PublicAPI.Unshipped.txt (build) |
| Test cases are not lost | CI lane floors in .github/workflows/build.yml and Fluence.Wpf.Tests/Baselines |
| Gallery routes resolve to their pages; header anchors match doc headings | DemoShellTests.MainWindow_DirectNavigation_LoadsConcretePagesAsync over DemoNavigationCatalog; GalleryPageHeaderTests |
| Get-FluenceInput offers only New-FluencePrompt input types | Get-FluenceInput.Tests.ps1 |
| Exported commands match FunctionsToExport | Import-FluenceModule.Tests.ps1; Sync-WebsiteDocs.ps1 page count |
| Every input type has a New-FluenceInputControl branch, docs row, and count | InputTypeParity.Tests.ps1 |
| Gallery pages are registered once, in DemoNavigationCatalog | GalleryPageCatalog feeds the catalog to DemoShellTests.GalleryPages_UseSharedWinUiGalleryPageLayoutAsync, MainWindow_DirectNavigation_LoadsConcretePagesAsync, MainWindow_NonHomePagesExposeInlineSourceSamplesAsync, and DemoSampleContractTests.GallerySamplePages_AllVisibleDemoSamplesExposeSourceAsync |
| Skills link to owner guidance instead of copying values | Guidance only: this file |
