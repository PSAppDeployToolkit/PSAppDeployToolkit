# Fluence.Wpf consumer app starters

The `Fluence.Wpf.Templates` package provides three `dotnet new` starters. Each creates a separate WPF app targeting `net10.0-windows10.0.26100.0` and uses an explicit `Fluence.Wpf` package reference. Every generated project includes a `.gitignore` for its local NuGet cache, build output, and temporary local-feed `NuGet.Config`.

| Short name | Starting point |
| --- | --- |
| `fluence-window` | Small form and status sample with an app-name title bar |
| `fluence-nav-mvvm` | MVVM navigation, Back history, compact pane option, and accent action |
| `fluence-settings` | Four Settings categories, Back history, collapsible navigation, bundled color category images, and monochrome page glyphs |

The simple and MVVM starters place a generic vector first in the title bar, 12 DIP from the client edge. The MVVM starter keeps Back visible but disabled until navigation creates history, then shows the pane toggle. Settings starts with Back at the same 12 DIP inset, followed by its pane toggle, and has no title-bar icon. All title bars keep the app name without a category subtitle. None sets the application or window icon. Supply your own application icon when branding your app.

## Try the local candidate package

The current development package is local-only: `0.9.4-starters-dev.gb3879f16.r8`. The `.r8` stamp identifies the three title-bar arrangements, enabled Back history in the MVVM and Settings starters, and collapsible panes. It is not published and is not the normal NuGet install flow. Use the repository checker to pack the fixed library and template packages into a fresh local feed and cache, install the template into an isolated hive, and retain the run paths:

```powershell
$result = pwsh -NoProfile -File tools/Check-ConsumerTemplates.ps1 -KeepArtifacts | ConvertFrom-Json
if ($result.status -ne 'PASS') { throw "Template check failed: $($result.error)" }
```

The checker returns JSON with `run`, `feed`, `packages`, `generated`, `hive`, and `nugetConfig` paths. It installs the package in its isolated hive, generates all three apps under `generated`, builds them, then uninstalls the package from that hive before returning. This command retains the run directory so you can run the generated apps or reinstall the package for manual generation below. The generated projects restore the matching fixed library package from the local feed, so keep that feed available through restore.

The Settings starter bundles four color category images as ordinary WPF PNG resources and uses theme-aware monochrome FontIcons in page rows. The `Assets/SettingsIcons/ARTWORK-NOTICE.md` and `MICROSOFT-FLUENT-LICENSE.txt` files are copied to its build output. The General and Appearance images come from Windows Settings artwork; the artwork notice records that no explicit redistribution grant was found. Notifications and About use Microsoft Fluent Emoji derivatives under the included MIT notice. Review those distinct terms before distributing a generated app. The app does not load Windows PRI files or require Windows App SDK components.

Install the exact local template package into the isolated hive and list its templates. Pass the custom hive to every `dotnet new` command so this flow does not use the normal template registration:

```powershell
$run = $result.run
$feed = $result.feed
$hive = $result.hive
$packages = $result.packages
$nugetConfig = $result.nugetConfig
$generated = $result.generated
$templatePackage = $result.packageFiles | Where-Object { (Split-Path $_ -Leaf) -like 'Fluence.Wpf.Templates.*.nupkg' } | Select-Object -First 1
if (-not $templatePackage) { throw 'Checker output did not include the template package path.' }
$manualGenerated = Join-Path $run 'manual-examples'
New-Item -ItemType Directory -Path $manualGenerated | Out-Null

dotnet new install $templatePackage --debug:custom-hive $hive
dotnet new list fluence --debug:custom-hive $hive
```

Choose one short name and generate the app:

```powershell
dotnet new fluence-window -n Contoso.Sample -o (Join-Path $manualGenerated 'window') --debug:custom-hive $hive
# Or:
dotnet new fluence-nav-mvvm -n Contoso.Sample -o (Join-Path $manualGenerated 'navigation-mvvm') --debug:custom-hive $hive
# Or:
dotnet new fluence-settings -n Contoso.Sample -o (Join-Path $manualGenerated 'settings-shell') --debug:custom-hive $hive
```

Restore, build, and run the selected project from its generated directory. For example, after generating `fluence-window`:

```powershell
$project = Join-Path $manualGenerated 'window\Contoso.Sample.csproj'
dotnet restore $project --packages $packages --configfile $nugetConfig
dotnet build $project --no-restore -c Release
dotnet run --project $project -c Release --no-build
```

For the other starters, set `$project` to `navigation-mvvm\Contoso.Sample.csproj` or `settings-shell\Contoso.Sample.csproj` under `$manualGenerated`, then restore/build and run with `--no-build`. To run the apps already created by the checker instead, set `$project` under `$generated` and run `dotnet run --project $project -c Release --no-build`; do not generate into those existing output directories. Keep the checker run directory while using the local feed and hive. The generated `.gitignore` keeps the local package cache and temporary feed configuration out of source control. Afterward, remove the template from that same isolated hive with `dotnet new uninstall Fluence.Wpf.Templates --debug:custom-hive $hive`.

## Install a validated published package

Only after a fixed library version and matching template package have been separately approved, released, and verified on the public feed, install the exact validated template package version. Set `$validatedTemplateVersion` to the supplied version that passed those checks:

```powershell
dotnet new install "Fluence.Wpf.Templates@$validatedTemplateVersion"
dotnet new list fluence
```

The `0.9.4` source baseline predates required fixes. Published `0.9.4` package bytes have not been audited, so this guide does not qualify any `0.9.4` package for use. Do not use that version as a starter dependency or infer public package availability from this local candidate.

## Install the agent skill

The checked-in skill at `skills/fluence-consumer-starter/SKILL.md` routes agents to these template names and defines completion checks. To install it for Codex, copy the file to your configured skill directory, for example:

```text
C:\Users\<user>\.agents\skills\fluence-consumer-starter\SKILL.md
```

Create the `fluence-consumer-starter` directory first. Other agent runtimes use their own configured skill path. A consumer repository does not inherit this repository's `AGENTS.md`, and installing the NuGet package does not install agent instructions. Install the skill before starting an agent that should discover it, then verify discovery in that runtime; do not assume an existing agent automatically reloads changed skills.
