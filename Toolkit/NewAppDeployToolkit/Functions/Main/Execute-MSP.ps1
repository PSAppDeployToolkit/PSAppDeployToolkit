#region Function Execute-MSP
Function Execute-MSP {
<#
.SYNOPSIS
	Reads SummaryInfo targeted product codes in MSP file and determines if the MSP file applies to any installed products
	If a valid installed product is found, triggers the Execute-MSI function to patch the installation.
	Uses default config MSI parameters. You can use -AddParameters to add additional parameters.
.PARAMETER Path
	Path to the msp file
.PARAMETER AddParameters
	Additional parameters
.EXAMPLE
	Execute-MSP -Path 'Adobe_Reader_11.0.3_EN.msp'
.EXAMPLE
	Execute-MSP -Path 'AcroRdr2017Upd1701130143_MUI.msp' -AddParameters 'ALLUSERS=1'
.NOTES
.LINK
	http://psappdeploytoolkit.com
#>
	[CmdletBinding()]
	Param (
		[Parameter(Mandatory=$true,HelpMessage='Please enter the path to the MSP file')]
		[ValidateScript({('.msp' -contains [IO.Path]::GetExtension($_))})]
		[Alias('FilePath')]
		[string]$Path,
		[Parameter(Mandatory=$false)]
		[ValidateNotNullorEmpty()]
		[string]$AddParameters
	)

	Begin {
		## Get the name of this function and write header
		[string]${CmdletName} = $PSCmdlet.MyInvocation.MyCommand.Name
		Write-FunctionInfo -CmdletName ${CmdletName} -CmdletBoundParameters $PSBoundParameters -Header
	}
	Process {
		## If the MSP is in the Files directory, set the full path to the MSP
		If (Test-Path -LiteralPath (Join-Path -Path $dirFiles -ChildPath $path -ErrorAction 'SilentlyContinue') -PathType 'Leaf' -ErrorAction 'SilentlyContinue') {
			[string]$mspFile = Join-Path -Path $dirFiles -ChildPath $path
		}
		ElseIf (Test-Path -LiteralPath $Path -ErrorAction 'SilentlyContinue') {
			[string]$mspFile = (Get-Item -LiteralPath $Path).FullName
		}
		Else {
			Write-Log -Message "Failed to find MSP file [$path]." -Severity 3 -Source ${CmdletName}
			If (-not $ContinueOnError) {
				Throw "Failed to find MSP file [$path]."
			}
			Continue
		}
		Write-Log -Message 'Checking MSP file for valid product codes' -Source ${CmdletName}

		[boolean]$IsMSPNeeded = $false

		$Installer = New-Object -com WindowsInstaller.Installer
		$Database = $Installer.GetType().InvokeMember("OpenDatabase", "InvokeMethod", $Null, $Installer, $($mspFile,([int32]32)))
		[__comobject]$SummaryInformation = Get-ObjectProperty -InputObject $Database -PropertyName 'SummaryInformation'
		[hashtable]$SummaryInfoProperty = @{}
		$all = (Get-ObjectProperty -InputObject $SummaryInformation -PropertyName 'Property' -ArgumentList @(7)).Split(";")
		Foreach($FormattedProductCode in $all) {
			[psobject]$MSIInstalled = Get-InstalledApplication -ProductCode $FormattedProductCode
			If ($MSIInstalled) {[boolean]$IsMSPNeeded = $true }
		}
		Try { $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($SummaryInformation) } Catch { }
		Try { $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($DataBase) } Catch { }
		Try { $null = [Runtime.Interopservices.Marshal]::ReleaseComObject($Installer) } Catch { }
		If ($IsMSPNeeded) { 
			If ($AddParameters) {
				Execute-MSI -Action Patch -Path $Path -AddParameters $AddParameters
			}
			Else {
				Execute-MSI -Action Patch -Path $Path
			}
		}
	}
}
#endregion
