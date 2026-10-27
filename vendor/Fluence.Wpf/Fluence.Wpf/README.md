# Fluence.Wpf library

This project contains the reusable WPF control library. It targets `net472`, `net8.0-windows10.0.26100.0`, and `net10.0-windows10.0.26100.0`.

| Area | Contents |
| --- | --- |
| `Controls/` | Controls, `FluenceWindow`, and window chrome |
| `Themes/` | Resource dictionaries, templates, colors, and typography |
| `Theming/` | Theme resolution and published colors and brushes |
| `Automation/` | UI Automation peers |
| `Native/` | Windows interop |

Build from the repository root:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release
```

Start with the [first WPF application tutorial](https://fluencewpf.com/docs/tutorials/first-wpf-app), then use the [control catalog](https://fluencewpf.com/docs/controls) and [theming guide](https://fluencewpf.com/docs/theming). Browse the [documentation website](https://fluencewpf.com) for the full set. For project conventions and theme engine internals, read [AGENTS.md](../AGENTS.md).
