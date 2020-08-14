# Copy-File

## SYNOPSIS

Copy a file or group of files to a destination path.

## SYNTAX

 `Copy-File [-Path] <String> [-Destination] <String> [-Recurse] [[-ContinueOnError] <Boolean>] [[-ContinueFileCopyOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Copy a file or group of files to a destination path.

## PARAMETERS

`-Path <String>`

Path of the file to copy.

`-Destination <String>`

Destination Path of the file to copy.

`-Recurse [<SwitchParameter>]`

Copy files in subdirectories.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. This will continue the deployment script, but will not continue copying files if an error is encountered. Default is: `$true`.

`-ContinueFileCopyOnError <Boolean>`

Continue copying files if an error is encountered. This will continue the deployment script and will warn about files that failed to be copied. Default is: `$false`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Copy-File -Path "$dirSupportFiles\MyApp.ini" -Destination "$envWindir\MyApp.ini"`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Copy-File -Path "$dirSupportFiles\\*.\*" -Destination "$envTemp\tempfiles"`

Copy all of the files in a folder to a destination folder.

## REMARKS

To see the examples, type: `Get-Help Copy-File -Examples`

For more information, type: `Get-Help Copy-File -Detailed`

For technical information, type: `Get-Help Copy-File -Full`

For online help, type: `Get-Help Copy-File -Online`
