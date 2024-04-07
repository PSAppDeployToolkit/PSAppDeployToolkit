﻿Function Get-UniversalDate {
	<#
.SYNOPSIS

Returns the date/time for the local culture in a universal sortable date time pattern.

.DESCRIPTION

Converts the current datetime or a datetime string for the current culture into a universal sortable date time pattern, e.g. 2013-08-22 11:51:52Z

.PARAMETER DateTime

Specify the DateTime in the current culture.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default: $false.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the date/time for the local culture in a universal sortable date time pattern.

.EXAMPLE

Get-UniversalDate

Returns the current date in a universal sortable date time pattern.

.EXAMPLE

Get-UniversalDate -DateTime '25/08/2013'

Returns the date for the current culture in a universal sortable date time pattern.

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		#  Get the current date
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[String]$DateTime = ((Get-Date -Format ($culture).DateTimeFormat.UniversalDateTimePattern).ToString()),
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Boolean]$ContinueOnError = $false
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			## If a universal sortable date time pattern was provided, remove the Z, otherwise it could get converted to a different time zone.
			If ($DateTime -match 'Z$') {
				$DateTime = $DateTime -replace 'Z$', ''
			}
			[DateTime]$DateTime = [DateTime]::Parse($DateTime, $culture)

			## Convert the date to a universal sortable date time pattern based on the current culture
			Write-Log -Message "Converting the date [$DateTime] to a universal sortable date time pattern based on the current culture [$($culture.Name)]." -Source ${CmdletName}
			[String]$universalDateTime = (Get-Date -Date $DateTime -Format ($culture).DateTimeFormat.UniversalSortableDateTimePattern -ErrorAction 'Stop').ToString()
			Write-Output -InputObject ($universalDateTime)
		} Catch {
			Write-Log -Message "The specified date/time [$DateTime] is not in a format recognized by the current culture [$($culture.Name)]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "The specified date/time [$DateTime] is not in a format recognized by the current culture: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
