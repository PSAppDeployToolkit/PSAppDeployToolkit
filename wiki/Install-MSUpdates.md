# Install-MSUpdates

## SYNOPSIS

Install all Microsoft Updates in a given directory.

## SYNTAX

 `Install-MSUpdates [-Directory] <String> [<CommonParameters>]`

## DESCRIPTION

Install all Microsoft Updates of type ".exe", ".msu", or ".msp" in a given directory (recursively search directory).

## PARAMETERS

`-Directory <String>`

Directory containing the updates.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Install-MSUpdates -Directory "$dirFiles\MSUpdates"`

## REMARKS

To see the examples, type: `Get-Help Install-MSUpdates -Examples`

For more information, type: `Get-Help Install-MSUpdates -Detailed`

For technical information, type: `Get-Help Install-MSUpdates -Full`

For online help, type: `Get-Help Install-MSUpdates -Online`
