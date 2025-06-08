#-----------------------------------------------------------------------------
#
# MARK: Get-ADTEdgeExtensions
#
#-----------------------------------------------------------------------------

function Private:Get-ADTEdgeExtensions
{
    # Check if the ExtensionSettings registry key exists. If not, create it.
    if (!(Test-ADTRegistryValue -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings))
    {
        Set-ADTRegistryKey -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings -Value "" | Out-Null
        return [pscustomobject]@{}
    }
    $extensionSettings = Get-ADTRegistryKey -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings
    Write-ADTLogEntry -Message "Configured extensions: [$($extensionSettings)]." -Severity 1
    return $extensionSettings | ConvertFrom-Json
}
