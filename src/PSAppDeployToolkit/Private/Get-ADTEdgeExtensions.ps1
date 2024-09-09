#-----------------------------------------------------------------------------
#
# MARK: Get-ADTEdgeExtensions
#
#-----------------------------------------------------------------------------

function Get-ADTEdgeExtensions
{
    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseSingularNouns', '', Justification = "This function is appropriately named and we don't need PSScriptAnalyzer telling us otherwise.")]
    param
    (
    )

    # Check if the ExtensionSettings registry key exists if not create it.
    if (!(& $Script:CommandTable.'Test-ADTRegistryValue' -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Value ExtensionSettings))
    {
        & $Script:CommandTable.'Set-ADTRegistryKey' -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings -Value "" | & $Script:CommandTable.'Out-Null'
        return [pscustomobject]@{}
    }
    else
    {
        $extensionSettings = & $Script:CommandTable.'Get-ADTRegistryKey' -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Value ExtensionSettings
        & $Script:CommandTable.'Write-ADTLogEntry' -Message "Configured extensions: [$($extensionSettings)]." -Severity 1
        return $extensionSettings | & $Script:CommandTable.'ConvertFrom-Json'
    }
}
