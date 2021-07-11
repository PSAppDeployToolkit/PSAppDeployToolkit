#region Function Get-MsiTableProperty
Function Get-MsiTableProperty {
<#
.SYNOPSIS
	Get all of the properties from a Windows Installer database table or the Summary Information stream and return as a custom object.
.DESCRIPTION
	Use the Windows Installer object to read all of the properties from a Windows Installer database table or the Summary Information stream.
.PARAMETER Path
	The fully qualified path to an database file. Supports .msi and .msp files.
.PARAMETER TransformPath
	The fully qualified path to a list of MST file(s) which should be applied to the MSI file.
.PARAMETER Table
	The name of the the MSI table from which all of the properties must be retrieved. Default is: 'Property'.
.PARAMETER TablePropertyNameColumnNum
	Specify the table column number which contains the name of the properties. Default is: 1 for MSIs and 2 for MSPs.
.PARAMETER TablePropertyValueColumnNum
	Specify the table column number which contains the value of the properties. Default is: 2 for MSIs and 3 for MSPs.
.PARAMETER GetSummaryInformation
	Retrieves the Summary Information for the Windows Installer database.
	Summary Information property descriptions: https://msdn.microsoft.com/en-us/library/aa372049(v=vs.85).aspx
.PARAMETER ContinueOnError
	Continue if an error is encountered. Default is: $true.
.EXAMPLE
	Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst'
	Retrieve all of the properties from the default 'Property' table.
.EXAMPLE
	Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -TransformPath 'C:\Package\AppDeploy.mst' -Table 'Property' | Select-Object -ExpandProperty ProductCode
	Retrieve all of the properties from the 'Property' table and then pipe to Select-Object to select the ProductCode property.
.EXAMPLE
	Get-MsiTableProperty -Path 'C:\Package\AppDeploy.msi' -GetSummaryInformation
	Retrieves the Summary Information for the Windows Installer database.
.NOTES
	This is an internal script function and should typically not be called directly.
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding(DefaultParameterSetName='TableInfo')]
	Param (
		[Parameter(Mandatory=$true)]
		[ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[ValidateScript({ Test-Path -LiteralPath $_ -PathType 'Leaf' })]
		[string[]]$TransformPath,
		[Parameter(Mandatory=$false,ParameterSetName='TableInfo')]
		[ValidateNotNullOrEmpty()]
		[string]$Table = $(If ([IO.Path]::GetExtension($Path) -eq '.msi') { 'Property' } Else { 'MsiPatchMetadata' }),
		[Parameter(Mandatory=$false,ParameterSetName='TableInfo')]
		[ValidateNotNullorEmpty()]
		[int32]$TablePropertyNameColumnNum = $(If ([IO.Path]::GetExtension($Path) -eq '.msi') { 1 } Else { 2 }),
		[Parameter(Mandatory=$false,ParameterSetName='TableInfo')]
		[ValidateNotNullorEmpty()]
		[int32]$TablePropertyValueColumnNum = $(If ([IO.Path]::GetExtension($Path) -eq '.msi') { 2 } Else { 3 }),
		[Parameter(Mandatory=$true,ParameterSetName='SummaryInfo')]
		[ValidateNotNullorEmpty()]
		[switch]$GetSummaryInformation = $false,
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
			If ($PSCmdlet.ParameterSetName -eq 'TableInfo') {
				Write-Log -Message "Reading data from Windows Installer database file [$Path] in table [$Table]." -Source ${CmdletName}
			}
			Else {
				Write-Log -Message "Reading the Summary Information from the Windows Installer database file [$Path]." -Source ${CmdletName}
			}

			## Create a Windows Installer object
			[__comobject]$Installer = New-Object -ComObject 'WindowsInstaller.Installer' -ErrorAction 'Stop'
			## Determine if the database file is a patch (.msp) or not
			If ([IO.Path]::GetExtension($Path) -eq '.msp') { [boolean]$IsMspFile = $true }
			## Define properties for how the MSI database is opened
			[int32]$msiOpenDatabaseModeReadOnly = 0
			[int32]$msiSuppressApplyTransformErrors = 63
			[int32]$msiOpenDatabaseMode = $msiOpenDatabaseModeReadOnly
			[int32]$msiOpenDatabaseModePatchFile = 32
			If ($IsMspFile) { [int32]$msiOpenDatabaseMode = $msiOpenDatabaseModePatchFile }
			## Open database in read only mode
			[__comobject]$Database = Invoke-ObjectMethod -InputObject $Installer -MethodName 'OpenDatabase' -ArgumentList @($Path, $msiOpenDatabaseMode)
			## Apply a list of transform(s) to the database
			If (($TransformPath) -and (-not $IsMspFile)) {
				ForEach ($Transform in $TransformPath) {
					$null = Invoke-ObjectMethod -InputObject $Database -MethodName 'ApplyTransform' -ArgumentList @($Transform, $msiSuppressApplyTransformErrors)
				}
			}

			## Get either the requested windows database table information or summary information
			If ($PSCmdlet.ParameterSetName -eq 'TableInfo') {
				## Open the requested table view from the database
				[__comobject]$View = Invoke-ObjectMethod -InputObject $Database -MethodName 'OpenView' -ArgumentList @("SELECT * FROM $Table")
				$null = Invoke-ObjectMethod -InputObject $View -MethodName 'Execute'

				## Create an empty object to store properties in
				[psobject]$TableProperties = New-Object -TypeName 'PSObject'

				## Retrieve the first row from the requested table. If the first row was successfully retrieved, then save data and loop through the entire table.
				#  https://msdn.microsoft.com/en-us/library/windows/desktop/aa371136(v=vs.85).aspx
				[__comobject]$Record = Invoke-ObjectMethod -InputObject $View -MethodName 'Fetch'
				While ($Record) {
					#  Read string data from record and add property/value pair to custom object
					$TableProperties | Add-Member -MemberType 'NoteProperty' -Name (Get-ObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @($TablePropertyNameColumnNum)) -Value (Get-ObjectProperty -InputObject $Record -PropertyName 'StringData' -ArgumentList @($TablePropertyValueColumnNum)) -Force
					#  Retrieve the next row in the table
					[__comobject]$Record = Invoke-ObjectMethod -InputObject $View -MethodName 'Fetch'
				}
				Write-Output -InputObject $TableProperties
			}
			Else {
				## Get the SummaryInformation from the windows installer database
				[__comobject]$SummaryInformation = Get-ObjectProperty -InputObject $Database -PropertyName 'SummaryInformation'
				[hashtable]$SummaryInfoProperty = @{}
				## Summary property descriptions: https://msdn.microsoft.com/en-us/library/aa372049(v=vs.85).aspx
				$SummaryInfoProperty.Add('CodePage', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(1)))
				$SummaryInfoProperty.Add('Title', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(2)))
				$SummaryInfoProperty.Add('Subject', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(3)))
				$SummaryInfoProperty.Add('Author', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(4)))
				$SummaryInfoProperty.Add('Keywords', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(5)))
				$SummaryInfoProperty.Add('Comments', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(6)))
				$SummaryInfoProperty.Add('Template', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(7)))
				$SummaryInfoProperty.Add('LastSavedBy', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(8)))
				$SummaryInfoProperty.Add('RevisionNumber', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(9)))
				$SummaryInfoProperty.Add('LastPrinted', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(11)))
				$SummaryInfoProperty.Add('CreateTimeDate', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(12)))
				$SummaryInfoProperty.Add('LastSaveTimeDate', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(13)))
				$SummaryInfoProperty.Add('PageCount', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(14)))
				$SummaryInfoProperty.Add('WordCount', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(15)))
				$SummaryInfoProperty.Add('CharacterCount', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(16)))
				$SummaryInfoProperty.Add('CreatingApplication', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(18)))
				$SummaryInfoProperty.Add('Security', (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(19)))
				[psobject]$SummaryInfoProperties = New-Object -TypeName 'PSObject' -Property $SummaryInfoProperty
				Write-Output -InputObject $SummaryInfoProperties
			}
		}
		Catch {
			Write-Log -Message "Failed to get the MSI table [$Table]. `r`n$(Resolve-Error)" -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to get the MSI table [$Table]: $($_.Exception.Message)"
			}
		}
		Finally {
			Try {
				If ($View) {
					$null = Invoke-ObjectMethod -InputObject $View -MethodName 'Close' -ArgumentList @()
					Try { $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($View) } Catch { }
				}
				ElseIf ($SummaryInformation) {
					Try { $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($SummaryInformation) } Catch { }
				}
			}
			Catch { }
			Try { $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($DataBase) } Catch { }
			Try { $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer) } Catch { }
		}
	}
	End {
		Write-FunctionInfo -CmdletName ${CmdletName} -Footer
	}
}
#endregion
