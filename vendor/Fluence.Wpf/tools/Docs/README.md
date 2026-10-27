# Source documentation generation and website sync

This repository owns the C# API and PowerShell reference generators, the authored PowerShell reference introduction/type pages, and the authored `docs/controls.md`, `docs/theming.md`, and `docs/winui-parity.md`. Generated pages and capture candidates are local, ignored files under `artifacts/docs/`. The [website repository](https://github.com/sintaxasn/Fluence.Wpf.Website) owns the tracked published generated pages and images, navigation, and publication. Keep authored page URLs and anchors stable.

## Generate and check references

From a Windows checkout with .NET 10 and PowerShell 7, run these commands at the repository root. The PowerShell generator also requires `Alt3.Docusaurus.Powershell` 2.0.1. Write before `-Check`, using the same output paths for both invocations.

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release -f net10.0-windows10.0.26100.0
pwsh -NoProfile -File tools/ApiDocs/Generate-ApiDocs.ps1 -Configuration Release -OutputRoot artifacts/docs/api -CoveragePath artifacts/docs/coverage.md
pwsh -NoProfile -File tools/ApiDocs/Generate-ApiDocs.ps1 -Configuration Release -OutputRoot artifacts/docs/api -CoveragePath artifacts/docs/coverage.md -Check
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1 -Configuration Release -Build
Install-Module Alt3.Docusaurus.Powershell -RequiredVersion 2.0.1 -Scope CurrentUser
pwsh -NoProfile -File tools/Docs/Generate-PowerShellReference.ps1 -OutputRoot artifacts/docs/powershell/reference
pwsh -NoProfile -File tools/Docs/Generate-PowerShellReference.ps1 -OutputRoot artifacts/docs/powershell/reference -Check
git status --short
```

`-Check` compares exact output bytes and rejects missing, changed, or extra generated pages in the selected output root. It does not create a baseline in a fresh checkout. The API coverage report is at `artifacts/docs/coverage.md`. The PowerShell generator checks one page per exported function; authored reference introduction/type pages are outside that generated set. Use dedicated output roots, not directories containing authored content.

## Review and sync to a website checkout

`Sync-WebsiteDocs.ps1` defaults to the sibling website checkout's `docs/` directory. Pass `-WebsiteDocsPath` when using an isolated checkout. Before first-run initialization, generate both complete reference sets above and run a dry run. It fails if generated inputs are missing or an existing website reference has different bytes. Reconcile that difference for review before initialization; do not adopt an arbitrary website copy as a baseline. On a website checkout without root `source-docs-manifest.json`, initialize ownership from its verified existing published files once, then run the ordinary sync. Initialization preserves existing pages and PNGs, records legacy image provenance, and leaves unverified visual-input hashes null.

```powershell
$siteDocs = 'C:/path/to/Fluence.Wpf.Website/docs'
$siteRoot = Split-Path -Parent $siteDocs
pwsh -NoProfile -File tools/Docs/Sync-WebsiteDocs.ps1 -WebsiteDocsPath $siteDocs -DryRun
if (-not (Test-Path -LiteralPath (Join-Path $siteRoot 'source-docs-manifest.json') -PathType Leaf)) {
    pwsh -NoProfile -File tools/Docs/Sync-WebsiteDocs.ps1 -WebsiteDocsPath $siteDocs -InitializeManifest
}
pwsh -NoProfile -File tools/Docs/Sync-WebsiteDocs.ps1 -WebsiteDocsPath $siteDocs -DryRun
pwsh -NoProfile -File tools/Docs/Sync-WebsiteDocs.ps1 -WebsiteDocsPath $siteDocs
git -C $siteRoot status --short
git status --short
```

The sync preflights complete generated references and managed paths before website writes. It copies generated API and function pages, the authored PowerShell reference pages, and the authored top-level guides. It removes obsolete owned generated pages after checking their recorded hashes. The root manifest records managed hashes and image provenance; ordinary sync does not capture, replace, or delete screenshots. Review the website diff, then run `pnpm check:docs-transform`, `pnpm typecheck`, and `pnpm build` from its `website/` directory before any website commit. Source generation and sync should leave source Git status unchanged.

## Review visual changes separately

`VisualCaptureMap.psd1` declares capture IDs, page families, and source inputs. `-CaptureId` selects the whole mapped family, including Light/Dark companions and declared states, when any member needs review. A candidate is a request for review, not approval to replace an image. The gallery harness and PowerShell capture script write optional candidates under `artifacts/docs/screenshots/` and `artifacts/docs/powershell/images/`. Ordinary build and docs-sync CI do not capture PNGs.

```powershell
$candidates = & './tools/Docs/Select-VisualCaptures.ps1' -WebsiteManifestPath (Join-Path $siteRoot 'source-docs-manifest.json') -CaptureId 'gallery/buttons-light.png'
if (@($candidates.candidates).Count -eq 0) { throw 'No changed capture family was selected.' }
$candidates | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath artifacts/docs/visual-candidates.json -Encoding utf8BOM
```

After building the Debug .NET 10 test executable, capture this selected gallery family on an interactive Windows desktop. The harness's `FLUENCE_CAPTURE_INCLUDE` is an exact low-level filter; deriving it from the expanded candidates keeps the required family together. An exact subset is useful for scratch diagnosis but cannot be approved as an incomplete family. The gallery harness writes a partial capture manifest; never replace the website's complete gallery manifest with it.

```powershell
$env:FLUENCE_CAPTURE_SCREENSHOTS = '1'
$env:FLUENCE_CAPTURE_INCLUDE = (@($candidates.candidates | ForEach-Object { $_.id }) -join ',')
$env:FLUENCE_CAPTURE_OUTPUT_DIRECTORY = (Join-Path $PWD 'artifacts/docs/screenshots')
try {
    & 'Fluence.Wpf.Tests/bin/Debug/net10.0-windows10.0.26100.0/Fluence.Wpf.Tests.exe' --filter-class Fluence.Wpf.Tests.Tools.GalleryScreenshotHarness --no-ansi --progress off
    if ($LASTEXITCODE -ne 0) { throw 'Gallery candidate capture failed.' }
} finally {
    Remove-Item Env:FLUENCE_CAPTURE_SCREENSHOTS, Env:FLUENCE_CAPTURE_INCLUDE, Env:FLUENCE_CAPTURE_OUTPUT_DIRECTORY -ErrorAction SilentlyContinue
}
& './tools/Docs/Review-VisualCaptures.ps1' -CandidatesPath artifacts/docs/visual-candidates.json -WebsiteRoot $siteRoot
```

The review command compares decoded pixels with the published PNGs and reports pending material decisions. For pixel differences, the decision JSON contains `decisions` entries with `id`, `decision` (`material-change` or `visually-unchanged`), `candidatePngSha256`, and a reviewed `provenance`. A control capture with no existing image or recorded baseline uses `new-capture` instead. New gallery captures are not supported by this review flow. Once every pending candidate has a decision, write the approved manifest and sync it:

```powershell
pwsh -NoProfile -File tools/Docs/Review-VisualCaptures.ps1 -CandidatesPath artifacts/docs/visual-candidates.json -WebsiteRoot $siteRoot -DecisionsPath artifacts/docs/visual-decisions.json -OutputPath artifacts/docs/approved-captures.json
pwsh -NoProfile -File tools/Docs/Sync-WebsiteDocs.ps1 -WebsiteDocsPath $siteDocs -ApprovedCaptureManifest artifacts/docs/approved-captures.json
```

A visually unchanged decision advances the checked input hash while retaining the published PNG. A material change can replace only its approved image. Review the resulting website diff and site checks before publication.

Sync uses exclusive creation for a new capture. If a write fails partway through and the resulting bytes differ from the approved PNG, rollback leaves the file in place and reports the retained recovery snapshot for inspection.

The [docs workflow](../../.github/workflows/docs-sync.yml) runs on `main` and can be started manually. It always writes and checks ignored references. With `WEBSITE_DOCS_READY` unset or false, it stops there with no Git commit. To enable website sync, set the source repository Actions variable `WEBSITE_DOCS_READY` to `true` and configure `WEBSITE_DOCS_TOKEN` as a fine-grained token with Contents read/write access to the website repository. With readiness true, the workflow initializes the manifest if absent, syncs managed pages, validates the website, checks that source and website `main` have not advanced, and commits only changed website managed paths. It has read-only source permissions and does not capture screenshots, commit generated source files, publish a website deployment, or release a package.
