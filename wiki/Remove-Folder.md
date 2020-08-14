# Remove-Folder

## SYNOPSIS

Remove folder and files if they exist.

## SYNTAX

 `Remove-Folder [-Path] <String> [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Remove folder and all files recursively in a given path.

## PARAMETERS

`-Path <String>`

Path to the folder to remove.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Remove-Folder -Path "$envWinDir\Downloaded Program Files"`

## REMARKS

To see the examples, type: `Get-Help Remove-Folder -Examples`

For more information, type: `Get-Help Remove-Folder -Detailed`

For technical information, type: `Get-Help Remove-Folder -Full`

For online help, type: `Get-Help Remove-Folder -Online`
