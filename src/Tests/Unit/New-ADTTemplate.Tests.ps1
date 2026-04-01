BeforeAll {
    Remove-Module PSAppDeployToolkit -Force -ErrorAction SilentlyContinue
    Import-Module "$PSScriptRoot\..\..\PSAppDeployToolkit\PSAppDeployToolkit.psd1" -Force
}

Describe 'New-ADTTemplate' {
    BeforeAll {
        # Mock Write-ADTLogEntry due to its expense when running via Pester.
        Mock -ModuleName PSAppDeployToolkit Write-ADTLogEntry { }

        # Helper: generate a template and return a hashtable with content strings, BOM flags, and parsed config.
        function Get-ADTTemplateContent
        {
            [CmdletBinding()]
            [OutputType([System.Collections.Hashtable])]
            param ([System.Collections.Hashtable]$Params = @{})
            $Params.Destination = $TestDrive
            $Params.Force = $true
            $Params.PassThru = $true
            $template = New-ADTTemplate @Params
            $scriptPath = Join-Path -Path $template.FullName -ChildPath 'Invoke-AppDeployToolkit.ps1'
            $configPath = Join-Path -Path $template.FullName -ChildPath 'Config\config.psd1'
            $result = @{
                Path = $template.FullName
            }
            if (Test-Path -LiteralPath $scriptPath)
            {
                $result.ScriptContent = Get-Content -LiteralPath $scriptPath -Raw
                $bom = [System.Byte[]]::new(3)
                $stream = [System.IO.File]::OpenRead($scriptPath)
                try { $null = $stream.Read($bom, 0, 3) } finally { $stream.Dispose() }
                $result.ScriptHasBom = $bom[0] -eq 0xEF -and $bom[1] -eq 0xBB -and $bom[2] -eq 0xBF
            }
            if (Test-Path -LiteralPath $configPath)
            {
                $result.ConfigData = Import-PowerShellDataFile -LiteralPath $configPath
                $bom = [System.Byte[]]::new(3)
                $stream = [System.IO.File]::OpenRead($configPath)
                try { $null = $stream.Read($bom, 0, 3) } finally { $stream.Dispose() }
                $result.ConfigHasBom = $bom[0] -eq 0xEF -and $bom[1] -eq 0xBB -and $bom[2] -eq 0xBF
            }
            $result
        }

        # Helper: parse $adtSession hashtable AST keys from script content and return as a dictionary.
        function Get-ADTSessionPropertiesFromScriptContent
        {
            [CmdletBinding()]
            [OutputType([System.Collections.Hashtable])]
            param ([System.String]$Content)
            $ast = [System.Management.Automation.Language.Parser]::ParseInput($Content, [ref]$null, [ref]$null)
            $assignmentAst = $ast.Find({
                    param ($node)
                    $node -is [System.Management.Automation.Language.AssignmentStatementAst] -and
                    ($node.Left | Get-Member -Name VariablePath) -and
                    $node.Left.VariablePath.UserPath -eq 'adtSession'
                }, $true)
            if (!$assignmentAst)
            {
                throw 'Could not find $adtSession assignment in script content.'
            }
            $hashtableAst = $assignmentAst.Right.Expression
            $keys = @{}
            foreach ($kvp in $hashtableAst.KeyValuePairs)
            {
                $keys[$kvp.Item1.Value] = $kvp.Item2.Extent.Text
            }
            $keys
        }
    }

    Context 'SessionProperties' {
        BeforeAll {
            # Single call with all property types; individual tests assert each aspect.
            $template = Get-ADTTemplateContent -Params @{
                SessionProperties = [ordered]@{
                    AppVendor = 'Contoso'
                    AppName = 'TestApp'
                    AppVersion = '6.7'
                    RequireAdmin = $false
                    AppSuccessExitCodes = @(0, 3010)
                    AppProcessesToClose = @('notepad', [ordered]@{ Name = 'calc'; Description = 'Calculator' })
                    LogName = 'CustomLog'
                }
            }
            $content = $template.ScriptContent
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'keys', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
            $keys = Get-ADTSessionPropertiesFromScriptContent -Content $content
        }

        It 'Updates existing string keys' {
            $keys['AppVendor'] | Should -Be "'Contoso'"
            $keys['AppName'] | Should -Be "'TestApp'"
            $keys['AppVersion'] | Should -Be "'6.7'"
        }

        It 'Updates boolean keys' {
            $keys['RequireAdmin'] | Should -Be '$false'
        }

        It 'Updates array keys' {
            $keys['AppSuccessExitCodes'] | Should -Be '@(0, 3010)'
        }

        It 'Updates AppProcessesToClose with nested hashtable array' {
            # ordered not typically used, but it's supported and used here so that the order is deterministic for testing.
            $content | Should -Match ([regex]::Escape("AppProcessesToClose = @('notepad', [ordered]@{ 'Name' = 'calc'; 'Description' = 'Calculator' })"))
        }

        It 'Adds keys not present in the template' {
            $keys.ContainsKey('LogName') | Should -BeTrue
            $keys['LogName'] | Should -Be "'CustomLog'"
        }

        It 'Preserves keys not specified in SessionProperties' {
            $keys['AppLang'] | Should -Be "'EN'"
        }
    }

    Context 'Default template without customization' {
        BeforeAll {
            $template = Get-ADTTemplateContent
            $template.SessionProperties = Get-ADTSessionPropertiesFromScriptContent -Content $template.ScriptContent
        }

        It 'Works without SessionProperties specified' {
            $template.ScriptContent | Should -Not -BeNullOrEmpty
        }

        It 'Replaces the AppScriptDate placeholder with a valid date' {
            $template.SessionProperties['AppScriptDate'] | Should -Not -Be "'2000-12-31'"
            $template.SessionProperties['AppScriptDate'] | Should -Match "^'\d{4}-\d{2}-\d{2}'$"
        }

        It 'Invoke-AppDeployToolkit.ps1 has UTF-8 BOM' {
            $template.ScriptHasBom | Should -BeTrue
        }

        It 'Config\config.psd1 has UTF-8 BOM' {
            $template.ConfigHasBom | Should -BeTrue
        }
    }

    Context 'Config' {
        BeforeAll {
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'template', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
            $template = Get-ADTTemplateContent -Params @{
                Config = @{
                    # Level 1: replacement + insertion inside existing 'MSI' section.
                    MSI = @{ InstallParams = 'InstallParamsText'; MutexWaitTime = 99999; NewMSIParams = 'NewMSIParamsText' }
                    # Level 1: replacement + insertion inside existing 'Toolkit' section.
                    Toolkit = @{ LogPath = '$env:TEMP\logs'; NewToolkitParams = 'NewToolkitParamsText' }
                    # Level 1: replacement + insertion inside existing 'UI' section.
                    UI = @{ NewUIParams = 'NewUIParamsText'; DefaultTimeout = 7777; MoreNewUIParams = 'MoreNewUIParamsText' }
                    # Level 0: full replacement of existing 'Assets' section (all new values).
                    Assets = @{ Logo = 'CustomLogo.png'; LogoDark = 'CustomLogoDark.png'; NewAssetsParams = 'NewAssetsParamsText' }
                    # Level 0: entirely new top-level section.
                    CustomAppSettings = @{ Timeout = 3600; CustomAppProperties = @{ Retries = 5 } }
                    MoreCustomAppSettings = @{ IsValid = $true; CustomAppProperties = @{ MoreStuff = 'No' } }
                }
            }
        }

        It 'Overrides a scalar config value' {
            $template.ConfigData.MSI.InstallParams | Should -Be 'InstallParamsText'
        }

        It 'Single-quotes strings containing dollar signs' {
            $template.ConfigData.Toolkit.LogPath | Should -Be '$env:TEMP\logs'
        }

        It 'Overrides a nested config value while preserving siblings' {
            $template.ConfigData.MSI.MutexWaitTime | Should -Be 99999
            $template.ConfigData.MSI.LoggingOptions | Should -Not -BeNullOrEmpty
        }

        It 'Replaces values at every nesting level' {
            # Level 0 replacement: existing top-level section overridden.
            $template.ConfigData.Assets.Logo | Should -Be 'CustomLogo.png'
            $template.ConfigData.Assets.LogoDark | Should -Be 'CustomLogoDark.png'
            # Level 1 replacements: existing keys inside existing sections.
            $template.ConfigData.MSI.InstallParams | Should -Be 'InstallParamsText'
            $template.ConfigData.MSI.MutexWaitTime | Should -Be 99999
            $template.ConfigData.Toolkit.LogPath | Should -Be '$env:TEMP\logs'
            $template.ConfigData.UI.DefaultTimeout | Should -Be 7777
        }

        It 'Allows adding new top-level and nested config sections' {
            # Level 1 insertions: new keys inside existing sections.
            $template.ConfigData.MSI.NewMSIParams | Should -Be 'NewMSIParamsText'
            $template.ConfigData.Toolkit.NewToolkitParams | Should -Be 'NewToolkitParamsText'
            $template.ConfigData.UI.NewUIParams | Should -Be 'NewUIParamsText'
            $template.ConfigData.UI.MoreNewUIParams | Should -Be 'MoreNewUIParamsText'
            $template.ConfigData.Assets.NewAssetsParams | Should -Be 'NewAssetsParamsText'
            # Level 0 insertion: entirely new top-level section.
            $template.ConfigData.CustomAppSettings.Timeout | Should -Be 3600
            $template.ConfigData.CustomAppSettings.CustomAppProperties.Retries | Should -Be 5
            $template.ConfigData.MoreCustomAppSettings.IsValid | Should -Be $true
            $template.ConfigData.MoreCustomAppSettings.CustomAppProperties.MoreStuff | Should -Be 'No'
        }

        It 'Config file has UTF-8 BOM' {
            $template.ConfigHasBom | Should -BeTrue
        }
    }

    Context 'Config error handling' {
        It 'Throws ConfigKeyTypeMismatch when providing hashtable for scalar key' {
            { New-ADTTemplate -Destination $TestDrive -Force -Config @{ MSI = @{ InstallParams = @{ Nested = 'bad' } } } } | Should -Throw -ErrorId 'ConfigKeyTypeMismatch*'
        }
    }

    Context 'ScriptBlocks' {
        Context 'All phases replaced' {
            BeforeAll {
                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'content', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
                $content = (Get-ADTTemplateContent -Params @{
                        PreInstallScriptBlock = { Write-ADTLogEntry -Message 'TEST-pre-install' }
                        InstallScriptBlock = { Write-ADTLogEntry -Message 'TEST-install' }
                        PostInstallScriptBlock = { Write-ADTLogEntry -Message 'TEST-post-install' }
                        PreUninstallScriptBlock = { Write-ADTLogEntry -Message 'TEST-pre-uninstall' }
                        UninstallScriptBlock = { Write-ADTLogEntry -Message 'TEST-uninstall' }
                        PostUninstallScriptBlock = { Write-ADTLogEntry -Message 'TEST-post-uninstall' }
                        PreRepairScriptBlock = { Write-ADTLogEntry -Message 'TEST-pre-repair' }
                        RepairScriptBlock = { Write-ADTLogEntry -Message 'TEST-repair' }
                        PostRepairScriptBlock = { Write-ADTLogEntry -Message 'TEST-post-repair' }
                    }).ScriptContent
            }

            It 'Replaces the Pre-Install phase content' {
                $content | Should -Match '\$PreInstall = \{\r?\n    Write-ADTLogEntry -Message ''TEST-pre-install''\r?\n\}'
                $content | Should -Not -Match 'Show-ADTInstallationWelcome @saiwParams'
            }

            It 'Replaces the Install phase content' {
                $content | Should -Match '\$Install = \{\r?\n    Write-ADTLogEntry -Message ''TEST-install''\r?\n\}'
                $content | Should -Not -Match 'UseDefaultMsi'
            }

            It 'Replaces the Post-Install phase content' {
                $content | Should -Match '\$PostInstall = \{\r?\n    Write-ADTLogEntry -Message ''TEST-post-install''\r?\n\}'
            }

            It 'Replaces the Pre-Uninstall phase content' {
                $content | Should -Match '\$PreUninstall = \{\r?\n    Write-ADTLogEntry -Message ''TEST-pre-uninstall''\r?\n\}'
                $content | Should -Not -Match 'CloseProcessesCountdown 60'
            }

            It 'Replaces the Uninstall phase content' {
                $content | Should -Match '\$Uninstall = \{\r?\n    Write-ADTLogEntry -Message ''TEST-uninstall''\r?\n\}'
            }

            It 'Replaces the Post-Uninstall phase content' {
                $content | Should -Match '\$PostUninstall = \{\r?\n    Write-ADTLogEntry -Message ''TEST-post-uninstall''\r?\n\}'
            }

            It 'Replaces the Pre-Repair phase content' {
                $content | Should -Match '\$PreRepair = \{\r?\n    Write-ADTLogEntry -Message ''TEST-pre-repair''\r?\n\}'
            }

            It 'Replaces the Repair phase content' {
                $content | Should -Match '\$Repair = \{\r?\n    Write-ADTLogEntry -Message ''TEST-repair''\r?\n\}'
            }

            It 'Replaces the Post-Repair phase content' {
                $content | Should -Match '\$PostRepair = \{\r?\n    Write-ADTLogEntry -Message ''TEST-post-repair''\r?\n\}'
            }
        }

        Context 'Single phase preserves others' {
            BeforeAll {
                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'originalContent', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
                $originalContent = (Get-ADTTemplateContent).ScriptContent
                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'content', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
                $content = (Get-ADTTemplateContent -Params @{
                        InstallScriptBlock = { Write-ADTLogEntry -Message 'custom install' }
                    }).ScriptContent
            }

            It 'Preserves unspecified scriptblocks when only one scriptblock is modified' {
                foreach ($phase in 'PreInstall', 'PostInstall', 'PreUninstall', 'Uninstall', 'PostUninstall', 'PreRepair', 'Repair', 'PostRepair')
                {
                    $pattern = '(?s)\$' + $phase + ' = \{.*?\r?\n\}'
                    $originalMatch = [regex]::Match($originalContent, $pattern).Value
                    $customMatch = [regex]::Match($content, $pattern).Value
                    $customMatch | Should -Be $originalMatch -Because "$phase should be unchanged"
                }
            }
        }

        Context 'ZeroConfig' {
            Context 'ZeroConfig alone injects default MSI logic into Install, Uninstall, and Repair' {
                BeforeAll {
                    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'content', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
                    $content = (Get-ADTTemplateContent -Params @{ ZeroConfig = $true }).ScriptContent
                }

                It 'Injects zero-config MSI logic into the Install phase' {
                    $content | Should -Match '\$Install = \{\r?\n    ## Handle Zero-Config MSI installations\.\r?\n    if \(\$adtSession\.UseDefaultMsi\)'
                }

                It 'Injects zero-config MSI logic into the Uninstall phase' {
                    $content | Should -Match '\$Uninstall = \{\r?\n    ## Handle Zero-Config MSI installations\.\r?\n    if \(\$adtSession\.UseDefaultMsi\)'
                }

                It 'Injects zero-config MSI logic into the Repair phase' {
                    $content | Should -Match '\$Repair = \{\r?\n    ## Handle Zero-Config MSI installations\.\r?\n    if \(\$adtSession\.UseDefaultMsi\)'
                }
            }

            Context 'ZeroConfig prepended to user-supplied scriptblocks' {
                BeforeAll {
                    [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'content', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
                    $content = (Get-ADTTemplateContent -Params @{
                            ZeroConfig = $true
                            InstallScriptBlock = { Write-ADTLogEntry -Message 'USER-install' }
                            UninstallScriptBlock = { Write-ADTLogEntry -Message 'USER-uninstall' }
                            RepairScriptBlock = { Write-ADTLogEntry -Message 'USER-repair' }
                        }).ScriptContent
                }

                It 'Zero-config content precedes user Install scriptblock content' {
                    $content | Should -Match '(?s)\$Install = \{.*## Handle Zero-Config MSI installations\..*Write-ADTLogEntry -Message ''USER-install'''
                }

                It 'Zero-config content precedes user Uninstall scriptblock content' {
                    $content | Should -Match '(?s)\$Uninstall = \{.*## Handle Zero-Config MSI installations\..*Write-ADTLogEntry -Message ''USER-uninstall'''
                }

                It 'Zero-config content precedes user Repair scriptblock content' {
                    $content | Should -Match '(?s)\$Repair = \{.*## Handle Zero-Config MSI installations\..*Write-ADTLogEntry -Message ''USER-repair'''
                }
            }
        }
    }

    Context 'Assets, Files, and SupportFiles parameters' {
        BeforeAll {
            # Create test source content in $TestDrive.
            $null = New-Item -Path "$TestDrive\SourceAssets" -ItemType Directory -Force
            $null = New-Item -Path "$TestDrive\SourceFiles" -ItemType Directory -Force
            $null = New-Item -Path "$TestDrive\SourceSupport" -ItemType Directory -Force
            'icondata' | Set-Content -Path "$TestDrive\SourceAssets\custom.ico" -Force
            'installer' | Set-Content -Path "$TestDrive\SourceFiles\setup.msi" -Force
            'transform' | Set-Content -Path "$TestDrive\SourceFiles\app.mst" -Force
            'config' | Set-Content -Path "$TestDrive\SourceSupport\settings.xml" -Force
            $null = New-Item -Path "$TestDrive\SourceSupport\SubDir" -ItemType Directory -Force
            'nested' | Set-Content -Path "$TestDrive\SourceSupport\SubDir\nested.txt" -Force
        }

        Context 'Combined file copy' {
            BeforeAll {
                [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'template', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
                $template = Get-ADTTemplateContent -Params @{
                    Assets = "$TestDrive\SourceAssets\custom.ico"
                    Files = "$TestDrive\SourceFiles\setup.msi", "$TestDrive\SourceFiles\app.mst"
                    SupportFiles = "$TestDrive\SourceSupport\settings.xml", "$TestDrive\SourceSupport\SubDir"
                }
            }

            It 'Copies files into the Assets folder' {
                Join-Path $template.Path 'Assets\custom.ico' | Should -Exist
                Get-Content -LiteralPath (Join-Path $template.Path 'Assets\custom.ico') | Should -Be 'icondata'
            }

            It 'Copies files into the Files folder' {
                Join-Path $template.Path 'Files\setup.msi' | Should -Exist
                Join-Path $template.Path 'Files\app.mst' | Should -Exist
            }

            It 'Copies files into the SupportFiles folder' {
                Join-Path $template.Path 'SupportFiles\settings.xml' | Should -Exist
                Get-Content -LiteralPath (Join-Path $template.Path 'SupportFiles\settings.xml') | Should -Be 'config'
            }

            It 'Recursively copies a folder into SupportFiles' {
                Join-Path $template.Path 'SupportFiles\SubDir\nested.txt' | Should -Exist
                Get-Content -LiteralPath (Join-Path $template.Path 'SupportFiles\SubDir\nested.txt') | Should -Be 'nested'
            }
        }

        It 'Supports wildcard paths' {
            $template = Get-ADTTemplateContent -Params @{ Files = "$TestDrive\SourceFiles\*.msi" }
            Join-Path $template.Path 'Files\setup.msi' | Should -Exist
        }
    }

    Context 'Version 3 template creation' {
        BeforeAll {
            $null = New-Item -Path "$TestDrive\SourceFiles" -ItemType Directory -Force
            'installer' | Set-Content -Path "$TestDrive\SourceFiles\setup.msi" -Force
            [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseDeclaredVarsMoreThanAssignments', 'template', Justification = "This variable is used within scriptblocks that PSScriptAnalyzer has no visibility of.")]
            $template = Get-ADTTemplateContent -Params @{
                Version = 3
                Config = @{ MSI = @{ InstallParams = 'TEST' } }
                Files = "$TestDrive\SourceFiles\setup.msi"
            }
        }

        It 'Creates a v3 template with expected structure' {
            Join-Path $template.Path 'AppDeployToolkit\PSAppDeployToolkit' | Should -Exist
            Join-Path $template.Path 'Deploy-Application.exe' | Should -Exist
            Join-Path $template.Path 'Files' | Should -Exist
            Join-Path $template.Path 'SupportFiles' | Should -Exist
            Join-Path $template.Path 'Config' | Should -Exist
        }

        It 'Copies files with -Version 3' {
            Join-Path $template.Path 'Files\setup.msi' | Should -Exist
        }

        It 'Accepts -Config with -Version 3' {
            # Template created successfully in BeforeAll with -Config — reaching here proves no throw.
            $template.Path | Should -Not -BeNullOrEmpty
        }
    }

    Context 'Version 3 parameter validation' {
        It 'Throws when SessionProperties is used with -Version 3' {
            { New-ADTTemplate -Destination $TestDrive -Version 3 -Force -SessionProperties @{ AppName = 'Test' } } | Should -Throw -ErrorId 'InvalidParameter*'
        }

        It 'Throws when a deployment script param is used with -Version 3' {
            { New-ADTTemplate -Destination $TestDrive -Version 3 -Force -InstallScriptBlock { test } } | Should -Throw -ErrorId 'InvalidParameter*'
        }
    }
}
