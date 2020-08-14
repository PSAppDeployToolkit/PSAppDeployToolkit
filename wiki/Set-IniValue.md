# Set-IniValue

## SYNOPSIS

Opens an INI file and sets the value of the specified section and key.

## SYNTAX

 `Set-IniValue [-FilePath] <String> [-Section] <String> [-Key] <String> [-Value] <Object> [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Opens an INI file and sets the value of the specified section and key.

## PARAMETERS

`-FilePath <String>`

Path to the INI file.

`-Section <String>`

Section within the INI file.

`-Key <String>`

Key within the section of the INI file.

`-Value <Object>`

Value for the key within the section of the INI file. To remove a value, set this variable to $null.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Set-IniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName' -Value 'MyFile.ID'`

## REMARKS

To see the examples, type: `Get-Help Set-IniValue -Examples`

For more information, type: `Get-Help Set-IniValue -Detailed`

For technical information, type: `Get-Help Set-IniValue -Full`

For online help, type: `Get-Help Set-IniValue -Online`
