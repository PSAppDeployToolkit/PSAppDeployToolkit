## Summary

- 

## Verification

- [ ] `dotnet build Fluence.Wpf.sln -c Release --no-restore -v minimal` (0 errors / 0 warnings, all solution TFMs)
- [ ] `Fluence.Wpf.Tests\bin\Release\net472\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off`
- [ ] `Fluence.Wpf.Tests\bin\Release\net472\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off`
- [ ] `Fluence.Wpf.Tests\bin\Release\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off`
- [ ] `Fluence.Wpf.Tests\bin\Release\net10.0-windows10.0.26100.0\Fluence.Wpf.Tests.exe --filter-not-class Fluence.Wpf.Tests.Gallery.DemoShellTests Fluence.Wpf.Tests.Gallery.DemoSampleContractTests Fluence.Wpf.Tests.Control.NavigationViewTests Fluence.Wpf.Tests.Control.ProgressBarTests Fluence.Wpf.Tests.Control.ContentDialogTests Fluence.Wpf.Tests.Control.ColorPickerTests Fluence.Wpf.Tests.Control.TimePickerTests --filter-not-trait "Category=Screenshots" --no-ansi --progress off`
- [ ] `pwsh -NoProfile .claude/hooks/post-tool-util.ps1 -CheckAll` passes (UTF-8 BOM, LF, banned APIs, no hard-coded hex or em/en dashes).
- [ ] PowerShell module touched: `pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1 -Configuration Release` then `build/Test-Module.ps1` green on `pwsh`, `pwsh -MTA` and `powershell.exe -STA`, and the render lane (`-IncludeUi`) run locally once.
- [ ] Visual pass completed in `Fluence.Wpf.Demo` for Light, Dark, High Contrast, accent swap, and relevant backdrop.

## Checklist

- [ ] Public API changes have XML docs and tests; the test count does not drop below the baseline.
- [ ] Template or visual changes use canonical WinUI-style theme keys and `DynamicResource` where theme-bound (no inline hex colors).
- [ ] Visual or behavioral choices are grounded in a Section 4 reference authority (in-tree precedent, WinUI 3 CommonStyles, or .NET 10 WPF Themes).
- [ ] `CHANGELOG.md` is updated under `Unreleased`.
- [ ] Public docs are updated when consumer behavior changes.
- [ ] No unrelated files or local tool state are included.
