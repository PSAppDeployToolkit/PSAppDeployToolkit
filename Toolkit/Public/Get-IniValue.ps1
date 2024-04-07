﻿Function Get-IniValue {
	<#
.SYNOPSIS

Parses an INI file and returns the value of the specified section and key.

.DESCRIPTION

Parses an INI file and returns the value of the specified section and key.

.PARAMETER FilePath

Path to the INI file.

.PARAMETER Section

Section within the INI file.

.PARAMETER Key

Key within the section of the INI file.

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

System.String

Returns the value of the specified section and key.

.EXAMPLE

Get-IniValue -FilePath "$envProgramFilesX86\IBM\Notes\notes.ini" -Section 'Notes' -Key 'KeyFileName'

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true)]
		[ValidateNotNullorEmpty()]
		[String]$FilePath,
		[Parameter(Mandatory = $true)]
		[ValidateNotNullorEmpty()]
		[String]$Section,
		[Parameter(Mandatory = $true)]
		[ValidateNotNullorEmpty()]
		[String]$Key,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullorEmpty()]
		[Boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message "Reading INI Key: [Section = $Section] [Key = $Key]." -Source ${CmdletName}

			If (-not (Test-Path -LiteralPath $FilePath -PathType 'Leaf')) {
				Throw "File [$filePath] could not be found."
			}

			$IniValue = [PSADT.IniFile]::GetIniValue($Section, $Key, $FilePath)
			Write-Log -Message "INI Key Value: [Section = $Section] [Key = $Key] [Value = $IniValue]." -Source ${CmdletName}

			Write-Output -InputObject ($IniValue)
		} Catch {
			Write-Log -Message "Failed to read INI file key value. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to read INI file key value: $($_.Exception.Message)"
			}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
