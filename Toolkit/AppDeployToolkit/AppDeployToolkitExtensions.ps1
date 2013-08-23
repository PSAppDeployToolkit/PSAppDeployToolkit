<#
.SYNOPSIS
	This script is a template that allows you to extend the toolkit with your own custom functions.
.DESCRIPTION
	The script is automatically dot-sourced by the appdeploytoolkitmain.ps1 script.
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
$appDeployExtScriptDate = "08/16/2013"
$appDeployExtScriptParameters = $psBoundParameters

#*=============================================
#* FUNCTION LISTINGS
#*=============================================

# Your custom functions go here

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