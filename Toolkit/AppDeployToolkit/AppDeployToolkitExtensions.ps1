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
$appDeployExtScriptFriendlyName = "App Deploy Toolkit Extensions"
$appDeployExtScriptVersion = "1.0.0"
$appDeployExtScriptDate = "08/07/2013"

#*=============================================
#* FUNCTION LISTINGS
#*=============================================

### Place your custom functions here ###

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