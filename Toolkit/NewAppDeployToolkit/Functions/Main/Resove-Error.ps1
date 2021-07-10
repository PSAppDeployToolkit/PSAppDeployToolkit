#region Function Resolve-Error
Function Resolve-Error {
<#
.SYNOPSIS
	Enumerate error record details.
.DESCRIPTION
	Enumerate an error record, or a collection of error record, properties. By default, the details for the last error will be enumerated.
.PARAMETER ErrorRecord
	The error record to resolve. The default error record is the latest one: $global:Error[0]. This parameter will also accept an array of error records.
.PARAMETER GetErrorRecord
	Get error record details as represented by $_.
.PARAMETER GetErrorInvocation
	Get error record invocation information as represented by $_.InvocationInfo.
.PARAMETER GetErrorException
	Get error record exception details as represented by $_.Exception.
.PARAMETER GetErrorInnerException
	Get error record inner exception details as represented by $_.Exception.InnerException. Will retrieve all inner exceptions if there is more than one.
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
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$false,Position=0,ValueFromPipeline=$true,ValueFromPipelineByPropertyName=$true)]
		[AllowEmptyCollection()]
		[Management.Automation.ErrorRecord[]]$ErrorRecord = $global:Error[0],
		[Parameter(Mandatory=$false,Position=1)]
		[switch]$GetErrorRecord = $true,
		[Parameter(Mandatory=$false,Position=2)]
		[switch]$GetErrorInvocation = $true,
		[Parameter(Mandatory=$false,Position=3)]
		[switch]$GetErrorException = $true,
		[Parameter(Mandatory=$false,Position=4)]
		[switch]$GetErrorInnerException = $true
	)

	If (-not $ErrorRecord) {
		If ($global:Error.Count -eq 0) {
			Return
		} Else {
			[Management.Automation.ErrorRecord[]]$ErrorRecord = $global:Error[0]
		}
	}
	[Collections.Generic.List[string]]$SOutput = New-Object Collections.Generic.List[string]
	# Do not use newline character in here because it will not be shown consistently in the console and the log. Each $SOutput.Add() is a new line
	for ($i = 0; $i -lt $ErrorRecord.Count; $i++) {
		If ($ErrorRecord.Count -le 1) {
			$SOutput.Add("Error Record:")
		} else {
			$SOutput.Add("Error Record $($i+1):")
		}
		$SOutput.Add("------↓------")
		$ErrRecord = $ErrorRecord[$i]
		## Capture Error Exception
		If ($GetErrorException -and $ErrRecord.Exception.Message) {
			If ($ErrRecord.Exception.Message -eq $ErrRecord.FullyQualifiedErrorId) {
				$SOutput.Add("Exception.Message/FullyQualifiedErrorId: $($ErrRecord.Exception.Message)")
				$SOutput.Add([String]::Empty)
			} Else {
				$SOutput.Add("Exception.Message: $($ErrRecord.Exception.Message)")
				$SOutput.Add([String]::Empty)
				$SOutput.Add("FullyQualifiedErrorId: $($ErrRecord.FullyQualifiedErrorId)")
				$SOutput.Add([String]::Empty)
			}
		}
		## Capture Error Record
		If ($GetErrorRecord) {
			$SOutput.Add("ScriptStackTrace: ")
			$SOutput.Add($ErrRecord.ScriptStackTrace)
			$SOutput.Add([String]::Empty)
		}
		## Error Invocation Information
		If ($GetErrorInvocation -and $ErrRecord.InvocationInfo) {
			$SOutput.Add("InvocationInfo.PositionMessage: ")
			$SOutput.Add($ErrRecord.InvocationInfo.PositionMessage)
			$SOutput.Add([String]::Empty)
		}
		## Capture Error Inner Exception(s)
		If ($GetErrorInnerException -and $ErrRecord.Exception.InnerException) {
			$SOutput.Add("Error Inner Exception`(s`): ")
			$SOutput.Add("-------------------------")
			$ErrorInnerException = $ErrRecord.Exception.InnerException
			$Count = 0

			While ($ErrorInnerException) {
				[string]$InnerExceptionSeperator = '~' * 25

				If ($Count -gt 0) { $SOutput.Add($InnerExceptionSeperator) }
				$SOutput.Add("InnerException.Message: $($ErrorInnerException.Message)")
				$SOutput.Add([String]::Empty)
				$SOutput.Add("InnerException.Source: $($ErrorInnerException.Source)")
				$SOutput.Add([String]::Empty)
				$SOutput.Add("InnerException.StackTrace: ")
				$SOutput.Add($ErrorInnerException.StackTrace)
				$SOutput.Add([String]::Empty)
				$Count++
				$ErrorInnerException = $ErrorInnerException.InnerException
			}
		}
		$ErrRecord = $null
	}
	#remove trailing newline
	if([String]::IsNullOrEmpty($SOutput[$SOutput.Count-1])) {
		$SOutput.RemoveAt($SOutput.Count-1)
	}
	$SOutput.Add("------↑------")
	$SOutput | Out-String
	$SOutput = $null
}
#endregion
