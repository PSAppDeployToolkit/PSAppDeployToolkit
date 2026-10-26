# Prompt input types

`New-FluencePrompt -InputType` selects the control and determines the value placed on a dialog result. `Get-FluenceInput` supports these types except `List`; use `Show-FluenceListSelection` for a standalone list. See [collect and validate input](../how-to/forms-and-validation.md) for recipes.

| Input type | Rendered control | Result when untouched | Notes |
| --- | --- | --- | --- |
| `Text` | Fluence `TextBox` | Default string or `$null`. | Single line; default input type. |
| `Multiline` | Fluence `TextBox` | Default string or `$null`. | Enter adds a line; minimum height is three lines. |
| `Password` | Styled WPF `PasswordBox` | Secure copy of string default or empty `SecureString`. | `-AsPlainText` changes result to string. |
| `Number` | Fluence `NumberBox` | Default converted to `double`, or `0`. | Result type is `double`. |
| `Checkbox` | Fluence `CheckBox` | Default converted to `bool`, or `$false`. | `Message` is the check box caption. |
| `Toggle` | Fluence `ToggleSwitch` | Default converted to `bool`, or `$false`. | On or Off control. |
| `Choice` | Fluence `ComboBox`, or radio buttons with `-As Radio` | Default string or `$null`. | Requires `-ValidateSet`. |
| `List` | Fluence `ListView` | Default selection, or empty array with `-MultiSelect`. | Requires `-ValidateSet`; multi selection always returns an array. |
| `Date` | Fluence `DatePicker` | Default `DateTime` or `$null`. | Default must convert to `datetime`. |
| `Time` | Fluence `TimePicker` | Default `TimeSpan` or `$null`. | Default must convert to `timespan`. |
| `FileOpen` | Text box and Open picker | Default path string or `$null`. | Uses `Microsoft.Win32.OpenFileDialog`. |
| `FileSave` | Text box and Save picker | Default path string or `$null`. | Uses `Microsoft.Win32.SaveFileDialog`. |
| `FolderOpen` | Text box and folder picker | Default path string or `$null`. | Uses `System.Windows.Forms.FolderBrowserDialog`. |
| `Link` | Fluence `HyperlinkButton` | URI string in `DefaultValue`. | Display only; `Message` is link text. |

`New-FluencePrompt` converts defaults for `Number`, `Date`, `Time`, `Checkbox`, and `Toggle` when the specification is created. Conversion failures occur before a window opens. Boolean defaults use `[Convert]::ToBoolean`, so the string `'false'` becomes `$false`.

## Validation rules

Validation runs for non-cancel actions in prompt order. The first failing prompt displays an error InfoBar and keeps the dialog open. A cancel button, Esc, or title bar dismissal skips validation.

| Option | Passing value |
| --- | --- |
| `-ValidateNotEmpty` | A nonblank scalar, at least one selected item for a multi select list, or a secure password with `Length` greater than zero. Password whitespace counts. |
| `-ValidatePattern <regex>` | Empty value or a value whose string form matches the pattern. Pair with `-ValidateNotEmpty` when required. Password patterns require `-AsPlainText`. |
| `-ValidateScript { param($value) ... }` | The last output is truthy. A thrown exception fails the rule. The password argument follows the selected `SecureString` or plain string return mode. |

The displayed messages are `'<Name>' is required.`, `'<Name>' does not match the required format.`, and `'<Name>' failed validation.` A password validator must not retain or dispose the secure argument supplied by the module.
