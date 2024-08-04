#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3

# Add system types required by the module.
Add-Type -AssemblyName System.Windows.Forms, System.ServiceProcess

# Add the custom types required for the toolkit.
Add-Type -LiteralPath "$PSScriptRoot\$($MyInvocation.MyCommand.ScriptBlock.Module.Name).cs" -ReferencedAssemblies $(
    'System.DirectoryServices'
    if ($PSVersionTable.PSEdition.Equals('Core'))
    {
        'System.Net.NameResolution', 'System.Collections', 'System.Text.RegularExpressions', 'System.Security.Principal.Windows', 'System.ComponentModel.Primitives', 'Microsoft.Win32.Primitives'
    }
)

# Dot-source our imports and perform exports.
(Get-ChildItem -Path $PSScriptRoot\*\*.ps1).FullName.ForEach({. $_})
Export-ModuleMember -Function (Get-ChildItem -LiteralPath $PSScriptRoot\Public).BaseName

# Define object for holding all PSADT variables.
New-Variable -Name ADT -Option ReadOnly -Value ([pscustomobject]@{
    OpeningCallbacks = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]$(
        $MyInvocation.MyCommand.ScriptBlock.Module.ExportedCommands.'Enable-ADTTerminalServerInstallMode'
    )
    ClosingCallbacks = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]$(
        $MyInvocation.MyCommand.ScriptBlock.Module.ExportedCommands.'Unblock-ADTAppExecution'
        $MyInvocation.MyCommand.ScriptBlock.Module.ExportedCommands.'Disable-ADTTerminalServerInstallMode'
    )
    Sessions = [System.Collections.Generic.List[ADTSession]]::new()
    TerminalServerMode = $false
    Environment = $null
    Language = $null
    Config = $null
    Strings = $null
    LastExitCode = 0
    Initialised = $false
})

# Logging constants used within an [ADTSession] object.
New-Variable -Name Logging -Option Constant -Value ([ordered]@{
    Formats = ([ordered]@{
        CMTrace = "<![LOG[[{1}] :: {0}]LOG]!><time=`"{2}`" date=`"{3}`" component=`"{4}`" context=`"$([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)`" type=`"{5}`" thread=`"$PID`" file=`"{6}`">"
        Legacy = '[{1} {2}] [{3}] [{4}] [{5}] :: {0}'
    }).AsReadOnly()
    SeverityNames = [System.Array]::AsReadOnly([System.String[]]@(
        'Success'
        'Info'
        'Warning'
        'Error'
    ))
    SeverityColours = [System.Array]::AsReadOnly([System.Collections.Specialized.OrderedDictionary[]]@(
        ([ordered]@{ForegroundColor = [System.ConsoleColor]::Green; BackgroundColor = [System.ConsoleColor]::Black}).AsReadOnly()
        ([ordered]@{}).AsReadOnly()
        ([ordered]@{ForegroundColor = [System.ConsoleColor]::Yellow; BackgroundColor = [System.ConsoleColor]::Black}).AsReadOnly()
        ([ordered]@{ForegroundColor = [System.ConsoleColor]::Red; BackgroundColor = [System.ConsoleColor]::Black}).AsReadOnly()
    ))
}).AsReadOnly()

# DialogBox constants used within Show-ADTDialogBox.
New-Variable -Name DialogBox -Option Constant -Value ([ordered]@{
    Buttons = ([ordered]@{
        OK = 0
        OKCancel = 1
        AbortRetryIgnore = 2
        YesNoCancel = 3
        YesNo = 4
        RetryCancel = 5
        CancelTryAgainContinue = 6
    }).AsReadOnly()
    Icons = ([ordered]@{
        None = 0
        Stop = 16
        Question = 32
        Exclamation = 48
        Information = 64
    }).AsReadOnly()
    DefaultButtons = ([ordered]@{
        First = 0
        Second = 256
        Third = 512
    }).AsReadOnly()
}).AsReadOnly()

# Registry path transformation constants used within Convert-ADTRegistryPath.
New-Variable -Name ADTRegistry -Option Constant -Value ([ordered]@{
    PathMatches = [System.Array]::AsReadOnly([System.String[]]@(
        ':\\'
        ':'
        '\\'
    ))
    PathReplacements = ([ordered]@{
        '^HKLM' = 'HKEY_LOCAL_MACHINE\'
        '^HKCR' = 'HKEY_CLASSES_ROOT\'
        '^HKCU' = 'HKEY_CURRENT_USER\'
        '^HKU' = 'HKEY_USERS\'
        '^HKCC' = 'HKEY_CURRENT_CONFIG\'
        '^HKPD' = 'HKEY_PERFORMANCE_DATA\'
    }).AsReadOnly()
    WOW64Replacements = ([ordered]@{
        '^(HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\|HKEY_CURRENT_USER\\SOFTWARE\\Classes\\|HKEY_CLASSES_ROOT\\)(AppID\\|CLSID\\|DirectShow\\|Interface\\|Media Type\\|MediaFoundation\\|PROTOCOLS\\|TypeLib\\)' = '$1Wow6432Node\$2'
        '^HKEY_LOCAL_MACHINE\\SOFTWARE\\' = 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\'
        '^HKEY_LOCAL_MACHINE\\SOFTWARE$' = 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node'
        '^HKEY_CURRENT_USER\\Software\\Microsoft\\Active Setup\\Installed Components\\' = 'HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\'
    }).AsReadOnly()
}).AsReadOnly()
