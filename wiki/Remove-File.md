# Remove-File

## SYNOPSIS

Removes one or more items from a given path on the filesystem.

## SYNTAX

 `Remove-File -Path <String> [-Recurse] [-ContinueOnError <Boolean>] [<CommonParameters>]`

Remove-File -LiteralPath <String> [-Recurse] [-ContinueOnError <Boolean>] [<CommonParameters>]

## DESCRIPTION

Removes one or more items from a given path on the filesystem.

## PARAMETERS

`-Path <String>`

Specifies the path on the filesystem to be resolved. The value of Path will accept wildcards. Will accept an array of values.

`-LiteralPath <String>`

Specifies the path on the filesystem to be resolved. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

`-Recurse [<SwitchParameter>]`

Deletes the files in the specified location(s) and in all child items of the location(s).

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Remove-File -Path 'C:\Windows\Downloaded Program Files\Temp.inf'`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Remove-File -LiteralPath 'C:\Windows\Downloaded Program Files' -Recurse`

## REMARKS

To see the examples, type: `Get-Help Remove-File -Examples`

For more information, type: `Get-Help Remove-File -Detailed`

For technical information, type: `Get-Help Remove-File -Full`

For online help, type: `Get-Help Remove-File -Online`
