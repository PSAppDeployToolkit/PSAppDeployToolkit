BeforeAll {
    Set-Location -Path $PSScriptRoot
    $ModuleName = 'PSAppDeployToolkit'
    Get-Module $ModuleName -ErrorAction SilentlyContinue | Remove-Module -Force
    Import-Module ([System.IO.Path]::Combine('..', '..', $ModuleName, "$ModuleName.psd1")) -Force
    $commandArray = (Get-Command).Name
    $fileCountOverrides = @{
        'Show-ADTBalloonTipClassic.ps1' = 2
        'Show-ADTBalloonTipFluent.ps1' = 5
        'Show-ADTInstallationProgressClassic.ps1' = 1
        'Unblock-ADTAppExecutionInternal.ps1' = 9
        'Show-ADTHelpConsole.ps1' = 5
    }
}

BeforeDiscovery {
    Set-Location -Path $PSScriptRoot
    $ModuleName = 'PSAppDeployToolkit'
    $moduleFiles = Get-ChildItem -LiteralPath (('Classes', 'Private', 'Public') -replace '^', "$([System.IO.Path]::Combine('..', '..', $ModuleName))\")
}

Describe $ModuleName {

    Context 'Command Map Utilization' {

        It 'File <_> has all command calls defined via the module CommandTable' -ForEach $moduleFiles {
            [System.Management.Automation.ScriptBlock]::Create([System.IO.File]::ReadAllText($_.FullName)).Ast.FindAll(
                {
                    # Is a raw statement.
                    ($args[0] -is [System.Management.Automation.Language.StringConstantExpressionAst]) -and

                    # Statement is a known PowerShell command/function/alias.
                    ($commandArray -contains $args[0].Value) -and

                    # Statement isn't part of a CommandTable call.
                    ($args[0].Parent.Expression.Extent.Text -ne '$Script:CommandTable')
                },
                $true
            ).Count | Should -BeExactly $(if (!$fileCountOverrides.ContainsKey($_.Name)) {0} else {$fileCountOverrides.($_.Name)})
        }

    }

}

