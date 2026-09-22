# Roadmap

## 1. Purpose and how to read this roadmap

This page records the work planned between the current preview and 1.0, plus the
candidate work that follows it. It is a maintainer document published for
consumers so that a downstream project can see what is committed, what is being
considered, and what is deliberately excluded.

Every item carries one of three statuses.

| Status | Meaning |
| --- | --- |
| Planned | Accepted as work to do. Not started. |
| In progress | Started on a branch or partially shipped. |
| Done | Shipped on `main` or complete on the branch named in the item. |

There are no dates. Items are not tracked as issues yet; once the tracker is
populated each item gains a link to its issue and this page keeps only the
summary. Defects and deliberate non-features live in
[KNOWN_ISSUES.md](../KNOWN_ISSUES.md), not here.

## 2. Release policy

The current version is whatever the [latest release](https://github.com/sintaxasn/Fluence.Wpf/releases/latest) says. `Directory.Build.props` is its single source in the tree.

1.0 ships alongside PSAppDeployToolkit 4.2, which is the first downstream
consumer of the library.

Until 1.0, breaking changes to the public API and to XAML resource keys are
permitted. Every such change is recorded in
[docs/migration-guide.md](migration-guide.md) and in
[CHANGELOG.md](../CHANGELOG.md) in the same commit that makes it.

From 1.0 the library follows Semantic Versioning. Minor releases are additive
only: new types, new members, new resource keys. Removals and signature changes
wait for a major release.

The enforcement mechanism is `Microsoft.CodeAnalysis.PublicApiAnalyzers`, which
pins the public surface in `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`
per target framework and fails the build when an undeclared public member
appears or a declared one disappears. It does not cover XAML resource keys, so
those are pinned separately by
`ThemeParityTests.PublicKeyInventory_MatchesFrozenSetAsync` against
`Fluence.Wpf.Tests/Theming/golden/PublicKeys.txt`, which fails on an addition
as well as a removal.

This page names PSAppDeployToolkit throughout. `AGENTS.md` section 12 restricts
consumer-specific filesystem paths, build steps and deployment artifacts from
the handbook and the public documentation set; it does not restrict naming the
consumer whose release this one is coordinated with, which a reader needs in
order to understand the 1.0 timing. That is a deliberate, recorded exemption
and applies to this page only.

## 3. 1.0 readiness

| Item | Status | Notes |
| --- | --- | --- |
| WinUI parity pass | Done | The colour and role audit and its corrections are complete; the residue is written up in [docs/winui-parity.md](winui-parity.md) and `KNOWN_ISSUES.md`. |
| Test suite consolidation | Done | One sealed class per subject in five folders, one resource merge helper, per-class isolation through `IAsyncLifetime`, and the two-lane executable invocation. |
| Public API review | Done | Four types internalised, nine template part constants made internal, four events given typed args, two routed events retyped, three enums renamed to their WinUI or .NET WPF names, and two vestigial members removed. Frozen by `PublicApiAnalyzers`. |
| XAML key review | Done | Fourteen unconsumed keys removed, two WinUI-named aliases published, one key renamed. Frozen by `PublicKeys.txt`. |
| Packaging | Done | `DebugType` is `portable`, and the library packs with `Microsoft.SourceLink.GitHub`, `IncludeSymbols`, and `SymbolPackageFormat=snupkg`. `dotnet pack -c Release` emits `Fluence.Wpf.<version>.nupkg` with no PDB inside, plus a matching `.snupkg` carrying one portable PDB per target framework, each embedding a SourceLink document map. `ContinuousIntegrationBuild` is set only when `GITHUB_ACTIONS` is `true`, so a CI-built PDB carries repository-relative paths while a local build keeps absolute ones for local debugging. `VersionPrefix` and `VersionSuffix` in `Directory.Build.props` are the single source of the version; the SDK derives `PackageVersion`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion` from them. The tag-gated GitHub release job (zipped per-TFM binaries, the demo, and the nupkg plus its snupkg) already exists and now also publishes to NuGet: a `v*` tag push runs the job once the build job passes, the job fails closed if the tag does not match the tree version or if `CHANGELOG.md` has no matching version section, and it then pushes the nupkg, with its sibling snupkg, to nuget.org using the `NUGET_API_KEY` secret. Creating and pushing that tag remains the owner's manual step. |
| Documentation pass | Done | The guides are reconciled with the shipped surface. The site in section 5 stays a 1.x item. |

## 4. PowerShell module over the PSADT UI bridge

**Goal.** Give PowerShell callers a supported way to show Fluence windows and
dialogs without writing C#, and give PSAppDeployToolkit a stable contract for
the Fluence dialogs it already renders.

**What exists today.** There is no PowerShell module. `Fluence.Wpf.Demo.PowerShell`
holds four standalone scripts (`01-HelloWorld.ps1`, `02-ThemeAndAccent.ps1`,
`03-ControlsTour.ps1`, `04-LoadXamlFile.ps1`) plus `MainWindow.xaml`, documented
in [docs/powershell.md](powershell.md). They run under Windows PowerShell 5.1,
load the `net472` assembly with `Add-Type`, relaunch themselves into an STA
apartment, and call the same static theme API a C# caller would. The library
also targets `net8.0-windows10.0.26100.0` for in-process consumption from
PowerShell 7.

**What PSADT does today.** In the PSAppDeployToolkit 4.2 checkout inspected for
this page, the toolkit vendors this library under `vendor/Fluence.Wpf` and
references it from `src/PSADT.UserInterface.Interfaces`. That project's `Fluent`
folder holds `FluentDialog.xaml`, whose root element is a `FluenceWindow`, and
the CloseApps, Custom, Input, ListSelection, Progress and Restart dialogs built
on it. An internal static `DialogManager` owns a dedicated WPF dispatcher thread
and forces `RenderOptions.ProcessRenderMode` to `SoftwareOnly`. PowerShell
functions such as `Show-ADTInstallationPrompt` do not create windows themselves;
they call the private `Invoke-ADTClientServerOperation` with `-ShowModalDialog`,
which sends `PipeCommand.ShowModalDialog` across an encrypted named pipe to the
client process running in the active user session, where `DialogManager` renders
it. The configured `DialogStyle` selects between the WinForms Classic dialogs
and these Fluence ones. The bridge that exists is therefore PSADT's own client
and server pair; Fluence has no PowerShell surface of its own.

**What the module would provide.**

- Cmdlets to show a `FluenceWindow` and the stock dialogs from a script.
- Theme, accent, and backdrop cmdlets over `ApplicationThemeManager` and
  `ApplicationAccentColorManager`.
- XAML loading from a file or a string with the Fluence namespace already
  resolvable, replacing the hand-rolled `XamlReader` calls in the sample scripts.
- STA and dispatcher lifetime handling, so a script does not have to reimplement
  the relaunch guard.
- A documented bridge contract with PSADT's client and server UI layer, so a
  PSADT dialog and a standalone script reach the same rendering path.

**Open questions.** Whether the module is binary (net472 plus net8) or script
only; whether it wraps PSADT's `DialogManager` or duplicates it; who owns the
dispatcher when PSADT already owns one; whether it ships inside the existing
NuGet package, as a separate PowerShell Gallery package, or both; and how its
version tracks the copy of the library vendored into PSADT.

**Dependencies.** The shape of PSADT 4.2's client and server UI layer. Nothing
here should fork `DialogManager` while it is internal to PSADT.

## 5. Documentation website

The guides under `docs/` are the only documentation today, and there is no build
or deploy workflow for a site: `.github/workflows/build.yml` has no job that
builds or publishes one. [docs/controls.md](controls.md) used to link API types
as `../../api/Fluence.Wpf.Controls.<Type>.html`, DocFX output layout for a site
that was never built; those dead anchors were stripped for 1.0. A generated
reference section is exactly what a documentation site would reintroduce.

Requirements: build from the existing `docs/*.md` without copying them, generate
API reference from the XML documentation the build already produces, host free
on GitHub Pages, and keep a stable version and a development version side by
side.

| Generator | .NET API reference | Uses `docs/*.md` in place | Versioning | Toolchain |
| --- | --- | --- | --- | --- |
| DocFX | Native. Reads the projects, or the assembly plus `Fluence.Wpf.xml`. | Yes, content globs point at the existing folder. | No built-in switcher. Publish each version to its own subfolder. | `dotnet tool`, already the repo's language. |
| MkDocs Material with `mike` | None. Needs a second tool to produce API pages. | Yes, `docs_dir` can be the existing folder. | `mike` maintains versioned subfolders and a switcher. | Python, a second toolchain to install and pin. |
| Docusaurus | None. Same second tool problem. | Yes, the docs plugin path can point outside the site folder. | Built in, with a version dropdown. | Node, a second toolchain, and MDX quirks in plain Markdown. |

**Recommendation: DocFX.** It is the only one of the three that produces the API
reference from the XML documentation the build already emits and already
requires on every public member. It matches the `api/*.html` paths
`docs/controls.md` was written against, so those links start working with no
edit. It consumes the Markdown where it sits, so nothing is duplicated. It
installs as a `dotnet tool`, adding no second language runtime to CI. The cost
is versioning: DocFX has no version switcher, so stable and development builds
publish to separate subfolders and a small banner or link handles the switch.

**Workflow outline.** A second workflow, separate from `build.yml`:

- On push to `main`, build the site and publish it to `/dev`.
- On a `v*` tag, build the site and publish it to `/stable`, and optionally to
  `/vX.Y` for an archived copy.
- Deploy with the GitHub Pages actions, using the `pages` and `id-token`
  permissions scoped to that workflow.
- The API metadata step compiles the WPF projects, so it runs on
  `windows-latest`, the same runner the build job already uses. No new agent
  type is needed.

**What changes in the repo.** A site configuration folder holding `docfx.json`,
a table of contents, and a landing page; one new workflow file; and a link from
`README.md`. No file under `docs/` moves or is duplicated. A custom domain is
optional and needs only a `CNAME` entry.

## 6. Candidate items

For the owner to accept or strike. Each line gives the value, then the cost.

| Item | Value | Cost |
| --- | --- | --- |
| Localisation and RTL audit | The library becomes usable in right to left and localised deployments. | A pass over every template for `FlowDirection`, mirroring, and hard-coded English strings, plus tests. |
| Per-monitor DPI v2 audit | Correct chrome metrics and crisp rendering when a window moves between mixed-DPI monitors. | Manual verification on a multi-monitor rig; the test harness cannot fake it. |
| `dotnet new` project templates | A themed WPF app in one command instead of a copied sample. | A template package to version and test alongside the library. |
| Remaining WinUI control gaps | Closes the catalogue against the WinUI Gallery. | Each control is a template, tests, a demo page, and docs. Absent today: `CalendarView`, `CalendarDatePicker`, `CommandBar`, `AppBarToggleButton`, `AppBarSeparator`, `MenuBar`, `FlipView`, `Pivot`, `RadioButtons`, `SplitView`, `ItemsRepeater`, `ItemsView`, `GridView` as a control of its own (`ListView.ItemsLayout` covers the wrapping layout), `RichEditBox`, `RichTextBlock`, `SwipeControl`, `RefreshContainer`, `SemanticZoom`, `AnnotatedScrollBar`, `ParallaxView`, and `AnimatedIcon`. |
| Wallpaper-aware content tint | Translucent layers over Mica would track the desktop wallpaper the way the shell does. | Deferred from the drift-correction work. It sits behind the 10 bpc alpha quantisation gate measured in `KNOWN_ISSUES.md`, so it cannot be evaluated until that gate is understood on an 8 bpc display. |
| Accessibility audit with Accessibility Insights | Independent verification of the automation peers and contrast beyond the in-tree tests. | A manual tool run per control, and the net472 API gaps already listed in `KNOWN_ISSUES.md` will surface as findings that cannot be fixed on that TFM. |
| Performance baseline as a test gate | Startup time and theme switch time stop regressing silently. | Timing assertions are noisy on shared runners and need a tolerance nobody can tune from first principles. |
| Split the four large library files | `NavigationView.cs` (2,345 lines), `FluenceWindow.cs` (2,066), `ColorPicker.cs` (1,337) and `ContentDialog.cs` (1,262) each mix three or four concerns, and `Themes/Controls/NavigationView.xaml` (1,039) holds three complete pane mode templates. | Pure internal restructuring with no API consequence, so it was deferred out of 1.0 rather than risking a late defect in the two most complex controls. The proposed split per file is recorded in the 1.0 readiness design spec. |
| Six more automation peers | `BreadcrumbBarItem`, `NavigationViewItemHeader`, `NavigationViewItemSeparator`, `CommandBarFlyoutPresenter`, `TabView` and `TabViewItem` would report their own roles. | Each is additive. The first three are covered today by their parent's peer or are decorative; `CommandBarFlyoutPresenter` needs the WinUI command bar pattern, which is a larger piece of work; the two TabView types get correct selection from the WPF `TabControl` and `TabItem` peers and are missing only the close button invoke. |
| Rename the eight contract-free `PART_*` names | `PART_BadgeBackground`, `PART_LayoutRoot`, `PART_SelectedContentHost`, `PART_StrengthSegment0` to `3` and `PART_ToggleButton` read as a code contract they do not have. | They are template internal under the support rule in [docs/theming.md](theming.md), so freezing them costs nothing and the rename can wait. |
| `NavigationViewPaneDisplayMode.Auto` and `LeftMinimal`, and the edge-aligned `TeachingTipPlacementMode` members | Closes the enum gap against WinUI. | Additive, so it does not have to precede the freeze. Each new member needs a layout path and tests. |
| `NavigationView.PaneOpened` and `PaneClosing` | Completes the pane event pair; WinUI has all four and Fluence ships `PaneOpening` and `PaneClosed`. | Additive. `PaneClosing` needs cancel semantics, which is a design question rather than a mechanical addition. |
| `KNOWN_ISSUES.md` entry for the display colour depth probe | `DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO` is superseded by `_INFO_2` from Windows 11 24H2. | The probe degrades to `default` on failure, so behaviour is safe today, but it may start reporting unknown on future builds and the limitation should be written down. |
| Unify `DemoResourceCleanupTests` on `TestApp` | Removes the one hand-rolled application and theme reset left in the suite, matching the per-class isolation pattern everywhere else. | `Fluence.Wpf.Tests/Gallery/DemoResourceCleanupTests.cs` hand-rolls its own application and theme reset instead of using `TestApp`. Unifying it needs a design decision the 1.0 work did not make, namely whether `TestApp` grows an entry point taking theme, backdrop and accent together, so it waits for 1.x. |
| Split `DemoShellTests` and `NavigationViewTests` | Smaller, single-concern test classes are easier to navigate and change. | `Fluence.Wpf.Tests/Gallery/DemoShellTests.cs` and `Fluence.Wpf.Tests/Control/NavigationViewTests.cs` are large enough to warrant splitting into smaller classes, which is deferred to 1.x by design. |
| `ContentDialog.Closed` ordering | `Closed` is raised after the `ShowAsync` task completes, so an `await ShowAsync()` continuation can run before `Closed` handlers. Raising `Closed` first would give handlers and awaiters one consistent order. | The typed event args froze the signature, not the order, so the change is behavioural only. It needs a test that pins the order and a migration note for consumers that depend on the current one. |
| `TabView` close aggregation coverage | `TabView` forwards each child `TabViewItem.CloseRequested` as one `TabCloseRequested` and then marks the child event handled, and no test pins the handled half of that contract. | One test in `Fluence.Wpf.Tests/Control/TabViewTests.cs`. |
| `InfoBadgeAutomationPeer` name change notification | A `Value` change updates the badge text but never raises `RaisePropertyChangedEvent` for the automation name, so a screen reader hears the old value until the tree refreshes. | Additive to fix and no consumer has reported it. It belongs with the accessibility audit above. |
| Drop the `Microsoft.SourceLink.GitHub` pin | The .NET 8 and later SDKs ship SourceLink in box, so the explicit package reference is redundant on two of the three target frameworks. | `net472` still needs the package, so removing the pin means conditioning it per target framework and regenerating three lock files. |
| Release job partial-run recovery | A first `v*` run that fails during asset upload leaves an incomplete GitHub release that the idempotent re-run then skips. | The fix is a delete-and-recreate step guarded by a release-body marker, which trades safety on re-run for recoverability. |
| Suite wall-clock baseline | The consolidated suite has never been timed against the pre-consolidation 3 min 29 s, so the `LightThemeFixture` win is asserted, not measured. | One timed two-lane run per target framework on a quiet machine, recorded in `KNOWN_ISSUES.md` or the test README. |

## 7. Out of scope for 1.0

- Drag to reorder and tear-off tabs in `TabView`.
- A navigation history stack inside `NavigationView`.
- Real acrylic as an in-process material. WPF cannot render it, so the opaque
  fallback plate stays.
- Hosted controls with their own runtimes: `WebView2`, `MapControl`,
  `MediaPlayerElement`.
- Any target that is not WPF on Windows.
- Theming third-party control libraries.
