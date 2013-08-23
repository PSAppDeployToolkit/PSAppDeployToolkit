<#
.SYNOPSIS
	This script contains the functions and logic engine for the Deploy-Application.ps1 script.
.DESCRIPTION
	The script can be called directly to dot-source the toolkit functions for testing, but it is usually called by the Deploy-Application.ps1 script.
	The script can usually be updated to the latest version without impacting your per-application Deploy-Application scripts. Please check release notes before upgrading.   
.PARAMETER CleanupBlockedApps
	Clean up the blocked applications.
	This parameter is passed to the script when it is called externally from a scheduled task or Image File Execution Options.
.PARAMETER ShowBlockedAppDialog
	Allows the 3010 return code (requires restart) to be passed back to the parent process (e.g. SCCM) if detected from an installation.
	This parameter is passed to the script when it is called externally from a scheduled task or Image File Execution Options.
.PARAMETER BlockedAppInstallName
	Name of the application installation that blocked the apps initially.
	This parameter is passed to the script when it is called externally from a scheduled task or Image File Execution Options.
.NOTES
.LINK 
	Http://psappdeploytoolkit.codeplex.com
"#>

#*=============================================
#* VARIABLE DECLARATION
#*=============================================

# Variables: Script
$appDeployExtScriptFriendlyName = "App Deploy Toolkit Extensions (PwC)"
$appDeployExtScriptVersion = "2.1.0"
$appDeployExtScriptDate = "08/16/2013"
$appDeployExtScriptParameters = $psBoundParameters

#*=============================================
#* FUNCTION LISTINGS
#*=============================================

# Determines whether a database exists in the system.
Function Test-Database {
	Param (
	[string] $SQLServer,
	[string] $DBName)

	Write-Log "Checking for existence of database [$SQLServer - $DBName]..."

	$dbExists = $false
	Try {
		# we set this to null so that nothing is displayed
		$null = [Reflection.Assembly]::LoadWithPartialName("Microsoft.SqlServer.Smo")
		# Get reference to database instance
		$server = new-object ("Microsoft.SqlServer.Management.Smo.Server") $SQLServer
		ForEach($db in $server.databases) { 
			If ($db.name -eq $DBName) { 
				$dbExists = $true 
			}
		}
	}
	Catch { $dbExists = $false }

	If ($dbExists -eq $true) { Write-Log "Database [$SQLServer - $DBName] exists" } Else { Write-Log "Database [$SQLServer - $DBName] does not exist" }

	Return $dbExists
}

Function Get-SQLVersion {

	Write-Log "Getting SQL version information"
	
	$sqlVersion = New-Object PSObject

	$sqlInstances = Get-Item "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server" -ErrorAction SilentlyContinue | Get-ItemProperty | Select "InstalledInstances" -ExpandProperty "InstalledInstances"
	ForEach ($sqlInstance In $sqlInstances) {
		$sqlInstancePath = Get-Item "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL" -ErrorAction SilentlyContinue | Get-ItemProperty | Select "$sqlInstance" -ExpandProperty "$sqlInstance"
		$sqlEdition = Get-Item "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$sqlInstancePath\Setup" -ErrorAction SilentlyContinue | Get-ItemProperty | Select "Edition" -ExpandProperty "Edition"
		$sqlVersion = Get-Item "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$sqlInstancePath\Setup" -ErrorAction SilentlyContinue | Get-ItemProperty | Select "Version" -ExpandProperty "Version"
		$sqlPatchLevel = Get-Item "HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\$sqlInstancePath\Setup" -ErrorAction SilentlyContinue | Get-ItemProperty | Select "PatchLevel" -ExpandProperty "PatchLevel"
	}

	Write-Log "SQL Edition: $sqlEdition. Version: $sqlVersion. Patch Level: $sqlPatchLevel"

	$sqlVersion | Add-Member -MemberType NoteProperty -Name Edition -Value $sqlEdition
	$sqlVersion | Add-Member -MemberType NoteProperty -Name Version -Value $sqlVersion
	$sqlVersion | Add-Member -MemberType NoteProperty -Name PatchLevel -Value $sqlPatchLevel
	
	Return $sqlVersion.Version
	
}

#*=============================================
#* END FUNCTION LISTINGS
#*=============================================

#*=============================================
#* SCRIPT BODY
#*=============================================

If ($scriptParentPath -ne "") {  
	Write-Log "Script [$($MyInvocation.MyCommand.Definition)] dot-source invoked by [$(((Get-Variable MyInvocation).Value).ScriptName)]";
}
Else {
	Write-Log "Script [$($MyInvocation.MyCommand.Definition)] invoked directly";  
}

#*=============================================
#* END SCRIPT BODY
#*=============================================