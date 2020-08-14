# Invoke-RegisterOrUnregisterDLL

## SYNOPSIS

Register or unregister a DLL file.

## SYNTAX

 `Invoke-RegisterOrUnregisterDLL [-FilePath] <String> [[-DLLAction] <String>] [[-ContinueOnError] <Boolean>] [<CommonParameters>]`

## DESCRIPTION

Register or unregister a DLL file using regsvr32.exe. Function can be invoked using alias: 'Register-DLL' or 'Unregister-DLL'.

## PARAMETERS

`-FilePath <String>`

Path to the DLL file.

`-DLLAction <String>`

Specify whether to register or unregister the DLL. Optional if function is invoked using 'Register-DLL' or 'Unregister-DLL' alias.

`-ContinueOnError <Boolean>`

Continue if an error is encountered. Default is: `$true`.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Register-DLL -FilePath "C:\Test\DcTLSFileToDMSComp.dll"`

Register DLL file using the "Register-DLL" alias for this function

-------------------------- EXAMPLE 2 --------------------------

`PS C:>UnRegister-DLL -FilePath "C:\Test\DcTLSFileToDMSComp.dll"`

Unregister DLL file using the "Unregister-DLL" alias for this function

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Invoke-RegisterOrUnregisterDLL -FilePath "C:\Test\DcTLSFileToDMSComp.dll" -DLLAction 'Register'`

Register DLL file using the actual name of this function

## REMARKS

To see the examples, type: `Get-Help Invoke-RegisterOrUnregisterDLL -Examples`

For more information, type: `Get-Help Invoke-RegisterOrUnregisterDLL -Detailed`

For technical information, type: `Get-Help Invoke-RegisterOrUnregisterDLL -Full`

For online help, type: `Get-Help Invoke-RegisterOrUnregisterDLL -Online`
