---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# Test-ADTServiceExists

## SYNOPSIS
Check to see if a service exists.

## SYNTAX

```
Test-ADTServiceExists [-Name] <String> [-UseCIM] [-PassThru] [<CommonParameters>]
```

## DESCRIPTION
Check to see if a service exists.
The UseCIM switch can be used in conjunction with PassThru to return WMI objects for PSADT v3.x compatibility, however, this method fails in Windows Sandbox.

## EXAMPLES

### EXAMPLE 1
```
Test-ADTServiceExists -Name 'wuauserv'
```

Checks if the service 'wuauserv' exists.

### EXAMPLE 2
```
Test-ADTServiceExists -Name 'testservice' -PassThru | Where-Object { $_ } | ForEach-Object { $_.Delete() }
```

Checks if a service exists and then deletes it by using the -PassThru parameter.

## PARAMETERS

### -Name
Specify the name of the service.

Note: Service name can be found by executing "Get-Service | Format-Table -AutoSize -Wrap" or by using the properties screen of a service in services.msc.

```yaml
Type: String
Parameter Sets: (All)
Aliases:

Required: True
Position: 1
Default value: None
Accept pipeline input: False
Accept wildcard characters: False
```

### -UseCIM
Use CIM/WMI to check for the service.
This is useful for compatibility with PSADT v3.x.

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases: UseWMI

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -PassThru
Return the WMI service object.
To see all the properties use: Test-ADTServiceExists -Name 'spooler' -PassThru | Get-Member

```yaml
Type: SwitchParameter
Parameter Sets: (All)
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable.
For more information, see about_CommonParameters (http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### None
### You cannot pipe objects to this function.
## OUTPUTS

### System.Boolean
### Returns $true if the service exists, otherwise returns $false.
## NOTES
An active ADT session is NOT required to use this function.

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (c) 2024 PSAppDeployToolkit Team, licensed under LGPLv3
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)
