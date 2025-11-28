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
    New-Variable -Name CommandTable -Value ([System.Collections.Frozen.FrozenDictionary]::ToFrozenDictionary($CommandTable, $null)) -Option Constant -Force -Confirm:$false
    Export-ModuleMember -Function $Module.Manifest.FunctionsToExport

    # Define object for holding all PSADT variables.
    New-Variable -Name ADT -Option Constant -Value ([pscustomobject]@{
            Callbacks = [System.Collections.Frozen.FrozenDictionary]::Create([System.ReadOnlySpan[System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]][System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]][]]$(
                    [System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]::new([PSADT.Module.CallbackType]::OnInit, [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new())
                    [System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]::new([PSADT.Module.CallbackType]::OnStart, [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new())
                    [System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]::new([PSADT.Module.CallbackType]::PreOpen, [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new())
                    [System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]::new([PSADT.Module.CallbackType]::PostOpen, [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new())
                    [System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]::new([PSADT.Module.CallbackType]::OnDefer, [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new())
                    [System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]::new([PSADT.Module.CallbackType]::PreClose, [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new())
                    [System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]::new([PSADT.Module.CallbackType]::PostClose, [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new())
                    [System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]::new([PSADT.Module.CallbackType]::OnFinish, [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new())
                    [System.Collections.Generic.KeyValuePair[PSADT.Module.CallbackType, System.Collections.Generic.List[System.Management.Automation.CommandInfo]]]::new([PSADT.Module.CallbackType]::OnExit, [System.Collections.Generic.List[System.Management.Automation.CommandInfo]]::new())
                ))
            Directories = [pscustomobject]@{
                Defaults = [System.Collections.Frozen.FrozenDictionary]::Create([System.ReadOnlySpan[System.Collections.Generic.KeyValuePair[System.String, System.Object]]][System.Collections.Generic.KeyValuePair[System.String, System.Object][]]$(
                        [System.Collections.Generic.KeyValuePair[System.String, System.Object]]::new('Script', $PSScriptRoot)
                        [System.Collections.Generic.KeyValuePair[System.String, System.Object]]::new('Config', (Join-Path -Path $PSScriptRoot -ChildPath Config))
                        [System.Collections.Generic.KeyValuePair[System.String, System.Object]]::new('Strings', (Join-Path -Path $PSScriptRoot -ChildPath Strings))
                    ))
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
            Sessions = [System.Collections.Generic.List[PSADT.Module.DeploymentSession]]::new()
            Environment = $null
            Language = $null
            Config = $null
            Strings = $null
            LastExitCode = 0
            Initialized = $false
        })

    # Registry path transformation constants used within Convert-ADTRegistryPath.
    New-Variable -Name Registry -Option Constant -Value ([System.Collections.Frozen.FrozenDictionary]::Create([System.ReadOnlySpan[System.Collections.Generic.KeyValuePair[System.String, System.Object]]][System.Collections.Generic.KeyValuePair[System.String, System.Object][]]$(
                [System.Collections.Generic.KeyValuePair[System.String, System.Object]]::new('PathMatches', [System.Collections.ObjectModel.ReadOnlyCollection[System.String]]::new([System.Collections.Immutable.ImmutableArray]::Create([System.String[]]$(
                                ':\\'
                                ':'
                                '\\'
                            ))))
                [System.Collections.Generic.KeyValuePair[System.String, System.Object]]::new('PathReplacements', [System.Collections.Frozen.FrozenDictionary]::Create([System.ReadOnlySpan[System.Collections.Generic.KeyValuePair[System.String, System.String]]][System.Collections.Generic.KeyValuePair[System.String, System.String][]]$(
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^HKLM', 'HKEY_LOCAL_MACHINE\')
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^HKCR', 'HKEY_CLASSES_ROOT\')
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^HKCU', 'HKEY_CURRENT_USER\')
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^HKU', 'HKEY_USERS\')
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^HKCC', 'HKEY_CURRENT_CONFIG\')
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^HKPD', 'HKEY_PERFORMANCE_DATA\')
                        )))
                [System.Collections.Generic.KeyValuePair[System.String, System.Object]]::new('WOW64Replacements', [System.Collections.Frozen.FrozenDictionary]::Create([System.ReadOnlySpan[System.Collections.Generic.KeyValuePair[System.String, System.String]]][System.Collections.Generic.KeyValuePair[System.String, System.String][]]$(
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^(HKEY_LOCAL_MACHINE\\SOFTWARE\\Classes\\|HKEY_CURRENT_USER\\SOFTWARE\\Classes\\|HKEY_CLASSES_ROOT\\)(AppID\\|CLSID\\|DirectShow\\|Interface\\|Media Type\\|MediaFoundation\\|PROTOCOLS\\|TypeLib\\)', '$1Wow6432Node\$2')
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^HKEY_LOCAL_MACHINE\\SOFTWARE\\', 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\')
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^HKEY_LOCAL_MACHINE\\SOFTWARE$', 'HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node')
                            [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('^HKEY_CURRENT_USER\\Software\\Microsoft\\Active Setup\\Installed Components\\', 'HKEY_CURRENT_USER\Software\Wow6432Node\Microsoft\Active Setup\Installed Components\')
                        )))
            )))

    # Array of all PowerShell common parameter names.
    New-Variable -Name PowerShellCommonParameters -Option Constant -Value ([System.Collections.ObjectModel.ReadOnlyCollection[System.String]]::new([System.Collections.Immutable.ImmutableArray]::Create([System.String[]]$([System.Management.Automation.PSCmdlet]::CommonParameters; [System.Management.Automation.PSCmdlet]::OptionalCommonParameters))))

    # Lookup table for preference variables and their associated CommonParameter name.
    New-Variable -Name PreferenceVariableTable -Option Constant -Value ([System.Collections.Frozen.FrozenDictionary]::Create([System.ReadOnlySpan[System.Collections.Generic.KeyValuePair[System.String, System.String]]][System.Collections.Generic.KeyValuePair[System.String, System.String][]]$(
                [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('InformationAction', 'InformationPreference')
                [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('ProgressAction', 'ProgressPreference')
                [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('WarningAction', 'WarningPreference')
                [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('Confirm', 'ConfirmPreference')
                [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('Verbose', 'VerbosePreference')
                [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('WhatIf', 'WhatIfPreference')
                [System.Collections.Generic.KeyValuePair[System.String, System.String]]::new('Debug', 'DebugPreference')
            )))

    # Send the module's database into the C# code for internal access.
    [PSADT.Module.ModuleDatabase]::Init($ADT)
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
