#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3

# Add system types required by the module.
Add-Type -AssemblyName System.Windows.Forms

# Dot-source our imports and perform exports.
(Get-ChildItem -Path $PSScriptRoot\*\*.ps1).FullName.ForEach({. $_})
Export-ModuleMember -Function (Get-ChildItem -LiteralPath $PSScriptRoot\Public).BaseName

# Registry path transformation constants used within Convert-ADTRegistryPath.
New-Variable -Name ADTRegistry -Option Constant -Value ([ordered]@{
    PathMatches = [System.Array]::AsReadOnly([System.String[]]@(
        ':\\'
        ':'
        '\\'
    ))
    PathReplacements = ([ordered]@{
        '^HKLM' = 'HKEY_LOCAL_MACHINE\'
        '^HKCR' = 'HKEY_CLASSES_ROOT\'
        '^HKCU' = 'HKEY_CURRENT_USER\'
        '^HKU' = 'HKEY_USERS\'
        '^HKCC' = 'HKEY_CURRENT_CONFIG\'
        '^HKPD' = 'HKEY_PERFORMANCE_DATA\'
    }).AsReadOnly()
    WOW64Replacements = ([ordered]@{
        '^(HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\|HKEY_CURRENT_USER\\SOFTWARE\\Classes\\|HKEY_CLASSES_ROOT\\)(AppID\\|CLSID\\|DirectShow\\|Interface\\|Media Type\\|MediaFoundation\\|PROTOCOLS\\|TypeLib\\)' = '$1Wow6432Node\$2'
        '^HKEY_LOCAL_MACHINE\\SOFTWARE\\' = 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\'
        '^HKEY_LOCAL_MACHINE\\SOFTWARE$' = 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node'
        '^HKEY_CURRENT_USER\\Software\\Microsoft\\Active Setup\\Installed Components\\' = 'HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\'
    }).AsReadOnly()
}).AsReadOnly()
