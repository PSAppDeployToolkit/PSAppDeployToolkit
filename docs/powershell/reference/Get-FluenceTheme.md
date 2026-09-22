# Get-FluenceTheme

Returns the current Fluence theme state for the process.

## Syntax

```text
Get-FluenceTheme [<CommonParameters>]
```

## Description

Reads the process-wide theme statics directly (current theme, resolved theme, current backdrop, and dark-mode flag) and returns them as an object. This is a cheap, side-effect free reader; it does not marshal onto a UI thread or create a WPF Application.

## Parameters

| Name | Type | Required | Default | Description |
| --- | --- | --- | --- | --- |

## Outputs

- Fluence.ThemeInfo

## Examples

### Example 1

```powershell
Get-FluenceTheme
```

### Example 2

```powershell
(Get-FluenceTheme).IsAppInDarkMode
```

## Notes

Reads current process theme state; does not require or create a host application.

## See also

- [Reference index](README.md)
- [Result objects](result-objects.md)
- [Input types](input-types.md)
