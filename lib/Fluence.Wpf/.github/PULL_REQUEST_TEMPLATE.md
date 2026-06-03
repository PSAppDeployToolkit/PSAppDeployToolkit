## Summary

- 

## Verification

- [ ] `dotnet build Fluence.Wpf.sln -c Release --no-restore -v minimal` (0 errors / 0 warnings, both TFMs)
- [ ] `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Release -f net472 --no-build`
- [ ] `dotnet test Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Release -f net10.0-windows10.0.26100.0 --no-build`
- [ ] `pwsh eng/Format-Xaml.ps1 -Check` passes (authored XAML conforms to `Settings.XamlStyler`).
- [ ] Visual pass completed in `Fluence.Wpf.Demo` for Light, Dark, High Contrast, accent swap, and relevant backdrop.

## Checklist

- [ ] Public API changes have XML docs and tests; the test count does not drop below the baseline.
- [ ] Template or visual changes use canonical WinUI-style theme keys and `DynamicResource` where theme-bound (no inline hex colors).
- [ ] Visual or behavioral choices are grounded in a Section 4 reference authority (in-tree precedent, WinUI 3 CommonStyles, or .NET 10 WPF Themes).
- [ ] `CHANGELOG.md` is updated under `Unreleased`.
- [ ] Public docs are updated when consumer behavior changes.
- [ ] No unrelated files or local tool state are included.
