# Resolve-Error

## SYNOPSIS

Enumerate error record details.

## SYNTAX

 `Resolve-Error [[-ErrorRecord] <Array>] [[-Property] <String>] [[-GetErrorRecord]] [[-GetErrorInvocation]] [[-GetErrorException]] [[-GetErrorInnerException]] [<CommonParameters>]`

## DESCRIPTION

Enumerate an error record, or a collection of error record, properties. By default, the details for the last error will be enumerated.

## PARAMETERS

`-ErrorRecord <Array>`

The error record to resolve. The default error record is the latest one: $global:Error[0]. This parameter will also accept an array of error records.

`-Property <String>`

The list of properties to display from the error record. Use "\*" to display all properties.

Default list of error properties is: Message, FullyQualifiedErrorId, ScriptStackTrace, PositionMessage, InnerException

`-GetErrorRecord [<SwitchParameter>]`

Get error record details as represented by $_.

`-GetErrorInvocation [<SwitchParameter>]`

Get error record invocation information as represented by $_.InvocationInfo.

`-GetErrorException [<SwitchParameter>]`

Get error record exception details as represented by $_.Exception.

`-GetErrorInnerException [<SwitchParameter>]`

Get error record inner exception details as represented by $_.Exception.InnerException. Will retrieve all inner exceptions if there is more than one.

<CommonParameters>

This cmdlet supports the common parameters: Verbose, Debug, ErrorAction, ErrorVariable, WarningAction, WarningVariable, OutBuffer, PipelineVariable, and OutVariable. For more information, see [about_CommonParameters](https:/go.microsoft.com/fwlink/?LinkID=113216).

-------------------------- EXAMPLE 1 --------------------------

`PS C:>Resolve-Error`

-------------------------- EXAMPLE 2 --------------------------

`PS C:>Resolve-Error -Property \*`

-------------------------- EXAMPLE 3 --------------------------

`PS C:>Resolve-Error -Property InnerException`

-------------------------- EXAMPLE 4 --------------------------

`PS C:>Resolve-Error -GetErrorInvocation:`$false``

## REMARKS

To see the examples, type: `Get-Help Resolve-Error -Examples`

For more information, type: `Get-Help Resolve-Error -Detailed`

For technical information, type: `Get-Help Resolve-Error -Full`

For online help, type: `Get-Help Resolve-Error -Online`
