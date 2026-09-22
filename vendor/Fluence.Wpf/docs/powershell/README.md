# Fluence.Wpf from PowerShell

`Fluence.Wpf.PowerShell` is a script module that gives Windows PowerShell 5.1 and PowerShell 7.4 or later scripts themed Fluent (Windows 11) dialogs, prompts, progress windows and full windows, built on the Fluence.Wpf control library. Nothing to compile: import the module and call a cmdlet.

```powershell
Import-Module .\Fluence.Wpf.PowerShell.psd1
Show-FluenceMessage -Message 'Installation complete.' -Icon Success
```

The documentation is organised by what you are trying to do.

## Learning

- [Tutorial: your first dialog in ten minutes](tutorial.md). Install the module, show a message, build a two-prompt form and read the result. Start here if the module is new to you.

## Doing

How-to guides, each starting from a goal:

- [Show dialogs and messages](how-to/dialogs.md): presets, custom buttons, icons, timeouts and countdowns, images and screen position.
- [Build forms with validation](how-to/forms-and-validation.md): prompts of every input type, defaults, required fields, patterns and custom validation.
- [Host a window from XAML](how-to/windows-from-xaml.md): show a full FluenceWindow from a XAML string or file and wire its controls.
- [Show progress during long work](how-to/progress.md): the non-modal progress window and how to keep it painting.
- [Change theme, accent and backdrop at runtime](how-to/theming-at-runtime.md).
- [Host the module inside PSADT](how-to/host-in-psadt.md): reuse the toolkit's application, map the deployment dialogs, and what the loader does when the host's library is older.
- [Use the library without the module](how-to/raw-library-without-the-module.md): the bare `Add-Type` bootstrap and the four `Fluence.Wpf.Demo.PowerShell` scripts.

## Looking things up

- [Reference](reference/README.md): one page per cmdlet, generated from the module's own help, plus the [result objects](reference/result-objects.md) and the [input types](reference/input-types.md).

## Understanding

- [Explanation](explanation.md): why there are two library builds and one loader, how the STA host and the MTA runspace differ, what theme seeding and the dictionary slots are, why the specification objects are plain objects, and why the module runs in-process.

## Where things live

| Item | Path |
| --- | --- |
| Module source | `Fluence.Wpf.PowerShell.Module/src/Fluence.Wpf.PowerShell/` |
| Runnable examples | `Fluence.Wpf.PowerShell.Module/examples/` |
| Pester tests and the gate | `Fluence.Wpf.PowerShell.Module/tests/`, `Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1` |
| Raw-library scripts (no module) | `Fluence.Wpf.Demo.PowerShell/` |
| Screenshots used in these pages | `docs/powershell/images/` |
