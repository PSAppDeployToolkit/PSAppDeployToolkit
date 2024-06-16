#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3

# Dot-source our imports and perform exports.
(Get-ChildItem -Path $PSScriptRoot\Public\*.ps1).FullName.ForEach({. $_})
Export-ModuleMember -Function (Get-ChildItem -LiteralPath $PSScriptRoot\Public).BaseName

# State data for the Installation Progress window.
New-Variable -Name ProgressWindow -Option Constant -Value @{
    Window = $null
    Thread = $null
    Running = $false
}

# Rig up process to dispose of the ProgressWindow objects for PowerShell 7.x
$MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = {
    if ($PSVersionTable.PSEdition.Equals('Core'))
    {
        $ProgressWindow.Window.CloseDialog()
        while (!$ProgressWindow.Thread.ThreadState.Equals([System.Threading.ThreadState]::Stopped)) {}
        $ProgressWindow.Thread = $null
        $ProgressWindow.Window = $null
    }
}
