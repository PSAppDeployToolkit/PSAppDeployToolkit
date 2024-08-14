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
New-Variable -Name ModuleManifest -Value ([System.Management.Automation.Language.Parser]::ParseFile("$PSScriptRoot\$($MyInvocation.MyCommand.ScriptBlock.Module.Name).psd1", [ref]$null, [ref]$null).EndBlock.Statements.PipelineElements.Expression.SafeGetValue()) -Option Constant -Force -Confirm:$false
New-Variable -Name ModuleFiles -Option Constant -Value ([System.IO.FileInfo[]][System.IO.Directory]::GetFiles("$PSScriptRoot\Public"))
New-Variable -Name FunctionPaths -Option Constant -Value ($ModuleFiles.BaseName -replace '^', 'Function:')
Remove-Item -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
$ModuleFiles.FullName | . { process { . $_ } }
Set-Item -LiteralPath $FunctionPaths -Options ReadOnly
Export-ModuleMember -Function $ModuleManifest.FunctionsToExport

# State data for the Installation Progress window.
New-Variable -Name ProgressWindow -Option Constant -Value @{
    Window = $null
    Thread = $null
    Running = $false
}

# Rig up process to dispose of the ProgressWindow objects for PowerShell 7.x
$MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = {
    if ($PSVersionTable.PSEdition.Equals('Core') -and $ProgressWindow.Window)
    {
        $ProgressWindow.Window.CloseDialog()
        while (!$ProgressWindow.Thread.ThreadState.Equals([System.Threading.ThreadState]::Stopped)) {}
        $ProgressWindow.Thread = $null
        $ProgressWindow.Window = $null
    }
}
