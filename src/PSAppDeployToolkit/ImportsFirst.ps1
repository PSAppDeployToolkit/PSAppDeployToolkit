#---------------------------------------------------------------------------
#
# Initial module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue

# Build out lookup table for all cmdlets used within module, starting with the core cmdlets.
$ModuleManifest = [System.Management.Automation.Language.Parser]::ParseFile("$PSScriptRoot\PSAppDeployToolkit.psd1", [ref]$null, [ref]$null).EndBlock.Statements.PipelineElements.Expression.SafeGetValue()
$CommandTable = [ordered]@{}; $ExecutionContext.SessionState.InvokeCommand.GetCmdlets() | & { process { if ($_.PSSnapIn -and $_.PSSnapIn.Name.Equals('Microsoft.PowerShell.Core') -and $_.PSSnapIn.IsDefault) { $CommandTable.Add($_.Name, $_) } } }
& $CommandTable.'Get-Command' -FullyQualifiedModule $ModuleManifest.RequiredModules | & { process { $CommandTable.Add($_.Name, $_) } }
& $CommandTable.'New-Variable' -Name CommandTable -Value $CommandTable.AsReadOnly() -Option Constant -Force -Confirm:$false
& $CommandTable.'New-Variable' -Name ModuleManifest -Value $ModuleManifest -Option Constant -Force -Confirm:$false

# Ensure module operates under the strictest of conditions.
& $CommandTable.'Set-StrictMode' -Version 3

# Add the custom types required for the toolkit.
& $CommandTable.'Add-Type' -LiteralPath "$PSScriptRoot\PSAppDeployToolkit.cs" -ReferencedAssemblies $(
    'System.DirectoryServices'
    if ($PSVersionTable.PSEdition.Equals('Core'))
    {
        'System.Net.NameResolution', 'System.Collections', 'System.Collections.Specialized', 'System.Text.RegularExpressions', 'System.Security.Principal.Windows', 'System.ComponentModel.Primitives', 'Microsoft.Win32.Primitives'
    }
)

# Set the process as HiDPI so long as we're in a real console.
if ($Host.Name.Equals('ConsoleHost'))
{
    # Use the most recent API supported by the operating system.
    $null = switch ([System.Environment]::OSVersion.Version)
    {
        { $_ -ge '10.0.15063.0' }
        {
            [PSADT.UiAutomation]::SetProcessDpiAwarenessContext([PSADT.UiAutomation+DPI_AWARENESS_CONTEXT]::DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2)
            break
        }
        { $_ -ge '10.0.14393.0' }
        {
            [PSADT.UiAutomation]::SetProcessDpiAwarenessContext([PSADT.UiAutomation+DPI_AWARENESS_CONTEXT]::DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE)
            break
        }
        { $_ -ge '6.3.9600.0' }
        {
            [PSADT.UiAutomation]::SetProcessDpiAwareness([PSADT.UiAutomation+PROCESS_DPI_AWARENESS]::PROCESS_PER_MONITOR_DPI_AWARE)
            break
        }
        { $_ -ge '6.0.6000.0' }
        {
            [PSADT.UiAutomation]::SetProcessDPIAware()
            break
        }
    }
}

# Add system types required by the module.
& $CommandTable.'Add-Type' -AssemblyName System.ServiceProcess, System.Drawing, System.Windows.Forms, PresentationCore, PresentationFramework, WindowsBase

# All WinForms-specific initialistion code.
try
{
    [System.Windows.Forms.Application]::EnableVisualStyles()
    [System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)
}
catch
{
    $null = $null
}

# Remove any previous functions that may have been defined.
if ($MyInvocation.MyCommand.Name.Equals('PSAppDeployToolkit.psm1'))
{
    & $CommandTable.'New-Variable' -Name FunctionPaths -Option Constant -Value ($MyInvocation.MyCommand.ScriptBlock.Ast.EndBlock.Statements | & { process { if ($_ -is [System.Management.Automation.Language.FunctionDefinitionAst]) { return "Microsoft.PowerShell.Core\Function::$($_.Name)" } } })
    & $CommandTable.'Remove-Item' -LiteralPath $FunctionPaths -Force -ErrorAction Ignore
}

# Import all classes.
[System.IO.Directory]::GetFiles("$PSScriptRoot\Classes") | . { process { . $_ } }
