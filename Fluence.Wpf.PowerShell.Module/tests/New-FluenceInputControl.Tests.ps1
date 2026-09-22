#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

# Verifies New-FluenceInputControl seeds $State.Result with a correctly-typed live value for an
# untouched control, so a field the user never edits still returns a usable value (not $null or a
# raw/typeless default). The control build runs through Invoke-OnFluenceUi so it executes on an STA
# thread on every host shape: inline on STA (powershell, pwsh) and the module-owned STA runspace on
# an MTA host (pwsh -mta). The prompt is built from primitives inside the UI block so nothing has to
# cross a runspace boundary.

BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '..\src\Fluence.Wpf.PowerShell\Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force

    $script:SeedFor = {
        param($name, $inputType, $default, $hasDefault)
        & (Get-Module Fluence.Wpf.PowerShell) {
            param($n, $type, $def, $has)
            Invoke-OnFluenceUi -Script {
                param($n2, $type2, $def2, $has2)
                $pp = @{ Name = $n2; Message = 'm'; InputType = $type2 }
                if ($has2) { $pp.DefaultValue = $def2 }
                $prompt = New-FluencePrompt @pp
                $state = @{ Result = @{} }
                $null = New-FluenceInputControl -Prompt $prompt -State $state
                $state.Result[$n2]
            } -ArgumentList @($n, $type, $def, $has)
        } $name $inputType $default $hasDefault
    }
}

Describe 'New-FluenceInputControl result seeding' {

    It 'seeds an untouched Checkbox as a [bool] false, not $null' {
        $v = & $script:SeedFor 'Flag' 'Checkbox' $null $false
        $v | Should -BeOfType ([bool])
        $v | Should -BeFalse
    }

    It 'seeds an untouched Number as the control value 0, not $null' {
        $v = & $script:SeedFor 'Qty' 'Number' $null $false
        $v | Should -Be 0
    }

    It 'seeds a Checkbox from a string default through [bool] coercion' {
        $v = & $script:SeedFor 'Flag' 'Checkbox' 'true' $true
        $v | Should -BeTrue
    }
}


Describe 'Password control capture without a window' {
    It 'captures <Mode> passwords as <ExpectedType> on the real STA route' -ForEach @(
        @{ Mode = 'empty'; ExpectedType = 'System.Security.SecureString'; Plain = $false; Default = $null; Changed = $null; Length = 0 }
        @{ Mode = 'untouched'; ExpectedType = 'System.Security.SecureString'; Plain = $false; Default = 'seed'; Changed = $null; Length = 4 }
        @{ Mode = 'edited'; ExpectedType = 'System.Security.SecureString'; Plain = $false; Default = 'seed'; Changed = 'new-value'; Length = 9 }
        @{ Mode = 'explicit plaintext'; ExpectedType = 'System.String'; Plain = $true; Default = 'seed'; Changed = 'new-value'; Length = 9 }
    ) {
        $value = & (Get-Module Fluence.Wpf.PowerShell) {
            param($plain, $initial, $changed)
            Invoke-OnFluenceUi -Script {
                param($plain2, $initial2, $changed2)
                $prompt = New-FluencePrompt -Name Key -Message 'Key' -InputType Password -AsPlainText:$plain2 -DefaultValue $initial2
                $state = @{ Result = @{} }
                $control = New-FluenceInputControl -Prompt $prompt -State $state
                if ($null -ne $changed2) { $control.Password = $changed2 }
                return $state.Result.Key
            } -ArgumentList @($plain, $initial, $changed)
        } $Plain $Default $Changed
        try
        {
            $value.GetType().FullName | Should -Be $ExpectedType
            $value.Length | Should -Be $Length
            if ($Plain) { $value | Should -BeExactly 'new-value' }
        }
        finally
        {
            if ($value -is [System.Security.SecureString]) { $value.Dispose() }
        }
    }
}
