# The Core library targets .NET 8, which first shipped with PowerShell 7.4.
if ($PSEdition -eq 'Core' -and $PSVersionTable.PSVersion -lt [version]'7.4')
{
    throw 'Fluence.Wpf.PowerShell requires PowerShell 7.4 or later on Core, or Windows PowerShell 5.1.'
}

$script:ModuleRoot = $PSScriptRoot
$script:ModuleManifestPath = Join-Path $PSScriptRoot 'Fluence.Wpf.PowerShell.psd1'

# AppDomain data slot that publishes the process-wide STA UI runspace (MTA hosts) so a later module
# instance adopts it instead of starting a second UI thread; see Initialize-FluenceStaRunspace.
$script:StaRunspaceSlot = 'Fluence.Wpf.PowerShell.StaRunspace'

# AppDomain data slot recording that a theme, backdrop or accent has been applied in this process.
# It has to outlive a single module instance for the same reason as the runspace slot: on an MTA
# host the UI work runs in a second runspace with its own instance. See Initialize-FluenceApplication.
$script:ThemeSeededSlot = 'Fluence.Wpf.PowerShell.ThemeSeeded'

# Load the WPF framework assemblies before dot-sourcing functions, so that System.Windows.* types
# resolve both for the host router and for public function parameter types that are bound at
# dot-source time (for example Show-FluenceDialog's -Accent and -ParentWindow).
Add-Type -AssemblyName PresentationFramework, PresentationCore, WindowsBase

# Dot-source private then public functions.
$private = @(Get-ChildItem -LiteralPath (Join-Path $script:ModuleRoot 'Private') -Filter '*.ps1' -File -ErrorAction Stop)
$public  = @(Get-ChildItem -LiteralPath (Join-Path $script:ModuleRoot 'Public') -Filter '*.ps1' -File -ErrorAction Stop)
if ($private.Count -eq 0 -or $public.Count -eq 0)
{
    throw 'Fluence.Wpf.PowerShell package is incomplete: Private and Public must each contain function scripts.'
}
foreach ($file in @($private + $public))
{
    . $file.FullName
}

# Load the Fluence.Wpf assembly for this edition (idempotent across runspaces).
Import-FluenceLibrary -ModuleRoot $script:ModuleRoot

# Close module progress on removal. Preserve an Application-owning UI thread for later imports;
# WPF permits only one Application in an AppDomain for the lifetime of the process.
$MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = { Close-FluenceUiRunspace }

Export-ModuleMember -Function @($public | ForEach-Object { $_.BaseName })
