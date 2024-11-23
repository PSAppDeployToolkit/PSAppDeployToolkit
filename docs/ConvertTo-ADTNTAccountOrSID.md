---
external help file: PSAppDeployToolkit-help.xml
Module Name: PSAppDeployToolkit
online version: https://psappdeploytoolkit.com
schema: 2.0.0
---

# ConvertTo-ADTNTAccountOrSID

## SYNOPSIS
Convert between NT Account names and their security identifiers (SIDs).

## SYNTAX

### NTAccountToSID
```
ConvertTo-ADTNTAccountOrSID -AccountName <String> [<CommonParameters>]
```

### SIDToNTAccount
```
ConvertTo-ADTNTAccountOrSID -SID <String> [<CommonParameters>]
```

### WellKnownName
```
ConvertTo-ADTNTAccountOrSID -WellKnownSIDName <String> [-WellKnownToNTAccount] [-LocalHost]
 [<CommonParameters>]
```

## DESCRIPTION
Specify either the NT Account name or the SID and get the other.
Can also convert well known sid types.

## EXAMPLES

### EXAMPLE 1
```
ConvertTo-ADTNTAccountOrSID -AccountName 'CONTOSO\User1'
```

Converts a Windows NT Account name to the corresponding SID.

### EXAMPLE 2
```
ConvertTo-ADTNTAccountOrSID -SID 'S-1-5-21-1220945662-2111687655-725345543-14012660'
```

Converts a Windows NT Account SID to the corresponding NT Account Name.

### EXAMPLE 3
```
ConvertTo-ADTNTAccountOrSID -WellKnownSIDName 'NetworkServiceSid'
```

Converts a Well Known SID name to a SID.

## PARAMETERS

### -AccountName
The Windows NT Account name specified in \<domain\>\\\<username\> format.

Use fully qualified account names (e.g., \<domain\>\\\<username\>) instead of isolated names (e.g, \<username\>) because they are unambiguous and provide better performance.

```yaml
Type: String
Parameter Sets: NTAccountToSID
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -SID
The Windows NT Account SID.

```yaml
Type: String
Parameter Sets: SIDToNTAccount
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WellKnownSIDName
Specify the Well Known SID name translate to the actual SID (e.g., LocalServiceSid).

To get all well known SIDs available on system: \[Enum\]::GetNames(\[Security.Principal.WellKnownSidType\])

```yaml
Type: String
Parameter Sets: WellKnownName
Aliases:

Required: True
Position: Named
Default value: None
Accept pipeline input: True (ByPropertyName)
Accept wildcard characters: False
```

### -WellKnownToNTAccount
Convert the Well Known SID to an NTAccount name.

```yaml
Type: SwitchParameter
Parameter Sets: WellKnownName
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### -LocalHost
Avoids a costly domain check when only converting local accounts.

```yaml
Type: SwitchParameter
Parameter Sets: WellKnownName
Aliases:

Required: False
Position: Named
Default value: False
Accept pipeline input: False
Accept wildcard characters: False
```

### CommonParameters
This cmdlet supports the common parameters: -Debug, -ErrorAction, -ErrorVariable, -InformationAction, -InformationVariable, -OutVariable, -OutBuffer, -PipelineVariable, -Verbose, -WarningAction, and -WarningVariable. For more information, see [about_CommonParameters](http://go.microsoft.com/fwlink/?LinkID=113216).

## INPUTS

### System.String
### Accepts a string containing the NT Account name or SID.
## OUTPUTS

### System.String
### Returns the NT Account name or SID.
## NOTES
An active ADT session is NOT required to use this function.

The conversion can return an empty result if the user account does not exist anymore or if translation fails Refer to: http://blogs.technet.com/b/askds/archive/2011/07/28/troubleshooting-sid-translation-failures-from-the-obvious-to-the-not-so-obvious.aspx

Tags: psadt
Website: https://psappdeploytoolkit.com
Copyright: (C) 2024 PSAppDeployToolkit Team (Sean Lillis, Dan Cunningham, Muhammad Mashwani, Mitch Richters, Dan Gough).
License: https://opensource.org/license/lgpl-3-0

## RELATED LINKS

[https://psappdeploytoolkit.com](https://psappdeploytoolkit.com)

[http://msdn.microsoft.com/en-us/library/system.security.principal.wellknownsidtype(v=vs.110).aspx](http://msdn.microsoft.com/en-us/library/system.security.principal.wellknownsidtype(v=vs.110).aspx)
