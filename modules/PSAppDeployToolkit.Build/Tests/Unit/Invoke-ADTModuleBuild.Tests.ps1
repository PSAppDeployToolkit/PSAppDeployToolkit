Describe 'Invoke-ADTModuleBuild' {
    Context 'Build environment confirmation' {
        It 'Confirms the build environment whichever steps were asked for' {
            # Asserted against the source rather than by running the build, because the unit test suite
            # is itself driven by Invoke-ADTModuleBuild: re-entering it, or mocking the module it lives
            # in, would interfere with the run in progress.
            $ast = [System.Management.Automation.Language.Parser]::ParseFile("$PSScriptRoot\..\..\Public\Invoke-ADTModuleBuild.ps1", [ref]$null, [ref]$null)
            $calls = @($ast.FindAll({ ($args[0] -is [System.Management.Automation.Language.CommandAst]) -and ($args[0].GetCommandName() -eq 'Test-ADTBuildEnvironment') }, $true))
            $calls.Count | Should -Be 1

            # Sitting inside a conditional is what let a subset of steps skip it entirely.
            $conditionals = 0
            $parent = $calls[0].Parent
            while ($null -ne $parent)
            {
                if ($parent -is [System.Management.Automation.Language.IfStatementAst])
                {
                    $conditionals++
                }
                $parent = $parent.Parent
            }
            $conditionals | Should -Be 0
        }
    }
}
