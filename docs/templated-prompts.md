# Templated prompts

Referenced from `AGENTS.md` Section 13. Lifted out of the handbook to keep the always-loaded file lean; the content is the same canonical templates a contributor (human or AI) copies to drive an end-to-end change.

Two canonical task templates. Copy the relevant block, fill in the `TASK` line, and execute end-to-end.

## Generic Fluence.Wpf development workflow

```text
ROLE: Senior WPF engineer maintaining Fluence.Wpf, a Windows 11 Fluent control library for .NET Framework 4.7.2 and .NET 10+.

CONTEXT (read before touching code):
- Fluence.Wpf/AGENTS.md - this handbook (authoritative)
- Fluence.Wpf/docs/controls.md - public control catalogue
- Fluence.Wpf/docs/theming.md - canonical brush/color families
- Fluence.Wpf/docs/contributing.md - contribution notes
- Fluence.Wpf/docs/release.md - release validation notes
- Fluence.Wpf/CHANGELOG.md - recent scope

Reference authority (see AGENTS.md Section 4):
  1. In-tree precedent (XAML, controls, tests)
  2. Per-domain authority:
     - WinUI 3 CommonStyles - https://github.com/microsoft/microsoft-ui-xaml/tree/main/src/controls/dev/CommonStyles
     - .NET 10 WPF Themes - https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes
  3. Windows 11 design guidance on Microsoft Learn (tie-breaker only)

TASK: <one sentence describing the concrete change>

WORKFLOW:
 1. Re-read the relevant sections of AGENTS.md: Section 3 Theme architecture, Section 4 Reference priority, Section 5 Control authoring, and Section 6 Testing.
 2. Enumerate files and regions you plan to touch. Keep the diff minimal and name the slot/layer each file belongs to.
 3. If the change is visual or behavioural, cite the authority from Section 4 that justifies it.
 4. For any new control, follow Section 5 (Control authoring checklist) exactly.
 5. For any theme / brush / color change, update the matching entries in all three Theme.{Light|Dark|HighContrast}.xaml tables; BrushFactory auto-twins each color, and SpecialBrushes.cs is touched only for exceptions (non-standard twin names, gradients, high-contrast overrides).
 6. TDD: add or extend an MSTest before writing implementation. Run just that test on `net10.0-windows10.0.26100.0` first (fast feedback), then both TFMs.
 7. dotnet build Fluence.Wpf.sln -c Debug - 0 errors / 0 warnings on net472 + net10.0-windows10.0.26100.0.
 8. dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net472 --no-build, then dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug -f net10.0-windows10.0.26100.0 --no-build; all tests pass and known failures are unchanged.
 9. Update docs: CHANGELOG.md (always), docs/*.md (when the public surface changes), and create or update KNOWN_ISSUES.md only when an accepted known gap is opened or closed.
10. Stage changes; show diffs; wait for the user's explicit commit instruction.

ACCEPTANCE:
- Build: 0/0 on both TFMs
- Tests: no new regressions; net count +N for N new tests you added
- Docs: synced with the change
- No new third-party WPF runtime dependency introduced; no external agent bundles referenced
- Every visual / behavioural choice has a cited authority from Section 4

STOP CONDITION: working tree is "git-clean minus your intended diff"; wait for explicit user approval before committing.
```
