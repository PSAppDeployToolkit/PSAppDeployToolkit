Function Remove-File {
	<#
.SYNOPSIS

Removes one or more items from a given path on the filesystem.

.DESCRIPTION

Removes one or more items from a given path on the filesystem.

.PARAMETER Path

Specifies the path on the filesystem to be resolved. The value of Path will accept wildcards. Will accept an array of values.

.PARAMETER LiteralPath

Specifies the path on the filesystem to be resolved. The value of LiteralPath is used exactly as it is typed; no characters are interpreted as wildcards. Will accept an array of values.

.PARAMETER Recurse

Deletes the files in the specified location(s) and in all child items of the location(s).

.PARAMETER ContinueOnError

Continue if an error is encountered. Default is: $true.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Remove-File -Path 'C:\Windows\Downloaded Program Files\Temp.inf'

.EXAMPLE

Remove-File -LiteralPath 'C:\Windows\Downloaded Program Files' -Recurse

.NOTES

.LINK

https://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory = $true, ParameterSetName = 'Path')]
		[ValidateNotNullorEmpty()]
		[String[]]$Path,
		[Parameter(Mandatory = $true, ParameterSetName = 'LiteralPath')]
		[ValidateNotNullorEmpty()]
		[String[]]$LiteralPath,
		[Parameter(Mandatory = $false)]
		[Switch]$Recurse = $false,
		[Parameter(Mandatory = $false)]
		[ValidateNotNullOrEmpty()]
		[Boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[String]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## Build hashtable of parameters/value pairs to be passed to Remove-Item cmdlet
		[Hashtable]$RemoveFileSplat = @{ 'Recurse' = $Recurse
			'Force'                                   = $true
			'ErrorVariable'                           = '+ErrorRemoveItem'
		}
		If ($ContinueOnError) {
			$RemoveFileSplat.Add('ErrorAction', 'SilentlyContinue')
		} Else {
			$RemoveFileSplat.Add('ErrorAction', 'Stop')
		}

		## Resolve the specified path, if the path does not exist, display a warning instead of an error
		If ($PSCmdlet.ParameterSetName -eq 'Path') {
			[String[]]$SpecifiedPath = $Path
		} Else {
			[String[]]$SpecifiedPath = $LiteralPath
		}
		ForEach ($Item in $SpecifiedPath) {
			Try {
				If ($PSCmdlet.ParameterSetName -eq 'Path') {
					[String[]]$ResolvedPath += Resolve-Path -Path $Item -ErrorAction 'Stop' | Where-Object { $_.Path } | Select-Object -ExpandProperty 'Path' -ErrorAction 'Stop'
				} Else {
					[String[]]$ResolvedPath += Resolve-Path -LiteralPath $Item -ErrorAction 'Stop' | Where-Object { $_.Path } | Select-Object -ExpandProperty 'Path' -ErrorAction 'Stop'
				}
			} Catch [System.Management.Automation.ItemNotFoundException] {
				Write-Log -Message "Unable to resolve file(s) for deletion in path [$Item] because path does not exist." -Severity 2 -Source ${CmdletName}
			} Catch {
				Write-Log -Message "Failed to resolve file(s) for deletion in path [$Item]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
				If (-not $ContinueOnError) {
					Throw "Failed to resolve file(s) for deletion in path [$Item]: $($_.Exception.Message)"
				}
			}
		}

		## Delete specified path if it was successfully resolved
		If ($ResolvedPath) {
			ForEach ($Item in $ResolvedPath) {
				Try {
					If (($Recurse) -and (Test-Path -LiteralPath $Item -PathType 'Container')) {
						Write-Log -Message "Deleting file(s) recursively in path [$Item]..." -Source ${CmdletName}
					} ElseIf ((-not $Recurse) -and (Test-Path -LiteralPath $Item -PathType 'Container')) {
						Write-Log -Message "Skipping folder [$Item] because the Recurse switch was not specified." -Source ${CmdletName}
						Continue
					} Else {
						Write-Log -Message "Deleting file in path [$Item]..." -Source ${CmdletName}
					}
					$null = Remove-Item @RemoveFileSplat -LiteralPath $Item
				} Catch {
					Write-Log -Message "Failed to delete file(s) in path [$Item]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
					If (-not $ContinueOnError) {
						Throw "Failed to delete file(s) in path [$Item]: $($_.Exception.Message)"
					}
				}
			}
		}

		If ($ErrorRemoveItem) {
			Write-Log -Message "The following error(s) took place while removing file(s) in path [$SpecifiedPath]. `r`n$(Resolve-Error -ErrorRecord $ErrorRemoveItem)" -Severity 2 -Source ${CmdletName}
		}
	}
	End {
		Write-FunctionHeaderOrFooter -CmdletName ${CmdletName} -Footer
	}
}
