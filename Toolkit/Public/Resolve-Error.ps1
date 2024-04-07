﻿Function Resolve-Error {
	<#
.SYNOPSIS

Enumerate error record details.

.DESCRIPTION

Enumerate an error record, or a collection of error record, properties. By default, the details for the last error will be enumerated.

.PARAMETER ErrorRecord

The error record to resolve. The default error record is the latest one: $global:Error(0). This parameter will also accept an array of error records.

.PARAMETER Property

The list of properties to display from the error record. Use "*" to display all properties.

Default list of error properties is: Message, FullyQualifiedErrorId, ScriptStackTrace, PositionMessage, InnerException

.PARAMETER GetErrorRecord

Get error record details as represented by $_.

.PARAMETER GetErrorInvocation

Get error record invocation information as represented by $_.InvocationInfo.

.PARAMETER GetErrorException

Get error record exception details as represented by $_.Exception.

.PARAMETER GetErrorInnerException

Get error record inner exception details as represented by $_.Exception.InnerException. Will retrieve all inner exceptions if there is more than one.

.INPUTS

System.Array.

Accepts an array of error records.

.OUTPUTS

System.String

Displays the error record details.

.EXAMPLE

Resolve-Error

.EXAMPLE

Resolve-Error -Property *

.EXAMPLE

Resolve-Error -Property InnerException

.EXAMPLE

Resolve-Error -GetErrorInvocation:$false

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $false, Position = 0, ValueFromPipeline = $true, ValueFromPipelineByPropertyName = $true)]
		[AllowEmptyCollection()]
		[Array]$ErrorRecord,
		[Parameter(Mandatory = $false, Position = 1)]
		[ValidateNotNullorEmpty()]
		[String[]]$Property = ('Message', 'InnerException', 'FullyQualifiedErrorId', 'ScriptStackTrace', 'PositionMessage'),
		[Parameter(Mandatory = $false, Position = 2)]
		[Switch]$GetErrorRecord = $true,
		[Parameter(Mandatory = $false, Position = 3)]
		[Switch]$GetErrorInvocation = $true,
		[Parameter(Mandatory = $false, Position = 4)]
		[Switch]$GetErrorException = $true,
		[Parameter(Mandatory = $false, Position = 5)]
		[Switch]$GetErrorInnerException = $true
	)

	Begin {
		## If function was called without specifying an error record, then choose the latest error that occurred
		If (-not $ErrorRecord) {
			If ($global:Error.Count -eq 0) {
				#Write-Warning -Message "The `$Error collection is empty"
				Return
			} Else {
				[Array]$ErrorRecord = $global:Error[0]
			}
		}

		## Allows selecting and filtering the properties on the error object if they exist
		[ScriptBlock]$SelectProperty = {
			Param (
				[Parameter(Mandatory = $true)]
				[ValidateNotNullorEmpty()]
				$InputObject,
				[Parameter(Mandatory = $true)]
				[ValidateNotNullorEmpty()]
				[String[]]$Property
			)

			[String[]]$ObjectProperty = $InputObject | Get-Member -MemberType '*Property' | Select-Object -ExpandProperty 'Name'
			ForEach ($Prop in $Property) {
				If ($Prop -eq '*') {
					[String[]]$PropertySelection = $ObjectProperty
					Break
				} ElseIf ($ObjectProperty -contains $Prop) {
					[String[]]$PropertySelection += $Prop
				}
			}
			Write-Output -InputObject ($PropertySelection)
		}

		#  Initialize variables to avoid error if 'Set-StrictMode' is set
		$LogErrorRecordMsg = $null
		$LogErrorInvocationMsg = $null
		$LogErrorExceptionMsg = $null
		$LogErrorMessageTmp = $null
		$LogInnerMessage = $null
	}
	Process {
		If (-not $ErrorRecord) {
			Return
		}
		ForEach ($ErrRecord in $ErrorRecord) {
			## Capture Error Record
			If ($GetErrorRecord) {
				[String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord -Property $Property
				$LogErrorRecordMsg = $ErrRecord | Select-Object -Property $SelectedProperties
			}

			## Error Invocation Information
			If ($GetErrorInvocation) {
				If ($ErrRecord.InvocationInfo) {
					[String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.InvocationInfo -Property $Property
					$LogErrorInvocationMsg = $ErrRecord.InvocationInfo | Select-Object -Property $SelectedProperties
				}
			}

			## Capture Error Exception
			If ($GetErrorException) {
				If ($ErrRecord.Exception) {
					[String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrRecord.Exception -Property $Property
					$LogErrorExceptionMsg = $ErrRecord.Exception | Select-Object -Property $SelectedProperties
				}
			}

			## Display properties in the correct order
			If ($Property -eq '*') {
				#  If all properties were chosen for display, then arrange them in the order the error object displays them by default.
				If ($LogErrorRecordMsg) {
					[Array]$LogErrorMessageTmp += $LogErrorRecordMsg
				}
				If ($LogErrorInvocationMsg) {
					[Array]$LogErrorMessageTmp += $LogErrorInvocationMsg
				}
				If ($LogErrorExceptionMsg) {
					[Array]$LogErrorMessageTmp += $LogErrorExceptionMsg
				}
			} Else {
				#  Display selected properties in our custom order
				If ($LogErrorExceptionMsg) {
					[Array]$LogErrorMessageTmp += $LogErrorExceptionMsg
				}
				If ($LogErrorRecordMsg) {
					[Array]$LogErrorMessageTmp += $LogErrorRecordMsg
				}
				If ($LogErrorInvocationMsg) {
					[Array]$LogErrorMessageTmp += $LogErrorInvocationMsg
				}
			}

			If ($LogErrorMessageTmp) {
				$LogErrorMessage = 'Error Record:'
				$LogErrorMessage += "`n-------------"
				$LogErrorMsg = $LogErrorMessageTmp | Format-List | Out-String
				$LogErrorMessage += $LogErrorMsg
			}

			## Capture Error Inner Exception(s)
			If ($GetErrorInnerException) {
				If ($ErrRecord.Exception -and $ErrRecord.Exception.InnerException) {
					$LogInnerMessage = 'Error Inner Exception(s):'
					$LogInnerMessage += "`n-------------------------"

					$ErrorInnerException = $ErrRecord.Exception.InnerException
					$Count = 0

					While ($ErrorInnerException) {
						[String]$InnerExceptionSeperator = '~' * 40

						[String[]]$SelectedProperties = & $SelectProperty -InputObject $ErrorInnerException -Property $Property
						$LogErrorInnerExceptionMsg = $ErrorInnerException | Select-Object -Property $SelectedProperties | Format-List | Out-String

						If ($Count -gt 0) {
							$LogInnerMessage += $InnerExceptionSeperator
						}
						$LogInnerMessage += $LogErrorInnerExceptionMsg

						$Count++
						$ErrorInnerException = $ErrorInnerException.InnerException
					}
				}
			}

			If ($LogErrorMessage) {
				$Output = $LogErrorMessage
			}
			If ($LogInnerMessage) {
				$Output += $LogInnerMessage
			}

			Write-Output -InputObject $Output

			If (Test-Path -LiteralPath 'variable:Output') {
				Clear-Variable -Name 'Output'
			}
			If (Test-Path -LiteralPath 'variable:LogErrorMessage') {
				Clear-Variable -Name 'LogErrorMessage'
			}
			If (Test-Path -LiteralPath 'variable:LogInnerMessage') {
				Clear-Variable -Name 'LogInnerMessage'
			}
			If (Test-Path -LiteralPath 'variable:LogErrorMessageTmp') {
				Clear-Variable -Name 'LogErrorMessageTmp'
			}
		}
	}
	End {
	}
}
