#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version Latest

# Add the custom types required for the toolkit.
Add-Type -LiteralPath "$PSScriptRoot\PSAppDeployToolkit.cs" -ReferencedAssemblies $(
    'System.Drawing', 'System.Windows.Forms', 'System.DirectoryServices'
    if ($PSVersionTable.PSEdition.Equals('Core'))
    {
        'System.Collections', 'System.Text.RegularExpressions', 'System.Security.Principal.Windows', 'System.ComponentModel.Primitives', 'Microsoft.Win32.Primitives'
    }
)

# Add system types required for the toolkit.
Add-Type -AssemblyName ('System.Drawing', 'System.Windows.Forms', 'PresentationFramework', 'Microsoft.VisualBasic', 'PresentationCore', 'WindowsBase', 'System.Activities')

# Set process as DPI-aware for better dialog rendering.
[System.Void][PSADT.UiAutomation]::SetProcessDPIAware()
[System.Windows.Forms.Application]::EnableVisualStyles()

# WinForms modern text rendering. Commented out for now as forms aren't coded for it.
# [System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)

# Dot-source our imports.
New-Variable -Name ADTSubmodules -Option Constant -Value @{
    Classes = Get-ChildItem -Path $PSScriptRoot\Classes\*.ps1
    Private = Get-ChildItem -Path $PSScriptRoot\Private\*.ps1
    Public = Get-ChildItem -Path $PSScriptRoot\Public\*.ps1
}
$ADTSubmodules.Values.ForEach({$_.ForEach({. $_.FullName})})
Export-ModuleMember -Function $ADTSubmodules.Public.BaseName

# Define object for holding all PSADT variables.
New-Variable -Name ADT -Option ReadOnly -Value @{
    Sessions = [System.Collections.Generic.List[ADTSession]]::new()
    Environment = $null
    Language = $null
    Config = $null
    Strings = $null
    LastExitCode = 0
}

# State data used by Show-ADTInstallationProgress.
New-Variable -Name ProgressWindow -Option Constant -Value @{
    SyncHash = [System.Collections.Hashtable]::Synchronized(@{})
    PowerShell = $null
    Invocation = $null
    Running = $false
}

# Asset data used by all forms.
New-Variable -Name FormData -Option Constant -Value @{
    Font = [System.Drawing.SystemFonts]::MessageBoxFont
    Width = 450
    BannerHeight = 0
    NotifyIcon = $null
    Assets = @{
        Icon = $null
        Logo = $null
        Banner = $null
    }
}

# Variables to track multiple sessions and each session's caller.
New-Variable -Name SessionCallers -Option Constant -Value @{}

# Values used for ADT module serialisation.
New-Variable -Name Serialisation -Option Constant -Value ([ordered]@{
    Hive = [Microsoft.Win32.Registry]::CurrentUser
    Key = "SOFTWARE\$($Script:MyInvocation.MyCommand.ScriptBlock.Module.Name)"
    Name = 'ModuleState'
    Type = [Microsoft.Win32.RegistryValueKind]::String
}).AsReadOnly()

# Logging constants used within an [ADTSession] object.
New-Variable -Name Logging -Option Constant -Value ([ordered]@{
    Formats = ([ordered]@{
        CMTrace = "<![LOG[[{1}] :: {0}]LOG]!><time=`"{2}`" date=`"{3}`" component=`"{4}`" context=`"$([Security.Principal.WindowsIdentity]::GetCurrent().Name)`" type=`"{5}`" thread=`"$PID`" file=`"{6}`">"
        Legacy = '[{1} {2}] [{3}] [{4}] [{5}] :: {0}'
    }).AsReadOnly()
    SeverityNames = [System.Array]::AsReadOnly(@(
        'Success'
        'Info'
        'Warning'
        'Error'
    ))
    SeverityColours = [System.Array]::AsReadOnly(@(
        ([ordered]@{ForegroundColor = [System.ConsoleColor]::Green; BackgroundColor = [System.ConsoleColor]::Black}).AsReadOnly()
        ([ordered]@{}).AsReadOnly()
        ([ordered]@{ForegroundColor = [System.ConsoleColor]::Yellow; BackgroundColor = [System.ConsoleColor]::Black}).AsReadOnly()
        ([ordered]@{ForegroundColor = [System.ConsoleColor]::Red; BackgroundColor = [System.ConsoleColor]::Black}).AsReadOnly()
    ))
}).AsReadOnly()
