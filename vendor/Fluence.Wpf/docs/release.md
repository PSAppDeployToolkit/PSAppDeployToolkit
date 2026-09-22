# Releasing

A release is one action: bump the version, tag, push the tag. Everything after that is CI. This page is the preconditions and what to check afterwards.

## Preconditions

Confirm all of these before tagging. CI enforces the first two; the rest are judgement.

1. **CI is green on `main`.** The `build` job runs the text policy check, restores in locked mode, builds Release, verifies formatting, and runs both target framework test lanes.
2. **`CHANGELOG.md` has a dated section for the version you are about to tag**, with nothing left under `Unreleased` that belongs in it. The release job slices that section for the release notes and fails if it is missing.
3. **`PublicAPI.Unshipped.txt` is empty.** One pair of baseline files under `Fluence.Wpf/PublicAPI/` serves all three target frameworks, because the surface is the same on each. Nothing in CI enforces this: `PublicApiAnalyzers` fails the build only when a public member is undeclared in both `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` (RS0016) or when a declared member has disappeared (RS0017). A member sitting in `Unshipped` satisfies that check just as well as one folded into `Shipped`, so a 1.0 tag can go out with additions never folded in unless you confirm this by hand:

   ```powershell
   (Get-Content Fluence.Wpf/PublicAPI/PublicAPI.Unshipped.txt).Count
   ```

   One line, `#nullable enable`, means empty. To fold the additions in, fold `PublicAPI.Unshipped.txt` into `PublicAPI.Shipped.txt`, sort the result, and reset the unshipped file to `#nullable enable`. Folding in is not a plain append. A `*REMOVED*` line is an instruction to delete the named member from `PublicAPI.Shipped.txt`, so apply it and drop the marker rather than carrying it across, or the shipped baseline ends up holding an entry form that does not belong in it. Both files also begin with `#nullable enable`, so keep one and drop the duplicate.
4. **`docs/migration-guide.md` has an entry for every breaking change in the section.** The release policy in [the roadmap](roadmap.md) promises this. After 1.0 there should be none in a minor release.
5. **Screenshots are current.** If gallery visuals changed, regenerate `docs/screenshots/` before tagging:

   ```powershell
   $env:FLUENCE_CAPTURE_SCREENSHOTS = '1'
   dotnet build Fluence.Wpf.sln -c Debug
   Fluence.Wpf.Tests\bin\Debug\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Tools.GalleryScreenshotHarness
   ```
6. **The `NUGET_API_KEY` repository secret is set.** The `release` job's last step pushes to nuget.org using it, and nothing prompts you to create it before the first tag. A missing or expired key fails only at that last step, after the GitHub release has already been created and the zips and packages already attached.
7. **The `release` environment has a required reviewer.** The `release` job runs in a GitHub Environment of that name so that creating the release and pushing to nuget.org wait for an approval, which is the one chance to stop a mistaken tag before anything irreversible happens. The workflow can only name the environment; the rule lives in repository settings (Settings, Environments, `release`, Required reviewers). GitHub creates the environment on first use with no rules, so until a reviewer is added the job runs unattended exactly as it did before. If the secret is moved into the environment rather than left at repository level, only this job can read it.

## 1.0 approval checkpoint

The PowerShell integration branch is submitted for mjr4077au review before the 1.0 version bump. Keep both package versions at 0.9.0-pre until that review is approved. CI validates and packages the module in a separate `powershell` job after `build` succeeds. Both jobs must pass before merging this integration change, but the existing .NET `release` job depends only on `build`, so a PowerShell feed or tooling outage cannot block the .NET release. This is the review policy; required checks must also be configured in repository branch protection. PowerShell Gallery publication, its credential setup and release-environment approval checks are a follow-up release change. The existing tag-gated NuGet publishing workflow remains in place. Do not create a 1.0 tag until that follow-up is reviewed and both publication paths are ready.

## Bump

Edit `VersionPrefix` and `VersionSuffix` in `Directory.Build.props`. Nothing in the solution carries a version besides this file:

```xml
<VersionPrefix>0.9.0</VersionPrefix>
<VersionSuffix>pre</VersionSuffix>
```

An empty `VersionSuffix` is a stable release. A prerelease sets it, for example `pre`, which produces `0.9.0-pre`, or `rc.1`, which produces `1.0.0-rc.1`. The SDK derives `PackageVersion`, `AssemblyVersion`, `FileVersion` and `InformationalVersion` from these two; do not add them back, and never restate a version in a csproj, where it would win over this file.

The one version outside the solution is the PowerShell module manifest, `Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1`, which the SDK does not generate. Set `ModuleVersion` to the same `VersionPrefix` and `PSData.Prerelease` to the same `VersionSuffix` (with no leading hyphen), so `Package-Module.ps1` names its artifacts with the same string the library nupkg carries. `Build-Module.ps1` enforces exact agreement before building or touching staging, and packaging invokes that same guard. For a stable release, clear both suffixes; switching only one fails the gate.

Commit the bump, along with the `CHANGELOG.md` section, on `main`.

## Tag

```powershell
git tag v0.9.0-pre
git push origin v0.9.0-pre
```

## PowerShell module gates

