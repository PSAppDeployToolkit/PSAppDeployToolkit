# New-MsiTransform

## SYNOPSIS

Create a transform file for an MSI database.

## SYNTAX

 `New-MsiTransform [-MsiPath] <String> [[-ApplyTransformPath] <String>] [[-NewTransformPath] <String>] [-TransformProperties] <Hashtable> [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Create a transform file for an MSI database and create/modify properties in the Properties table.

## PARAMETERS

`-MsiPath <String>`

Specify the path to an MSI file.

`-ApplyTransformPath <String>`

Specify the path to a transform which should be applied to the MSI database before any new properties are created or modified.

`-NewTransformPath <String>`

Specify the path where the new transform file with the desired properties will be created. If a transform file of the same name already exists, it will be deleted before a new one is

created.

Default is: a) If -ApplyTransformPath was specified but not -NewTransformPath, then <ApplyTransformPath>.new.mst

b) If only -MsiPath was specified, then <MsiPath>.mst

`-TransformProperties <Hashtable>`

Hashtable which contains calls to Set-MsiProperty for configuring the desired properties which should be included in new transform file.

Example hashtable: [hashtable]$TransformProperties = @{ 'ALLUSERS' = '1' }

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>[hashtable]$TransformProperties = {`

'ALLUSERS' = '1'

'AgreeToLicense' = 'Yes'

'REBOOT' = 'ReallySuppress'

'RebootYesNo' = 'No'

'ROOTDRIVE' = 'C:'

}

New-MsiTransform -MsiPath 'C:\Temp\PSADTInstall.msi' -TransformProperties $TransformProperties

## REMARKS

To see the examples, type: `Get-Help New-MsiTransform -Examples`

For more information, type: `Get-Help New-MsiTransform -Detailed`

For technical information, type: `Get-Help New-MsiTransform -Full`

For online help, type: `Get-Help New-MsiTransform -Online`
