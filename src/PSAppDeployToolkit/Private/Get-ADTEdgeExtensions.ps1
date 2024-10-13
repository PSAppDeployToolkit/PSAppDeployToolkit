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
    if (!(Test-ADTRegistryValue -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings))
    {
        Set-ADTRegistryKey -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings -Value "" | Out-Null
        return [pscustomobject]@{}
    }
    else
    {
        $extensionSettings = Get-ADTRegistryKey -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings
        Write-ADTLogEntry -Message "Configured extensions: [$($extensionSettings)]." -Severity 1
        return $extensionSettings | ConvertFrom-Json
    }
}
