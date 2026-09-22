#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    Import-Module "$PSScriptRoot/../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1" -Force
}

Describe 'Password prompt contracts' {
    It 'defaults to secure capture and accepts explicit plaintext capture' {
        (New-FluencePrompt -Message 'Key' -InputType Password).AsPlainText | Should -BeFalse
        (New-FluencePrompt -Message 'Key' -InputType Password -AsPlainText).AsPlainText | Should -BeTrue
    }
    It 'rejects plaintext output for other input types through either public entry point' {
        { New-FluencePrompt -Message 'Name' -AsPlainText } | Should -Throw '*only to Password*'
        { Get-FluenceInput -Message 'Name' -AsPlainText } | Should -Throw '*only to Password*'
    }
    It 'rejects a secure password regex unless plaintext handling is explicit' {
        { New-FluencePrompt -Message 'Key' -InputType Password -ValidatePattern '^abc$' } |
            Should -Throw '*-ValidatePattern requires -AsPlainText*'
        (New-FluencePrompt -Message 'Key' -InputType Password -AsPlainText -ValidatePattern '^abc$').ValidatePattern |
            Should -Be '^abc$'
    }
    It 'rejects a SecureString default instead of casting its type name to a password' {
        $secure = [System.Security.SecureString]::new()
        try
        {
            { New-FluencePrompt -Message 'Key' -InputType Password -DefaultValue $secure } |
                Should -Throw '*Password -DefaultValue must be a string*'
        }
        finally { $secure.Dispose() }
    }
    It 'validates SecureString length and passes the secure object to the custom validator' {
        & (Get-Module Fluence.Wpf.PowerShell) {
            $prompt = New-FluencePrompt -Name Token -Message 'Key' -InputType Password -ValidateNotEmpty -ValidateScript {
                param($value)
                $value -is [System.Security.SecureString] -and $value.Length -eq 1
            }
            $secure = [System.Security.SecureString]::new()
            try
            {
                (Test-FluenceInput -Prompts @($prompt) -Values @{ Token = $secure }).IsValid | Should -BeFalse
                $secure.AppendChar('x')
                (Test-FluenceInput -Prompts @($prompt) -Values @{ Token = $secure }).IsValid | Should -BeTrue
            }
            finally { $secure.Dispose() }
        }
    }
    It 'does not echo password validator exceptions to verbose output' {
        & (Get-Module Fluence.Wpf.PowerShell) {
            $prompt = New-FluencePrompt -Name Token -Message 'Key' -InputType Password -AsPlainText -ValidateScript {
                param($value)
                throw "Bad secret: $value"
            }
            $output = Test-FluenceInput -Prompts @($prompt) -Values @{ Token = 'sentinel-secret' } -Verbose 4>&1
            ($output | Out-String) | Should -Not -Match 'sentinel-secret'
            $output[-1].IsValid | Should -BeFalse
        }
    }
}

Describe 'Public password results' {
    BeforeEach {
        # Replace only dispatch. Public specification, wrapper and result conversion still execute.
        Mock Invoke-OnFluenceUi -ModuleName Fluence.Wpf.PowerShell {
            param($ArgumentList)
            $raw = @{ Cancelled = $false; TimedOut = $false; OK = $true }
            foreach ($prompt in $ArgumentList[0].Prompts)
            {
                if ($prompt.AsPlainText)
                {
                    $raw[$prompt.Name] = 'sentinel-secret'
                }
                else
                {
                    $secure = [System.Security.SecureString]::new()
                    $secure.AppendChar('x')
                    $raw[$prompt.Name] = $secure
                }
            }
            return $raw
        }
    }
    It 'preserves the secure value through Get-FluenceInput' {
        $value = Get-FluenceInput -Message 'Key' -InputType Password
        try
        {
            $value | Should -BeOfType ([System.Security.SecureString])
            $value.Length | Should -Be 1
        }
        finally { $value.Dispose() }
    }
    It 'forwards explicit plaintext capture through Get-FluenceInput' {
        Get-FluenceInput -Message 'Key' -InputType Password -AsPlainText | Should -BeExactly 'sentinel-secret'
    }
    It 'keeps arbitrary plaintext prompt names out of default dialog formatting' {
        $prompt = New-FluencePrompt -Name 'Unusual API token' -Message 'Key' -InputType Password -AsPlainText
        $result = Show-FluenceDialog -Prompts $prompt
        $result.'Unusual API token' | Should -BeExactly 'sentinel-secret'
        $rendered = $result | Out-String
        $rendered | Should -Match 'Cancelled'
        $rendered | Should -Not -Match 'sentinel-secret'
        ($result | Format-List * | Out-String) | Should -Match 'sentinel-secret'
    }
    It 'returns null and disposes discarded secure input on cancellation or timeout' {
        InModuleScope Fluence.Wpf.PowerShell {
            foreach ($timedOut in @($false, $true))
            {
                $script:DiscardedPassword = [System.Security.SecureString]::new()
                $script:TimedOutPassword = $timedOut
                Mock Invoke-OnFluenceUi {
                    @{ Cancelled = -not $script:TimedOutPassword; TimedOut = $script:TimedOutPassword; Input = $script:DiscardedPassword }
                }
                Get-FluenceInput -Message 'Key' -InputType Password | Should -BeNullOrEmpty
                { $script:DiscardedPassword.Copy() } | Should -Throw
            }
        }
    }
}
