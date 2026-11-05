# PowerShell module changes for a new control

Read this during step 5 of the new-control skill, together with [Fluence.Wpf.PowerShell.Module/AGENTS.md](../../../../Fluence.Wpf.PowerShell.Module/AGENTS.md). All paths below are relative to `Fluence.Wpf.PowerShell.Module/` unless they start at the repository root.

The module is a script module outside `Fluence.Wpf.sln`. It loads the `Fluence.Wpf` build staged under `src/Fluence.Wpf.PowerShell/lib`, so a new control reaches scripts only after you rebuild and restage the library.

## Choose the surface

Scripts can use a control in three ways. Choose the smallest one that meets the need:

| Need | Surface | Precedent |
| --- | --- | --- |
| Scripts host the control in their own XAML window | Example only. The `fluence` XML namespace already maps `Fluence.Wpf.Controls`, so `Show-FluenceWindow` loads any public control without module code. | `DataGrid` in `examples/06-ControlsTour.ps1` |
| A dialog field collects one value with the control | A new `New-FluencePrompt -InputType` value | `Number` uses `NumberBox`; `Date` uses `DatePicker` |
| A one-line command shows the control on its own | A new public function | `Show-FluenceListSelection` wraps a `List` prompt |

An example is the default for every public control that scripts can use. An input type or a function adds public module API, so add one only when the user agreed to it.

## Always: the controls tour example

1. In `examples/06-ControlsTour.ps1`, add a `fluence:Card` with `Margin="0,0,0,16"` to the XAML here-string. Use the same layout as its neighbors: a `StackPanel` with a `BodyStrong` title `TextBlock`, then the control. Give the control an `AutomationProperties.Name`.
2. Wire any interaction in the `-Initialize` block. Find named elements with `$Window.FindName`, and set data from PowerShell objects, as the `TaskGrid` card does. Use theme resources through `DynamicResource`, and do not add hard-coded colors.
3. Add the control to the list of controls in the header comment at the top of the script.
4. Update the `06-ControlsTour.ps1` row in `examples/README.md` with what to try.
5. If the control is a common form field, also consider `examples/02-Form.ps1`.
6. Keep the file's encoding: UTF-8 with BOM and LF line endings.

No Pester test runs the examples. The PSScriptAnalyzer pass in `Test-Module.ps1` lints them, so you must open the example yourself on both hosts:

```powershell
pwsh -NoProfile -File .\examples\06-ControlsTour.ps1
powershell.exe -NoProfile -STA -File .\examples\06-ControlsTour.ps1
```

## New prompt input type

The list of input types is copied by hand across several files. `tests/InputTypeParity.Tests.ps1` reads the `New-FluencePrompt` `ValidateSet` and fails when a value has no `New-FluenceInputControl` branch, no row in `input-types.md`, or is missing from the input type count in the reference README. The `default` branch of `New-FluenceInputControl` throws for a type without its own branch. Nothing checks the help text, the dialog label condition, or validation. Change all of these together:

1. `src/Fluence.Wpf.PowerShell/Public/New-FluencePrompt.ps1`: add the value to the `-InputType` `ValidateSet` and to the `.PARAMETER InputType` help text. Add a default-value conversion to the coercion switch so that a bad default fails before a window opens. Add parameter-combination checks like the existing ones for `-MultiSelect` and `-AsPlainText`.
2. `src/Fluence.Wpf.PowerShell/Public/Get-FluenceInput.ps1`: add the same `ValidateSet` value and help text, unless the wrapper leaves the type out on purpose, as it does for `List`. `Get-FluenceInput.Tests.ps1` checks that the wrapper accepts only types that `New-FluencePrompt` accepts.
3. `src/Fluence.Wpf.PowerShell/Private/New-FluenceInputControl.ps1`: add a `switch` branch before `default`. Create the Fluence control type. Seed `$State.Result[$name]` from the control's live, typed value, so that an untouched field returns a usable value. Write changes back through a handler that uses `.GetNewClosure()`.
4. `src/Fluence.Wpf.PowerShell/Private/New-FluenceDialogWindow.ps1`: if the control shows `Message` as its own caption, as `Checkbox` and `Link` do, add the type to the condition that suppresses the separate label.
5. `src/Fluence.Wpf.PowerShell/Private/Test-FluenceInput.ps1`: check that `-ValidateNotEmpty` and `-ValidatePattern` make sense for the result type.
6. `docs/powershell/reference/input-types.md` (from the repository root): add a table row with the rendered control, the untouched result, and notes. Update the input type count in `docs/powershell/reference/README.md`.
7. Tests: add a seeding case to `tests/New-FluenceInputControl.Tests.ps1` and conversion cases to `tests/New-FluencePrompt.Tests.ps1`. When validation behavior changes, add cases to `tests/Test-FluenceInput.Tests.ps1`. Cover rendering in `tests/Show-FluenceDialog.Render.Tests.ps1`.

