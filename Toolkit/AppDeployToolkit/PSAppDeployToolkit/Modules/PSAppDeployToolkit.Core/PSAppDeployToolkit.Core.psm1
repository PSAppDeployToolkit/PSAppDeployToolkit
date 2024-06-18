#---------------------------------------------------------------------------
#
# Module setup to ensure expected functionality.
#
#---------------------------------------------------------------------------

# Set required variables to ensure module functionality.
$ErrorActionPreference = [System.Management.Automation.ActionPreference]::Stop
$ProgressPreference = [System.Management.Automation.ActionPreference]::SilentlyContinue
Set-StrictMode -Version 3

# Add the custom types required for the toolkit.
Add-Type -LiteralPath "$PSScriptRoot\$($MyInvocation.MyCommand.ScriptBlock.Module.Name).cs" -ErrorAction Stop -ReferencedAssemblies $(
    'System.Drawing', 'System.Windows.Forms', 'System.DirectoryServices'
    if ($PSVersionTable.PSEdition.Equals('Core'))
    {
        'System.Collections', 'System.Text.RegularExpressions', 'System.Security.Principal.Windows', 'System.ComponentModel.Primitives', 'Microsoft.Win32.Primitives'
    }
)

# Add system types required by the module.
Add-Type -AssemblyName System.Windows.Forms, System.Activities

# Dot-source our imports and perform exports.
(Get-ChildItem -Path $PSScriptRoot\*\*.ps1).FullName.ForEach({. $_})
Export-ModuleMember -Function (Get-ChildItem -LiteralPath $PSScriptRoot\Public).BaseName

# Define object for holding all PSADT variables.
New-Variable -Name ADT -Option ReadOnly -Value @{
    Sessions = [System.Collections.Generic.List[ADTSession]]::new()
    Environment = $null
    Language = $null
    Config = $null
    Strings = $null
    LastExitCode = 0
}

# Values used for ADT module serialisation.
New-Variable -Name Serialisation -Option Constant -Value ([ordered]@{
    Hive = [Microsoft.Win32.Registry]::CurrentUser
    Key = "SOFTWARE\PSAppDeployToolkit"
    Name = 'ModuleState'
    Type = [Microsoft.Win32.RegistryValueKind]::String
}).AsReadOnly()

# Logging constants used within an [ADTSession] object.
New-Variable -Name Logging -Option Constant -Value ([ordered]@{
    Formats = ([ordered]@{
        CMTrace = "<![LOG[[{1}] :: {0}]LOG]!><time=`"{2}`" date=`"{3}`" component=`"{4}`" context=`"$([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)`" type=`"{5}`" thread=`"$PID`" file=`"{6}`">"
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
