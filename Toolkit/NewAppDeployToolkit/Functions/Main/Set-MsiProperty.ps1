#region Function Set-MsiProperty
Function Set-MsiProperty {
<#
.SYNOPSIS
	Set a property in the MSI property table.
.DESCRIPTION
	Set a property in the MSI property table.
.PARAMETER DataBase
	Specify a ComObject representing an MSI database opened in view/modify/update mode.
.PARAMETER PropertyName
	The name of the property to be set/modified.
.PARAMETER PropertyValue
	The value of the property to be set/modified.
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Set-MsiProperty -DataBase $TempMsiPathDatabase -PropertyName 'ALLUSERS' -PropertyValue '1'
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[__comobject]$DataBase,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$PropertyName,
		[Parameter(Mandatory=$true)]
		[ValidateNotNullorEmpty()]
		[string]$PropertyValue,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[boolean]$ContinueOnError = $true
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name

		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		Try {
			Write-Log -Message "Setting the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]." -Source ${CmdletName}

			## Open the requested table view from the database
			[__comobject]$View = Invoke-ObjectMethod -InputObject $DataBase -MethodName 'OpenView' -ArgumentList @("SELECT * FROM Property WHERE Property='$PropertyName'")
			$null = Invoke-ObjectMethod -InputObject $View -MethodName 'Execute'

			## Retrieve the requested property from the requested table.
			#  https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
			[__comobject]$Record = Invoke-ObjectMethod -InputObject $View -MethodName 'Fetch'

			## Close the previous view on the MSI database
			$null = Invoke-ObjectMethod -InputObject $View -MethodName 'Close' -ArgumentList @()
			$null = [Runtime.Interopservices.Marshal]::ReleaseComObject($View)

			## Set the MSI property
			If ($Record) {
				#  If the property already exists, then create the view for updating the property
				[__comobject]$View = Invoke-ObjectMethod -InputObject $DataBase -MethodName 'OpenView' -ArgumentList @("UPDATE Property SET Value='$PropertyValue' WHERE Property='$PropertyName'")
			}
			Else {
				#  If property does not exist, then create view for inserting the property
				[__comobject]$View = Invoke-ObjectMethod -InputObject $DataBase -MethodName 'OpenView' -ArgumentList @("INSERT INTO Property (Property, Value) VALUES ('$PropertyName','$PropertyValue')")
			}
			#  Execute the view to set the MSI property
			$null = Invoke-ObjectMethod -InputObject $View -MethodName 'Execute'
		}
		Catch {
			Write-Log -Message "Failed to set the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to set the MSI Property Name [$PropertyName] with Property Value [$PropertyValue]: $($_.Exception.Message)"
			}
		}
		Finally {
			Try {
				If ($View) {
					$null = Invoke-ObjectMethod -InputObject $View -MethodName 'Close' -ArgumentList @()
					$null = [Runtime.Interopservices.Marshal]::ReleaseComObject($View)
				}
			}
			Catch { }
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