The script module under `Fluence.Wpf.PowerShell.Module/` is outside the solution and has its own gate (see [AGENTS.md section 6](../AGENTS.md#6-testing) for the lane definitions). Install Pester 5.8.0 and PSScriptAnalyzer 1.25.0 in each edition before running the gate; the runner imports those exact versions. Follow the separate network setup commands in the [module README](../Fluence.Wpf.PowerShell.Module/README.md#tests-and-gate). Windows PowerShell setup pins the NuGet provider to 2.8.5.208; PowerShell 7 uses its compatible bundled provider. Packaging requires an already installed compatible provider and does not bootstrap one.

Stage the Release assemblies first, then run the analyzer and the Pester logic lane on both editions; the render lane opens real windows, so run it once locally before tagging:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1 -Configuration Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
pwsh -NoProfile -MTA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -SkipAnalyzer
powershell.exe -NoProfile -STA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -IncludeUi
powershell.exe -NoProfile -STA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -IncludeUi
pwsh -NoProfile -MTA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -IncludeUi
```

Expected counts for the current suite (Pester reports them on the `Pester:` summary line):

| Lane | Total | Passed | Skipped | Not run |
| --- | ---: | ---: | ---: | ---: |
| Logic lane, `pwsh` or `powershell.exe` (STA) | 212 | 184 | 2 | 26 |
| Logic lane, `pwsh -MTA` | 212 | 186 | 0 | 26 |
| Render lane (`-IncludeUi`), STA hosts | 212 | 210 | 2 | 0 |
| Render lane (`-IncludeUi`), `pwsh -MTA` | 212 | 212 | 0 | 0 |

The two skipped cases on STA hosts are the MTA-only transport tests; the not-run cases are the UI-tagged render tests. PSScriptAnalyzer must report no findings. A change that adds or removes a case updates this table and says why in `CHANGELOG.md`.

Run each gate in its own process. Require both passing Pester results and process exit code 0. The test runner performs terminal cleanup of an inline dispatcher; the module handles its owned secondary dispatcher during primary ConsoleHost exit. Host ownership boundaries are recorded in [KNOWN_ISSUES.md](../KNOWN_ISSUES.md).

Then package and inspect the module artifacts:

```powershell
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Package-Module.ps1 -Configuration Release
```

`Fluence.Wpf.PowerShell.Module/artifacts/` must hold `Fluence.Wpf.PowerShell-<version>.zip` and `Fluence.Wpf.PowerShell.<version>.nupkg`, where the version is `ModuleVersion` from the manifest with the `PSData.Prerelease` tag appended, so `0.9.0` plus `pre` names the artifacts `0.9.0-pre`. Confirm the module `README.md` and `docs/powershell/` describe the current cmdlet surface.

## Pack check

```powershell
dotnet msbuild Fluence.Wpf/Fluence.Wpf.csproj -getProperty:Version -p:TargetFramework=net472 -nologo
```

## What CI does

The `build` job owns the .NET restore, build, formatting, test, pack and artifact steps. A separate `powershell` job downloads its `fluence-wpf-release-dotnet472` and `fluence-wpf-release-dotnet8` artifacts into the corresponding Release output folders, stages the module, installs pinned tools in dedicated setup steps, and runs the PowerShell 7 STA, PowerShell 7 MTA and Windows PowerShell 5.1 STA logic lanes. It uploads `fluence-ps-module-package` and a separate `fluence-ps-module-test-results` artifact. The render lane stays local.

On a tag push, `release` depends only on `build` and downloads only `fluence-wpf-*` artifacts. PowerShell failures therefore cannot block .NET artifact upload or publication. No PowerShell Gallery publication step is configured. The `release` job then:

1. Checks the tag against the tree version and fails if they differ.
2. Zips the per-target-framework library binaries and the demo.
3. Slices the `CHANGELOG.md` section for the version into the release notes.
4. Creates the GitHub release with those assets, the `.nupkg` and the `.snupkg` attached, marking it a prerelease when the tag carries a SemVer prerelease identifier. If a release for the tag already exists, this step leaves it alone instead of recreating it, so re-running the job after a later step failed does not touch a release that already published correctly.
5. Pushes the `.nupkg` to nuget.org from the `NUGET_API_KEY` secret; `dotnet nuget push` pushes the sibling `.snupkg` from the same folder automatically. `--skip-duplicate` means a re-run whose package already reached nuget.org does not fail on that account.

## Afterwards

- The package page on nuget.org lists the symbol package.
- A consumer can step into a library source file from a Release build. Reference the published package from a scratch project, enable "Enable Source Link support" and "Enable source server support" and disable "Just My Code" in the debugger, put a breakpoint in a handler and step into `ApplicationThemeManager.Apply`.
- The GitHub release notes are the changelog section, not a commit list.

## A mistaken tag

A tag that fails the version guard has published nothing, so delete it, fix the version, and tag again. A tag that got past the guard has published to nuget.org, and **a published version can be unlisted but never replaced**. That is why the release candidate exists: tag `vX.Y.Z-rc.1` first and let it run the whole path before the stable tag.

An RC tag needs its own dated `## [X.Y.Z-rc.1]` section in `CHANGELOG.md`, distinct from the `## [X.Y.Z]` section the stable tag will use later. Precondition 2 above applies to whichever version you are tagging: without a matching section, the changelog slice step fails inside the release job, before anything is created or published.

## Strong naming

**The assembly is not strong named, deliberately.** There is no `SignAssembly` and no key file. The primary consumer references the library by project reference and does not need it, and a signing key is a release liability: it has to be stored, rotated and never lost, and every consumer is bound to it forever. If a consumer ever needs to load Fluence into a strong-named context, that is a major release decision, not a patch.
