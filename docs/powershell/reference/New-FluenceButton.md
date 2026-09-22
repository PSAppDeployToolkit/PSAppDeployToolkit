# New-FluenceButton

Builds a single button specification for Show-FluenceDialog.

## Syntax

```text
New-FluenceButton [-Text] <string> [-Name <string>] [-IsDefault] [-IsCancel] [<CommonParameters>]
```

## Description

Returns a Fluence.Button object carrying the caption, the result key, and the default and cancel flags. Pass one or more to Show-FluenceDialog -Buttons; a plain string there is shorthand for a button with no flags, so this cmdlet is what you use to mark one.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |
| `-Text` | String | Yes |  | The button caption (and the default result key). |
| `-Name` | String | No |  | The result key; defaults to Text. |
| `-IsDefault` | switch | No |  | Mark as the default button (activated by Enter). |
| `-IsCancel` | switch | No |  | Mark as the cancel button (activated by Esc; skips input validation). |

## Outputs

- Fluence.Button

## Examples

### Example 1

```powershell
New-FluenceButton -Text 'Sign in' -Name Login -IsDefault
```

### Example 2

```powershell
Show-FluenceDialog -Message 'Delete the folder?' -Buttons (New-FluenceButton 'Delete' -IsDefault), (New-FluenceButton 'Keep' -IsCancel)
```

## Notes

Does not require a host application.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
