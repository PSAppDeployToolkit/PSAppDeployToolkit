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

- new-control scaffolds a public control against [Fluence.Wpf/Controls/AGENTS.md](../Fluence.Wpf/Controls/AGENTS.md) and the required tests, demo, and documentation.
- demo-sample-page scaffolds a gallery page. Its SPEC.md defines the sample control contract, page layout, catalog integration, and completion checks.

## Hooks

- pre-tool-theme-slot.ps1 gives a non-blocking reminder for theme-slot-critical edits.
- post-tool-util.ps1 enforces text policy when invoked by the editor and supports the repository-wide CI check with -CheckAll.

Keep authoring and review in separate passes. A scaffold is not an independent review.
