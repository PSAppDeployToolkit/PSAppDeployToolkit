#-----------------------------------------------------------------------------
#
# MARK: Get-ADTEdgeExtensions
#
#-----------------------------------------------------------------------------

function Private:Get-ADTEdgeExtensions
{
    # Check if the ExtensionSettings registry key exists. If not, create it.
    # It is seeded with an empty object rather than nothing at all, so that the next caller reads back the
    # same empty list this one is about to report rather than a value that parses to nothing.
    if (!(Test-ADTRegistryValue -Key Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings))
    {
        Set-ADTRegistryKey -LiteralPath Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings -Value '{}' | Out-Null
        return [pscustomobject]@{}
    }
    $extensionSettings = Get-ADTRegistryKey -LiteralPath Microsoft.PowerShell.Core\Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge -Name ExtensionSettings
    Write-ADTLogEntry -Message "Configured extensions: [$($extensionSettings)]."

    # A value that is present but carries nothing means the same as no extensions at all. Callers add
    # members to what comes back and index into it, so it must never be null.
    if ([System.String]::IsNullOrWhiteSpace($extensionSettings))
    {
        return [pscustomobject]@{}
    }
    return $extensionSettings | ConvertFrom-Json
}
