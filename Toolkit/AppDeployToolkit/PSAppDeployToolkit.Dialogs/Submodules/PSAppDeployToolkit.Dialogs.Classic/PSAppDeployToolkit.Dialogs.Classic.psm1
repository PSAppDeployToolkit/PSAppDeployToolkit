#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3

# Set process DPI awareness before importing anything else.
Set-ADTProcessDpiAware

# Add system types required by the module.
Add-Type -AssemblyName System.Drawing, System.Windows.Forms, PresentationCore, PresentationFramework, WindowsBase

# All WinForms-specific initialistion code.
[System.Windows.Forms.Application]::EnableVisualStyles()
try {[System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)} catch {$null = $null}

# Dot-source our imports and perform exports.
Export-ModuleMember -Function (Get-ChildItem -Path $PSScriptRoot\*\*.ps1).ForEach({
    # As we declare all functions read-only, attempt removal before dot-sourcing the function again.
    Remove-Item -LiteralPath "Function:$($_.BaseName)" -Force -ErrorAction Ignore

    # Dot source in the function code.
    . $_.FullName

    # Mark the dot-sourced function as read-only.
    Set-Item -LiteralPath "Function:$($_.BaseName)" -Options ReadOnly

    # Echo out the public functions.
    if ($_.DirectoryName.EndsWith('Public'))
    {
        return $_.BaseName
    }
})

# WinForms global data.
New-Variable -Name FormData -Option Constant -Value @{
    Font = [System.Drawing.SystemFonts]::MessageBoxFont
    Width = 450
    BannerHeight = 0
    Assets = @{
        Icon = $null
        Logo = $null
        Banner = $null
    }
}

# State data for the Installation Progress window.
New-Variable -Name ProgressWindow -Option Constant -Value @{
    SyncHash = [System.Collections.Hashtable]::Synchronized(@{})
    PowerShell = $null
    Invocation = $null
    Running = $false
}
