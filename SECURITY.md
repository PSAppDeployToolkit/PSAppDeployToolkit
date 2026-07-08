# Security Policy

Fluence.Wpf is a WPF control library (UI controls, theming, accent handling, window chrome, and a thin layer of Win32 / DWM interop) for .NET Framework 4.7.2 and .NET 10 on Windows. This policy describes how security issues are handled for it.

## Supported versions

The project is pre-1.0 (currently a `0.8.9` preview). Only the latest tagged release and the `main` branch receive security fixes; there is no back-porting to older preview tags. Fixes land on `main` first and ship in the next tagged release. If you consume a built package or a project reference, upgrade to the newest available version once a fix is published.

Both target frameworks are in scope:

- `net472` (.NET Framework 4.7.2)
- `net10.0-windows10.0.26100.0` (.NET 10, Windows)

## What counts as a vulnerability

Because this is a client-side UI library with no network, storage, or authentication surface, the realistic security-relevant areas are:

- Memory-safety or undefined-behavior issues in the native interop layer (`Fluence.Wpf.Native`, DWM / Win32 P/Invoke, the `FluenceWindow` window-message handling).
- A control or theme code path that can be driven to crash the host process or corrupt state from untrusted input (for example XAML or data bound into a control by a downstream app).
- Any path that lets attacker-controlled input escalate beyond the control's intended UI behavior.

The following are generally **not** security issues; file them as normal bugs instead:

- Visual or theming defects (wrong color, missing rounded corner, backdrop not applied) on any theme or backdrop.
- Crashes that require the consuming application to pass clearly invalid arguments from trusted code.
- Behavior differences from WinUI 3 that have no safety impact.

## Reporting a vulnerability

Use GitHub private vulnerability reporting for this repository (Security tab -> "Report a vulnerability") at <https://github.com/sintaxasn/Fluence.Wpf>. If private reporting is not available, open a minimal public issue asking for a private contact path; do **not** include exploit details, secrets, or weaponized proof-of-concept material in a public issue.

Please include:

- Affected version or commit, and which target framework (`net472`, `net10.0-windows10.0.26100.0`, or both).
- The impacted API, control, window-message path, or interop surface.
- Minimal reproduction steps (XAML / C# snippet or a small sample project).
- Expected and actual behavior, and the security impact.
- Relevant OS, Windows build, and .NET SDK details.

There is no formal SLA for a pre-1.0 preview project; reports are triaged on a best-effort basis on `main`.
