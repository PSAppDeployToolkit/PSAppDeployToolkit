# Fluence.Wpf agent handbook

This file sets repository-wide rules and routes work to the smallest relevant guidance. Read it before editing, then read the nearest AGENTS.md for each affected path. When a change spans directories, follow every applicable file.

## Repository contract

- Fluence.Wpf is a WPF library that follows the Windows 11 Fluent and WinUI 3 visual language. The library targets .NET Framework 4.7.2, .NET 8 for Windows, and .NET 10 for Windows. Full tests target .NET Framework 4.7.2 and .NET 10; a separate smoke project covers .NET 8.
- Keep the library portable across downstream consumers. Consumer-specific paths and instructions belong in the consumer repository.
- Preserve existing user changes and keep edits within the requested scope. Add package dependencies only after the user approves them. Create commits only when the user has authorized a commit.
- Use Directory.Build.props, Directory.Packages.props, .editorconfig, project files, and scripts as the source of truth for build settings, analyzers, package versions, and commands. Do not duplicate their full configuration in guidance.
- New C# files need the existing BSD 3-Clause header. Public API changes need XML documentation. Follow the configured nullable and analyzer policies.
- The root CONTRIBUTING.md is the canonical source for the build and validation command set. Run focused checks for the change; use the full affected build and test matrix for release preparation.
- For visual decisions, prefer in-tree precedent, then the domain authority named in Fluence.Wpf/AGENTS.md, then Microsoft guidance. Do not invent undocumented WinUI behavior.

## Task routing

| Work area | Read this guidance |
| --- | --- |
| Any library source | [Fluence.Wpf/AGENTS.md](Fluence.Wpf/AGENTS.md) |
| Theme engine, palette, accent, theme publication, or backdrop behavior | [Fluence.Wpf/Theming/AGENTS.md](Fluence.Wpf/Theming/AGENTS.md) |
| XAML templates, theme resources, or color tables | [Fluence.Wpf/Themes/AGENTS.md](Fluence.Wpf/Themes/AGENTS.md) |
| Controls, dependency properties, window chrome, or control behavior | [Fluence.Wpf/Controls/AGENTS.md](Fluence.Wpf/Controls/AGENTS.md) |
| .NET tests | [Fluence.Wpf.Tests/AGENTS.md](Fluence.Wpf.Tests/AGENTS.md) |
| Gallery application | [Fluence.Wpf.Demo/AGENTS.md](Fluence.Wpf.Demo/AGENTS.md) |
| MVVM sample application | [Fluence.Wpf.Demo.Mvvm/AGENTS.md](Fluence.Wpf.Demo.Mvvm/AGENTS.md) |
| PowerShell module | [Fluence.Wpf.PowerShell.Module/AGENTS.md](Fluence.Wpf.PowerShell.Module/AGENTS.md) |
| Repository and public product documentation | [docs/AGENTS.md](docs/AGENTS.md) |
| GitHub workflows or pull request automation | Inspect the relevant files under .github/ and the matching validation instructions in CONTRIBUTING.md. |
| In-repository AI tooling | [.claude/AGENTS.md](.claude/AGENTS.md) describes its agents, skills, and hooks. Use a specialized lane only when its trigger matches the task. |

## Documentation ownership

This repository owns generated C# API pages in docs/api/ and PowerShell reference pages in docs/powershell/reference/, plus authored docs/controls.md, docs/theming.md, and docs/winui-parity.md. The standalone Fluence.Wpf.Website repository owns Docusaurus authoring, site navigation and styling, browser review, publishing, and runnable walkthroughs. Keep operational release material out of this repository's public documentation.

## Private planning artifacts

- Save agent-created designs, implementation plans, audits, research, reviews, and handoffs under docs/_internal/. Use docs/_internal/plans/ for designs and plans, with companion evidence and scripts beside the owning plan.
- Apply this repository preference to every skill, including Superpowers brainstorming, writing-plans, and executing-plans, in place of their default output locations. Keep authored product documentation in its existing tracked locations.
- docs/_internal/ is Git-ignored. Keep these artifacts out of staging and commits, and copy required artifacts separately when using another worktree or moving a checkout.
- After creating or materially revising a planning package, keep a timestamped copy outside the checkout under the user's Documents/RepositoryBackups/ directory, or another user-selected backup location. Verify the copied files with SHA-256 and record the backup location in the handoff. Preserve earlier backup snapshots.