## New public function

1. Create `src/Fluence.Wpf.PowerShell/Public/<Verb>-Fluence<Noun>.ps1` with one function and an approved verb. Copy the BSD header comment block from an existing function file. Add `[CmdletBinding()]` and `[OutputType(...)]`. Write comment-based help with `.SYNOPSIS`, `.DESCRIPTION`, every `.PARAMETER`, at least one `.EXAMPLE`, `.OUTPUTS`, and `.NOTES`. The reference pages are generated from this help.
2. Follow the module source conventions: syntax that works in Windows PowerShell 5.1 and PowerShell 7, Allman braces, fully qualified .NET types, and UTF-8 with BOM. Route UI work through the existing private helpers, such as `Invoke-OnFluenceUi` and the window builders, instead of creating new threads or Application objects.
3. Do not use a `Fluence.Wpf` type in a parameter type constraint. The module dot-sources its functions before `Import-FluenceLibrary` loads the library, so such a constraint fails at import. Take a `[string]` with a `ValidateSet`, and convert it in a private helper such as `ConvertTo-FluenceBackdropType`. Resolve renamed library types with `Resolve-FluenceLibraryType`.
4. Add the name to `FunctionsToExport` in `src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1`. `Import-FluenceModule.Tests.ps1` compares the exported commands with this list.
5. A new output object needs a `PSTypeName` of the form `Fluence.<Noun>`, a view in `src/Fluence.Wpf.PowerShell/Formats/Fluence.Format.ps1xml`, and a section in `docs/powershell/reference/result-objects.md`.
6. Add the command to the right table in `docs/powershell/reference/README.md`. Update every count of exported functions in that file and in the module `README.md`. To find the counts, search both files for the old number.
7. Tests: add `tests/<Verb>-Fluence<Noun>.Tests.ps1` for the logic lane. Import the module, and reach private helpers through `InModuleScope` or the module object. Add `tests/<Verb>-Fluence<Noun>.Render.Tests.ps1` for windows, with `Describe ... -Tag UI` and `-Skip:($env:FLUENCE_PS_UI -ne '1')` on each case.
8. Regenerate and check the command reference. This needs the staged module and `Alt3.Docusaurus.Powershell` 2.0.1. The output goes to the ignored `artifacts/` folder and is never committed:

   ```powershell
   pwsh -NoProfile -File tools/Docs/Generate-PowerShellReference.ps1 -OutputRoot artifacts/docs/powershell/reference
   pwsh -NoProfile -File tools/Docs/Generate-PowerShellReference.ps1 -OutputRoot artifacts/docs/powershell/reference -Check
   ```

## Verify the module

Run these from the repository root after you change the library or the module:

```powershell
dotnet build Fluence.Wpf/Fluence.Wpf.csproj -c Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Build-Module.ps1 -Configuration Release
pwsh -NoProfile -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
pwsh -NoProfile -MTA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1 -SkipAnalyzer
powershell.exe -NoProfile -STA -File Fluence.Wpf.PowerShell.Module/build/Test-Module.ps1
```

These are the three logic lanes that CI runs. When you add or change render tests, also run `Test-Module.ps1 -IncludeUi` on an interactive desktop, in one uninterrupted batch on each host. A lane passes only when every assertion passes and the process exits with code zero.

Do not change the module version. The manifest version must match `Directory.Build.props`, and release preparation changes both.

In the CHANGELOG entry, name the example, the input type, or the function, so that module users can find the change.
