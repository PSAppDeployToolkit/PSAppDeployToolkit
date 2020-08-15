# Get-FileVersion

## SYNOPSIS

Gets the version of the specified file

## SYNTAX

 `Get-FileVersion [-File] <String> [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Gets the version of the specified file

## PARAMETERS

`-File <String>`

Path of the file

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Get-FileVersion -File "$envProgramFilesX86\Adobe\Reader 11.0\Reader\AcroRd32.exe"`

## REMARKS

To see the examples, type: `Get-Help Get-FileVersion -Examples`

For more information, type: `Get-Help Get-FileVersion -Detailed`

For technical information, type: `Get-Help Get-FileVersion -Full`

For online help, type: `Get-Help Get-FileVersion -Online`
