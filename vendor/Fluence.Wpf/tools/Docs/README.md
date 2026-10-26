# Source documentation generation and local sync

This repository owns the generated C# API pages in `docs/api/`, the generated PowerShell function pages in `docs/powershell/reference/`, the authored PowerShell reference introduction/type pages, and the authored `docs/controls.md`, `docs/theming.md`, and `docs/winui-parity.md`. The website repository owns its other documentation, navigation, and publication.

From a Windows checkout with .NET 10 and PowerShell 7:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release -f net10.0-windows10.0.26100.0
pwsh -NoProfile -File tools/ApiDocs/Generate-ApiDocs.ps1 -Configuration Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1 -Configuration Release -Build
Install-Module Alt3.Docusaurus.Powershell -RequiredVersion 2.0.1 -Scope CurrentUser
pwsh -NoProfile -File tools/Docs/Generate-PowerShellReference.ps1
pwsh -NoProfile -File tools/Docs/Sync-WebsiteDocs.ps1
```

`Sync-WebsiteDocs.ps1` defaults to `../Fluence.Wpf.Website/docs` relative to this repository. Pass `-WebsiteDocsPath` to target another website checkout. The script copies only `api/`, PowerShell function `.mdx` pages, the three authored PowerShell reference pages, and the three authored top-level pages named above. It removes obsolete generated API pages and old PowerShell function `.md` pages from the website checkout. Review both repositories' diffs before committing.

The API generator uses the compiled assembly and XML comments. The PowerShell generator uses Alt3.Docusaurus.Powershell 2.0.1 and verifies that it produced one `.mdx` page for every function exported by the module manifest before replacing existing function pages.

After building Release API output and staging the PowerShell module, check for generated-page drift without changing repository files:

```powershell
pwsh -NoProfile -File tools/ApiDocs/Generate-ApiDocs.ps1 -Configuration Release -Check
pwsh -NoProfile -File tools/Docs/Generate-PowerShellReference.ps1 -Check
```

Both checks run in CI. The PowerShell check compares the writer's exact UTF-8 BOM and LF bytes, reports changed or missing exported command pages, and rejects obsolete function MDX or legacy function Markdown pages. The three authored reference pages remain outside this check. When a check reports drift, run the matching generator without `-Check` and review the generated diff. The main-only generation workflow retains its existing write and sync behavior.

The [docs workflow](../../.github/workflows/docs-sync.yml) runs on every commit pushed to `main` and can be started manually from `main`. It regenerates the C# API and PowerShell function pages and commits changed generated pages, including API coverage, to this repository. The website steps are disabled while the standalone GitHub repository is being prepared. Source generation does not require a website token.

Once `sintaxasn/Fluence.Wpf.Website` has an initial commit on `main`, create the source repository secret `WEBSITE_DOCS_TOKEN` with fine-grained Contents read/write access to that website repository. Set the source repository Actions variable `WEBSITE_DOCS_READY` to `true`, then start the docs workflow manually once to perform the first sync. When enabled, the workflow copies the selected source-owned pages, runs the website documentation transform check, type check, and production build, then pushes changed documentation. These steps do not publish the website or release a NuGet or PowerShell package.

The Alt3 2.0.1 generator produced all 16 exported function `.mdx` pages locally on September 29, 2026. The copied pages were included in a successful local Docusaurus production build on September 29, 2026.
