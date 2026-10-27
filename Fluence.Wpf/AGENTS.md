# Fluence.Wpf source guidance

These rules apply to library source. The nearest Controls, Themes, or Theming guidance adds the rules for that area.

## C# source and APIs

- Copy the 27-line BSD 3-Clause header from an existing source file when adding a C# file. Do not change the copyright year unless requested.
- Nullable reference types are enabled by default. Keep public APIs nullable-clean and add XML documentation to every public type and member.
- Directory.Build.props and .editorconfig define language, analyzer, and style policy. BannedSymbols.txt defines prohibited APIs. Fix analyzer findings at their source; add suppressions only through the documented project configuration.
- Modern C# syntax is available on every target. Check runtime API availability against .NET Framework 4.7.2 before using an API; a newer language version does not add runtime APIs. Prefer a WPF-compatible implementation over TFM-only conditional behavior. Record an accepted framework gap in KNOWN_ISSUES.md.
- Do not add third-party runtime or build packages without user approval.
- For changes to build policy, target frameworks, public API, resources, or packaging, verify both the standalone library and the downstream consumer build used for release gating. Keep consumer-specific paths and steps in that consumer's repository.

## Design and implementation references

Resolve visual and behavioral choices in this order:

1. Existing Fluence templates, controls, and test helpers.
2. The domain authority below.
3. Published Microsoft Fluent and Windows App SDK guidance.

| Concern | Primary reference |
| --- | --- |
| Fluent visual tokens and control templates | [WinUI 3 CommonStyles](https://github.com/microsoft/microsoft-ui-xaml/tree/main/src/controls/dev/CommonStyles) |
| WPF window chrome and system theme behavior | [.NET WPF themes](https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes) |
| DWM backdrops and window composition | [.NET WPF themes](https://github.com/dotnet/wpf/tree/main/src/Microsoft.DotNet.Wpf/src/Themes) and [DWM API documentation](https://learn.microsoft.com/windows/win32/api/dwmapi/) |
| UI Automation behavior | WinUI 3 CommonStyles and Microsoft UI Automation documentation |

If the references do not cover a design decision, explain the gap and ask for guidance before committing to a new behavior.

## Cross-area changes

- For a new control or a substantial control change, follow the checklist in [Controls/AGENTS.md](Controls/AGENTS.md) and the new-control playbook in [.claude/skills/new-control/SKILL.md](../.claude/skills/new-control/SKILL.md).
- For theme engine, accent, palette, or publication changes, follow [Theming/AGENTS.md](Theming/AGENTS.md). For template and resource changes, follow [Themes/AGENTS.md](Themes/AGENTS.md) as well.
- A visible control behavior change needs a focused test in Fluence.Wpf.Tests. A visual change also needs Light, Dark, and High Contrast verification in the gallery, plus screenshots at 100% and 150% DPI when the change is material.
- Public behavior changes update CHANGELOG.md and the relevant authored guide in docs/. See docs/AGENTS.md for the separate website ownership boundary.
