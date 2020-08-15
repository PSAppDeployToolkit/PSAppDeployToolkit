# Get-IniValue

## SYNOPSIS

Parses an INI file and returns the value of the specified section and key.

## SYNTAX

 `Get-IniValue [-FilePath] <String> [-Section] <String> [-Key] <String> [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Parses an INI file and returns the value of the specified section and key.

## PARAMETERS

`-FilePath <String>`

Path to the INI file.

`-Section <String>`

Section within the INI file.

`-Key <String>`

Key within the section of the INI file.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-IniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName'`

## REMARKS

To see the examples, type: `Get-Help Get-IniValue -Examples`

For more information, type: `Get-Help Get-IniValue -Detailed`

For technical information, type: `Get-Help Get-IniValue -Full`

For online help, type: `Get-Help Get-IniValue -Online`
