#-----------------------------------------------------------------------------
#
# MARK: Module Constants and Function Exports
#
#-----------------------------------------------------------------------------

# Rethrowing caught exceptions makes the error output from Import-Module look better.
try
{
    # Set all functions as read-only, export all public definitions and finalise the CommandTable.
    Set-Item -LiteralPath $FunctionPaths -Options ReadOnly; Get-Item -LiteralPath $FunctionPaths | & { process { $CommandTable.Add($_.Name, $_) } }
    New-Variable -Name CommandTable -Value ([System.Collections.ObjectModel.ReadOnlyDictionary[System.String, System.Management.Automation.CommandInfo]]::new($CommandTable)) -Option Constant -Force -Confirm:$false
    Export-ModuleMember -Function $Module.Manifest.FunctionsToExport

    # Define object for holding all PSADT variables.
    New-Variable -Name ADT -Option Constant -Value ([pscustomobject]@{
            Callbacks = ([ordered]@{
                    [PSAppDeployToolkit.Common.CallbackType]::OnInit = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Common.CallbackType]::OnStart = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Common.CallbackType]::PreOpen = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Common.CallbackType]::PostOpen = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Common.CallbackType]::OnDefer = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Common.CallbackType]::PreClose = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Common.CallbackType]::PostClose = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Common.CallbackType]::OnFinish = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                    [PSAppDeployToolkit.Common.CallbackType]::OnExit = [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new()
                }).AsReadOnly()
            Directories = [pscustomobject]@{
                Defaults = ([ordered]@{
                        Script = $PSScriptRoot
                        Config = Join-Path -Path $PSScriptRoot -ChildPath Config
                        Strings = Join-Path -Path $PSScriptRoot -ChildPath Strings
                    }).AsReadOnly()
                Script = $null
                Config = $null
                Strings = $null
            }
            Durations = [pscustomobject]@{
                ModuleImport = $null
                ModuleInit = $null
            }
            SessionState = $ExecutionContext.SessionState
            RestartOnExitCountdown = $null
            ClientServerProcess = $null
            Sessions = [System.Collections.Generic.List[PSAppDeployToolkit.SessionManagement.DeploymentSession]]::new()
            Environment = $null
            Language = $null
            Config = $null
            Strings = $null
            LastExitCode = 0
            Initialized = $false
        })

    # Registry path transformation constants used within Convert-ADTRegistryPath.
    New-Variable -Name Registry -Option Constant -Value ([ordered]@{
            PathMatches = [System.Collections.ObjectModel.ReadOnlyCollection[System.String]]$(
                ':\\'
                ':'
                '\\'
            )
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

    # Array of all PowerShell common parameter names.
    New-Variable -Name PowerShellCommonParameters -Option Constant -Value ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]]$([System.Management.Automation.PSCmdlet]::CommonParameters; [System.Management.Automation.PSCmdlet]::OptionalCommonParameters))

    # Lookup table for preference variables and their associated CommonParameter name.
    New-Variable -Name PreferenceVariableTable -Option Constant -Value ([ordered]@{
            'InformationAction' = 'InformationPreference'
            'ProgressAction' = 'ProgressPreference'
            'WarningAction' = 'WarningPreference'
            'Confirm' = 'ConfirmPreference'
            'Verbose' = 'VerbosePreference'
            'WhatIf' = 'WhatIfPreference'
            'Debug' = 'DebugPreference'
        }).AsReadOnly()

    # Send the module's database into the C# code for internal access.
    [PSAppDeployToolkit.Foundation.ModuleDatabase]::Init($ADT)
}
catch
{
    throw
}

# Ensure that the client/server process is closed on module remove.
$ModuleInfo.OnRemove = {
    if ($ADT.ClientServerProcess)
    {
        Close-ADTClientServerProcess
    }
}

# Determine how long the import took.
$ADT.Durations.ModuleImport = [System.DateTime]::Now - $ModuleImportStart
Remove-Variable -Name ModuleImportStart -Force -Confirm:$false
