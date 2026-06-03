# Fluence.Wpf -- Copilot Instructions

Condensed guidance for Copilot, Gemini, and similar assistants. Full detail is
in AGENTS.md -- read it before making any non-trivial change.

---

## Project at a glance

WPF control library reproducing Windows 11 Fluent / WinUI 3 visuals on .NET
Framework 4.7.2 and .NET 10. Four projects: `Fluence.Wpf` (library),
`Fluence.Wpf.Demo` (gallery), `Fluence.Wpf.Demo.Mvvm` (MVVM demo),
`Fluence.Wpf.Tests` (MSTest 4.2.2, multi-TFM).

**XML namespace:** `http://schemas.fluencewpf.com` (prefix: `fluence`)

---

## Non-negotiable rules

1. **File encoding:** All `.cs`, `.xaml`, `.csproj` files must be **UTF-8 with
   BOM + LF line endings**. The PostToolUse hook enforces this -- violations
   block writes.

2. **Zero warnings:** `TreatWarningsAsErrors=True`. Never suppress a warning
   with `#pragma`; fix the root cause.

3. **Banned API:** `string.IsNullOrEmpty()` is banned (RS0030). Always use
   `string.IsNullOrWhiteSpace()`.

4. **BSD header:** Every `.cs` file starts with the 27-line BSD 3-Clause header.
   Copy it from any existing library file.

5. **XML docs:** All `public` API members need `///` XML doc comments. Missing
   comments fail the build.

6. **Nullable clean:** `Nullable=enable` project-wide. Annotate `?` only where
   genuinely nullable.

7. **DynamicResource for theme values:** Any brush, color, corner radius, or
   typography that must react to theme changes uses `DynamicResource`. Static
   assets only use `StaticResource`.

8. **No hard-coded colors:** Never write hex values in XAML templates. Use
   canonical WinUI-style resource keys. Hex literals belong only in the Color
   tables `Themes/Colors/Theme.{Light|Dark|HighContrast}.xaml`; templates bind
   the auto-generated `*Brush` twin via `DynamicResource`.

9. **Overflow in Win32 bit-math:** Wrap HIWORD/LOWORD extractions in
   `unchecked { }`. `CheckForOverflowUnderflow=True` is active.

10. **Discard return values:** `_ = method()` for non-void returns you are not
    using. Ignored returns are build errors (CA1806).

---

## Theme architecture (3-slot layout -- do not break)

`Application.Current.Resources.MergedDictionaries` always has exactly 3 entries
after `ApplicationThemeManager.Apply(...)`:

| Slot | File | Lifecycle |
|------|------|-----------|
| [0] | Computed colors + brushes (built by `FluenceThemeEngine.BuildComputedDictionary`) | Rebuilt and replaced on every theme or accent change |
| [1] | `Themes/Typography/Typography.xaml` | Loaded once; never replaced |
| [2] | `Themes/Generic.xaml` | Loaded once; never replaced |

Slot [0] holds every canonical Color token plus its frozen `SolidColorBrush`
twin and the special brushes (gradients, high-contrast overrides). Adding a
color means adding it to all three `Themes/Colors/Theme.{Light|Dark|HighContrast}.xaml`
tables; `BrushFactory` auto-emits the `*Brush` twin. `DictionaryStabilityTests`
enforces the slot count and order. Changing either requires updating both the
code and the tests together.

---

## Reference priority

1. **In-tree precedent** -- existing XAML templates and C# controls always win.
2. **WinUI 3 CommonStyles** for visual tokens, states, animations, control
   templates.
3. **.NET 10 WPF Themes** for WPF-native chrome, DWM, dispatcher, `net472`
   patterns.
4. **Microsoft Learn docs** as tie-breaker only.

Never fabricate Fluent semantics from imagination. Cite an authority.

---

## Build and test

```powershell
dotnet restore Fluence.Wpf.sln
dotnet build   Fluence.Wpf.sln -c Debug          # must be 0 errors / 0 warnings
dotnet test    Fluence.Wpf.Tests/Fluence.Wpf.Tests.csproj -c Debug
```

Both `net472` and `net10.0-windows10.0.26100.0` must pass. Tests are
non-parallel (`[assembly: DoNotParallelize]`). All UI-touching work goes
through `WpfTestSta.Invoke`.

---

## Quality gates (all changes)

1. BSD header present on every `.cs` file.
2. Build: 0 errors, 0 warnings on both TFMs.
3. Tests: no new failures; new controls/behaviors ship with new MSTests.
4. Visual parity: template changes verified in Demo across Light, Dark, High
   Contrast, and at least one backdrop.
5. Docs synced: `CHANGELOG.md` updated; `docs/controls.md` / `docs/theming.md`
   updated when public surface changes.
6. No new third-party runtime dependencies without explicit user approval.
7. Do not commit without the user's explicit instruction.

---

For full detail on control authoring, theme API, common pitfalls, naming
conventions, and C# style rules: read **AGENTS.md**.
