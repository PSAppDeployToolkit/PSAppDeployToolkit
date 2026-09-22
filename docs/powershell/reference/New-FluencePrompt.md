# New-FluencePrompt

Builds a single input-prompt specification for Show-FluenceDialog.

## Syntax

```text
New-FluencePrompt [-Message] <string> [-Name <string>] [-InputType <string>] [-DefaultValue <Object>] [-AsPlainText] [-ValidateSet <string[]>] [-As <string>] [-MultiSelect] [-ValidateNotEmpty] [-ValidatePattern <string>] [-ValidateScript <scriptblock>] [<CommonParameters>]
```

## Description

Returns a Fluence.Prompt object describing one input field: its name (the result key), message, input type, default value, and optional validation rules.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Name` | String | No |  | The result key under which the captured value is returned. Defaults to an auto name if omitted. |
| `-Message` | String | Yes |  | The label shown above (or beside) the input control. |
| `-InputType` | String | No | Text | One of: Text, Multiline, Password, Number, Checkbox, Toggle, Choice, List, Date, Time, FileOpen, FileSave, FolderOpen, Link. Choice renders the -ValidateSet as a combo box or radio buttons; List renders it as a list view that can also take several selections (-MultiSelect). Values: Text, Multiline, Password, Number, Checkbox, Toggle, Choice, List, Date, Time, FileOpen, FileSave, FolderOpen, Link. |
| `-DefaultValue` | Object | No |  | The initial value. For a List prompt with -MultiSelect, one item or an array of items. Password defaults must be strings and remain plaintext in the specification; omit the default when collecting a secret. SecureString defaults are rejected without decryption. |
| `-AsPlainText` | switch | No |  | Password prompts only. Return a plain string instead of the default SecureString. Assign an explicitly requested plaintext value to a variable to avoid printing it. |
| `-ValidateSet` | String[] | No |  | For Choice and List prompts, the allowed values. Required when InputType is Choice or List. |
| `-As` | String | No | Combo | For Choice prompts, how to render the set: Combo (default) or Radio. Values: Combo, Radio. |
| `-MultiSelect` | switch | No |  | For List prompts, allow more than one item to be selected. The captured value is then always an array, even for one selected item. Not valid for other input types. |
| `-ValidateNotEmpty` | switch | No |  | Require a non-whitespace value (for a List prompt, at least one selected item) before the dialog can close on a non-cancel button. Secure passwords require Length greater than zero. |
| `-ValidatePattern` | String | No |  | A regular expression the value must match. Password prompts require -AsPlainText to use it. |
| `-ValidateScript` | ScriptBlock | No |  | A scriptblock that receives the value and returns $true when valid. On a separate UI runspace it is recreated from text; caller variables, functions and closures are unavailable. Keep validators self-contained. On the caller's STA thread the live block is preserved. Password validators receive SecureString unless -AsPlainText is specified; use Length to check the number of characters. Do not retain or dispose the validator input; the module replaces it as the field changes. Required secure passwords must have at least one character. |

## Outputs

- Fluence.Prompt

## Examples

### Example 1

```powershell
New-FluencePrompt -Name User -Message 'Account name' -ValidateNotEmpty
```

### Example 2

```powershell
New-FluencePrompt -Name Features -Message 'Features' -InputType List -ValidateSet 'Core', 'Docs', 'Samples' -MultiSelect -DefaultValue 'Core'
```

## Notes

Does not require a host application; this only builds a specification object.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
