Function Set-PinnedApplication {
    <#
.SYNOPSIS

Pins or unpins a shortcut to the start menu or task bar.

.DESCRIPTION

Pins or unpins a shortcut to the start menu or task bar.

This should typically be run in the user context, as pinned items are stored in the user profile.

.PARAMETER Action

Action to be performed. Options: 'PinToStartMenu','UnpinFromStartMenu','PinToTaskbar','UnpinFromTaskbar'.

.PARAMETER FilePath

Path to the shortcut file to be pinned or unpinned.

.INPUTS

None

You cannot pipe objects to this function.

.OUTPUTS

None

This function does not generate any output.

.EXAMPLE

Set-PinnedApplication -Action 'PinToStartMenu' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"

.EXAMPLE

Set-PinnedApplication -Action 'UnpinFromTaskbar' -FilePath "$envProgramFilesX86\IBM\Lotus\Notes\notes.exe"

.NOTES

Windows 10 logic borrowed from Stuart Pearson (https://pinto10blog.wordpress.com/2016/09/10/pinto10/)

.LINK

https://psappdeploytoolkit.com
#>
    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateSet('PinToStartMenu', 'UnpinFromStartMenu', 'PinToTaskbar', 'UnpinFromTaskbar')]
        [String]$Action,
        [Parameter(Mandatory = $true)]
        [ValidateNotNullorEmpty()]
        [String]$FilePath
    )

    Begin {
        $adtEnv = Get-ADTEnvironment
        Write-ADTDebugHeader

        #region Function Get-PinVerb
        Function Get-PinVerb {
            [CmdletBinding()]
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [Int32]$VerbId
            )

            Write-ADTLogEntry -Message "Get localized pin verb for verb id [$VerbID]."
            [String]$PinVerb = [PSADT.FileVerb]::GetPinVerb($VerbId)
            Write-ADTLogEntry -Message "Verb ID [$VerbID] has a localized pin verb of [$PinVerb]."
            Write-Output -InputObject ($PinVerb)
        }
        #endregion

        #region Function Invoke-Verb
        Function Invoke-Verb {
            [CmdletBinding()]
            Param (
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [String]$FilePath,
                [Parameter(Mandatory = $true)]
                [ValidateNotNullorEmpty()]
                [String]$Verb
            )

            Try {
                $Verb = $Verb.Replace('&', '')
                $path = Split-Path -Path $FilePath -Parent -ErrorAction 'Stop'
                $folder = $adtEnv.ShellApp.Namespace($path)
                $item = $folder.ParseName((Split-Path -Path $FilePath -Leaf -ErrorAction 'Stop'))
                $itemVerb = $item.Verbs() | Where-Object { $_.Name.Replace('&', '') -eq $Verb } -ErrorAction 'Stop'

                If ($null -eq $itemVerb) {
                    Write-ADTLogEntry -Message "Performing action [$Verb] is not programmatically supported for this file [$FilePath]." -Severity 2
                }
                Else {
                    Write-ADTLogEntry -Message "Performing action [$Verb] on [$FilePath]."
                    $itemVerb.DoIt()
                }
            }
            Catch {
                Write-ADTLogEntry -Message "Failed to perform action [$Verb] on [$FilePath]. `r`n$(Resolve-Error)" -Severity 2
            }
        }
        #endregion

        If ($adtEnv.envOSVersionMajor -ge 10) {
            Write-ADTLogEntry -Message 'Detected Windows 10 or higher, using Windows 10 verb codes.'
            [Hashtable]$Verbs = @{
                'PinToStartMenu'     = 51201
                'UnpinFromStartMenu' = 51394
                'PinToTaskbar'       = 5386
                'UnpinFromTaskbar'   = 5387
            }
        }
        Else {
            [Hashtable]$Verbs = @{
                'PinToStartMenu'     = 5381
                'UnpinFromStartMenu' = 5382
                'PinToTaskbar'       = 5386
                'UnpinFromTaskbar'   = 5387
            }
        }

    }
    Process {
        Try {
            Write-ADTLogEntry -Message "Execute action [$Action] for file [$FilePath]."

            If (-not (Test-Path -LiteralPath $FilePath -PathType 'Leaf' -ErrorAction 'Stop')) {
                Throw "Path [$filePath] does not exist."
            }

            If (-not ($Verbs.$Action)) {
                Throw "Action [$Action] not supported. Supported actions are [$($Verbs.Keys -join ', ')]."
            }

            If ($Action.Contains('StartMenu')) {
                If ($adtEnv.envOSVersionMajor -ge 10)   {
                    If ((Get-Item -Path $FilePath).Extension -ne '.lnk') {
                        Throw 'Only shortcut files (.lnk) are supported on Windows 10 and higher.'
                    }
                    ElseIf (-not ($FilePath.StartsWith($($adtEnv.envUserStartMenu), 'OrdinalIgnoreCase') -or $FilePath.StartsWith($($adtEnv.envCommonStartMenu), 'OrdinalIgnoreCase'))) {
                        Throw "Only shortcut files (.lnk) in [$($adtEnv.envUserStartMenu)] and [$($adtEnv.envCommonStartMenu)] are supported on Windows 10 and higher."
                    }
                }

                [String]$PinVerbAction = Get-PinVerb -VerbId ($Verbs.$Action)
                If (-not $PinVerbAction) {
                    Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
                }

                Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
            }
            ElseIf ($Action.Contains('Taskbar')) {
                If ($adtEnv.envOSVersionMajor -ge 10) {
                    $FileNameWithoutExtension = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
                    $PinExists = Test-Path -Path "$env:APPDATA\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk"

                    If (($Action -eq 'PinToTaskbar') -and ($PinExists)) {
                        If ($(Invoke-ADTObjectMethod -InputObject $adtEnv.Shell -MethodName 'CreateShortcut' -ArgumentList "$env:APPDATA\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\$($FileNameWithoutExtension).lnk").TargetPath -eq $FilePath) {
                            Write-ADTLogEntry -Message "Pin [$FileNameWithoutExtension] already exists."
                            Return
                        }
                    }
                    ElseIf (($Action -eq 'UnpinFromTaskbar') -and ($PinExists -eq $false)) {
                        Write-ADTLogEntry -Message "Pin [$FileNameWithoutExtension] does not exist."
                        Return
                    }

                    $ExplorerCommandHandler = Get-RegistryKey -Key 'Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\Windows.taskbarpin' -Value 'ExplorerCommandHandler'
                    $classesStarKey = (Get-Item "Registry::HKEY_USERS\$($adtEnv.RunasActiveUser.SID)\SOFTWARE\Classes").OpenSubKey('*', $true)
                    $shellKey = $classesStarKey.CreateSubKey('shell', $true)
                    $specialKey = $shellKey.CreateSubKey('{:}', $true)
                    $specialKey.SetValue('ExplorerCommandHandler', $ExplorerCommandHandler)

                    $Folder = Invoke-ADTObjectMethod -InputObject $adtEnv.ShellApp -MethodName 'Namespace' -ArgumentList $(Split-Path -Path $FilePath -Parent)
                    $Item = Invoke-ADTObjectMethod -InputObject $Folder -MethodName 'ParseName' -ArgumentList $(Split-Path -Path $FilePath -Leaf)

                    $Item.InvokeVerb('{:}')

                    $shellKey.DeleteSubKey('{:}')
                    If ($shellKey.SubKeyCount -eq 0 -and $shellKey.ValueCount -eq 0) {
                        $classesStarKey.DeleteSubKey('shell')
                    }
                }
                Else {
                    [String]$PinVerbAction = Get-PinVerb -VerbId ($Verbs.$Action)
                    If (-not $PinVerbAction) {
                        Throw "Failed to get a localized pin verb for action [$Action]. Action is not supported on this operating system."
                    }

                    Invoke-Verb -FilePath $FilePath -Verb $PinVerbAction
                }
            }
        }
        Catch {
            Write-ADTLogEntry -Message "Failed to execute action [$Action]. `r`n$(Resolve-Error)" -Severity 2
        }
        Finally {
            Try {
                If ($shellKey) {
                    $shellKey.Close()
                }
            }
            Catch {
            }
            Try {
                If ($classesStarKey) {
                    $classesStarKey.Close()
                }
            }
            Catch {
            }
        }
    }
    End {
        Write-ADTDebugFooter
    }
}
