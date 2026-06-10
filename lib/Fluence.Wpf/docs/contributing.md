## Build and test

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build Fluence.Wpf.sln
dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj
```

The test project runs on .NET Framework 4.7.2 and .NET 10 for Windows; both must pass. WPF tests share a single STA dispatcher (`WpfTestSta`), and the assembly carries `[assembly: DoNotParallelize]` to avoid cross-thread resource issues. Treat the branch's current test count as the floor. Add coverage for new behavior, and do not remove a test without documenting why its replacement supersedes it.

## Language and style

- **Fluence.Wpf** library: `LangVersion=latest` and nullable reference types are enabled centrally. Use modern C# syntax on any target framework, but keep runtime APIs to ones that exist on .NET Framework 4.7.2 unless the code is already isolated to a newer target.
- Every `.cs` file starts with the standard BSD 3-Clause header used across the repo; match an existing file exactly.
- Public APIs carry `///` XML comments. The library builds with `<DocumentationFile>` and does **not** suppress `CS1591` / `CS1574` - a missing comment becomes a build error.
- XAML lives in `Fluence.Wpf/Themes/Controls/<ControlName>.xaml` and is merged from `Themes/Generic.xaml`.

## Visual changes

- Run **Fluence.Wpf.Demo** and exercise: theme (Light / Dark / High Contrast / Auto), accent swatches, backdrop, and representative controls per gallery section.
- Prefer `DynamicResource` for theme-bound properties in XAML.
- Use WinUI 3 CommonStyles as the visual reference for resource keys, states, and animation timing. For WPF-specific chrome or interop behavior, follow .NET WPF theme sources.

## Tests

- Drop new test files alongside existing ones (`ControlTests.<Area>.cs`) as partial extensions of `public partial class ControlTests` so they share the `RunOnStaThread`, `EnsureApplication`, `MergeGenericDictionary`, and `FindVisualChild*` helpers.
- When adding a new public control, include at minimum:
  - A default-style / template smoke test.
  - A theme-cycle test if the control uses `DynamicResource` heavily (`ThemeTestHelpers.ApplyStandardThemeCycle`).
  - Interaction or state assertions for any public event / read-only DP the control exposes.
- `ControlTests.FluentStroke.cs` is the reference pattern for small template/behavior probes: apply the generic dictionary, show a minimal `Window`, `ApplyTemplate`, assert template parts and resolved brushes, then drain and close.

## Pull requests

- Keep changes focused; avoid unrelated refactors.
- If you add a public control or change a template, extend MSTest coverage (template parts, theme cycle, or demo navigation smoke where appropriate).
- Update [CHANGELOG.md](../CHANGELOG.md) under **Unreleased** or the next version section.
- The library builds with `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`; fix warnings rather than suppressing them.

## Documentation

- Public guides live in `docs/*.md`. Maintainer-only notes live under `docs/_internal/`; do not link them from `README.md` or public guides.
- AI-assisted edits should read [AGENTS.md](../AGENTS.md) for project standards and quality gates.

## Documentation site

There is currently **no** hosted documentation site; documentation lives entirely in the Markdown files under `docs/` and at the repository root. A published site is planned but not yet set up.

- Cross-doc links: prefer `[text](other-doc.md)` relative links so they resolve both on GitHub and in any future generated site.
