# Release Checklist

Use this checklist before publishing a package or tagging a release.

## Package Readiness

- Confirm `README.md`, `CHANGELOG.md`, and public docs under `docs/` describe the current public surface.
- Confirm every public API has XML documentation and that `Fluence.Wpf.xml` is included in package output.
- Confirm internal helpers such as `CaptionButtonChrome` and `WindowPolicy` are not documented as consumer-facing controls.
- Confirm screenshots under `docs/screenshots/` are current when visual changes affect the gallery banner.

## Local Gates

Run from the repository root:

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln -c Debug
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug
```

When demo source samples change, also build the gallery:

```powershell
dotnet build Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug
```

## Pack Check

```powershell
dotnet pack Fluence.Wpf/Fluence.Wpf.csproj -c Release -o ./artifacts
```

Inspect the package for the assembly, XML documentation file, license, README, and theme resources.

## Docs Site

There is currently **no** hosted documentation site or docs build/deploy workflow; the documentation lives entirely in the Markdown files under [`docs/`](.) and at the repository root. A published site is planned but not yet set up.

Release rule:

- When visual changes affect the gallery banner, regenerate `docs/screenshots/` (via the `GalleryScreenshotHarness`) before tagging so the in-repo screenshots stay current.
