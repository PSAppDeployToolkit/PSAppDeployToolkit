# New-Folder

## SYNOPSIS

Create a new folder.

## SYNTAX

 `New-Folder [-Path] <String> [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Create a new folder if it does not exist.

## PARAMETERS

`-Path <String>`

Path to the new folder to create.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>New-Folder -Path "$envWinDir\System32"`

## REMARKS

To see the examples, type: `Get-Help New-Folder -Examples`

For more information, type: `Get-Help New-Folder -Detailed`

For technical information, type: `Get-Help New-Folder -Full`

For online help, type: `Get-Help New-Folder -Online`
