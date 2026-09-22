#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1'
    Import-Module $script:ModulePath -Force
}

Describe 'Dialog specification integrity' {
    It 'rejects the reserved prompt name <Name> before dispatch' -ForEach @(
        @{ Name = 'Cancelled' }
        @{ Name = 'timedout' }
        @{ Name = 'PSTypeName' }
    ) {
        $prompt = New-FluencePrompt -Name $Name -Message 'Value'
        { Show-FluenceDialog -Prompts $prompt } | Should -Throw '*duplicate or reserved*'
    }
    It 'rejects prompt-button and case-insensitive prompt collisions before dispatch' {
        $prompt = New-FluencePrompt -Name ok -Message 'Value'
        { Show-FluenceDialog -Prompts $prompt } | Should -Throw '*duplicate or reserved*'
        $other = New-FluencePrompt -Name OK -Message 'Other'
        { Show-FluenceDialog -Prompts $prompt, $other -Buttons Save } | Should -Throw '*duplicate or reserved*'
    }
    It 'rejects duplicate and reserved button names before dispatch' {
        { Show-FluenceDialog -Buttons 'Save', 'save' } | Should -Throw '*duplicate or reserved*'
        { Show-FluenceDialog -Buttons 'TimedOut' } | Should -Throw '*duplicate or reserved*'
    }
    It 'rejects an out-of-set default for <Kind>' -ForEach @(
        @{ Kind = 'Choice' }
        @{ Kind = 'List' }
    ) {
        { New-FluencePrompt -Message 'Choose' -InputType $Kind -ValidateSet A, B -DefaultValue C } |
            Should -Throw '*not in -ValidateSet*'
    }
    It 'canonicalizes valid choice defaults to the displayed item' {
        (New-FluencePrompt -Message 'Choose' -InputType Choice -ValidateSet Alpha, Beta -DefaultValue alpha).DefaultValue |
            Should -BeExactly 'Alpha'
    }
    It 'validates every multi-select default' {
        { New-FluencePrompt -Message 'Choose' -InputType List -MultiSelect -ValidateSet A, B -DefaultValue A, C } |
            Should -Throw '*not in -ValidateSet*'
    }
    It 'rejects an invalid regular expression before a window is constructed' {
        { New-FluencePrompt -Message 'Value' -ValidatePattern '[' } | Should -Throw '*not a valid regular expression*'
    }
}

Describe 'Window callback and selection construction' {
    It 'keeps callback success output out of the returned window for <Mode>' -ForEach @(
        @{ Mode = 'ContentBlock' }
        @{ Mode = 'XamlString' }
    ) {
        $facts = & (Get-Module Fluence.Wpf.PowerShell) {
            param($modeName)
            Invoke-OnFluenceUi -Script {
                param($kind)
                Initialize-FluenceApplication -Theme Light -Backdrop None
                $callback = { param($window, $data) 'incidental output'; $window.Tag = $data.Value }
                $spec = @{
                    Mode = $kind
                    ChromeBound = @{}
                    CallerRunspaceId = [System.Management.Automation.Runspaces.Runspace]::DefaultRunspace.InstanceId
                    Content = $callback
                    ContentText = $callback.ToString()
                    Initialize = $callback
                    InitializeText = $callback.ToString()
                    Data = @{ Value = 'assigned' }
                    Xaml = '<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" />'
                }
                $state = @{ Error = $null }
                $window = Set-FluenceWindowContent -Spec $spec -State $state
                try
                {
                    return @{ IsWindow = ($window -is [System.Windows.Window]); Value = $window.Tag; Error = $state.Error }
                }
                finally
                {
                    $window.Close()
                }
            } -ArgumentList @($modeName)
        } $Mode
        $facts.IsWindow | Should -BeTrue
        $facts.Value | Should -Be 'assigned'
        $facts.Error | Should -BeNullOrEmpty
    }
    It 'seeds Choice from its actual selection for <As>' -ForEach @(
        @{ As = 'Combo' }
        @{ As = 'Radio' }
    ) {
        $result = & (Get-Module Fluence.Wpf.PowerShell) {
            param($choiceStyle)
            Invoke-OnFluenceUi -Script {
                param($style)
                $prompt = New-FluencePrompt -Name ChoiceValue -Message 'Choose' -InputType Choice -As $style -ValidateSet Alpha, Beta -DefaultValue alpha
                $state = @{ Result = @{} }
                $null = New-FluenceInputControl -Prompt $prompt -State $state
                return @{ Value = $state.Result.ChoiceValue }
            } -ArgumentList @($choiceStyle)
        } $As
        $result.Value | Should -BeExactly 'Alpha'
    }
}

Describe 'Invocation boundaries' {
    It 'imports an STA bootstrap manifest from a path containing an apostrophe' {
        $folder = Join-Path $TestDrive "O'Brien"
        $null = New-Item -ItemType Directory -Path $folder
        $manifest = Join-Path $folder 'ApostropheProbe.psd1'
        Set-Content -LiteralPath $manifest -Value "@{ ModuleVersion = '1.0'; GUID = 'ad12f1da-df10-40b9-9a6b-9f97dde376b3' }"
        $loaded = & (Get-Module Fluence.Wpf.PowerShell) {
            param($path)
            $savedRunspace = $script:StaRunspace
            $savedSlot = $script:StaRunspaceSlot
            $savedManifest = $script:ModuleManifestPath
            $probe = $null
            $isolated = $null
            try
            {
                $script:StaRunspace = $null
                $script:StaRunspaceSlot = [guid]::NewGuid().ToString()
                $script:ModuleManifestPath = $path
                $isolated = Initialize-FluenceStaRunspace
                $probe = [powershell]::Create()
                $probe.Runspace = $isolated
                return ($probe.AddCommand('Get-Module').AddParameter('Name', 'ApostropheProbe').Invoke().Count -eq 1)
            }
            finally
            {
                if ($null -ne $probe) { $probe.Dispose() }
                if ($null -ne $isolated) { $isolated.Dispose() }
                [System.AppDomain]::CurrentDomain.SetData($script:StaRunspaceSlot, $null)
                $script:StaRunspace = $savedRunspace
                $script:StaRunspaceSlot = $savedSlot
                $script:ModuleManifestPath = $savedManifest
            }
        } $manifest
        $loaded | Should -BeTrue
    }
    It 'honors false caption switches and cleans up a failing <Mode> callback' -ForEach @(
        @{ Mode = 'Content' }
        @{ Mode = 'Initialize' }
    ) {
        $facts = [hashtable]::Synchronized(@{})
        $callback = {
                param($window, $data)
                $data.Minimize = $window.IsMinimizeButtonVisible.ToString()
                $data.Maximize = $window.IsMaximizeButtonVisible.ToString()
                $data.Close = $window.IsCloseButtonVisible.ToString()
                $data.RegisteredWindows = [System.Windows.Application]::Current.Windows.Count
                throw 'Construction probe completed before ShowDialog'
        }
        $arguments = @{ NoMinimizeButton = $false; NoMaximizeButton = $false; NoCloseButton = $false; Data = $facts }
        if ($Mode -eq 'Content')
        {
            $arguments.Content = $callback
        }
        else
        {
            $arguments.Xaml = '<Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" />'
            $arguments.Initialize = $callback
        }
        { Show-FluenceWindow @arguments } | Should -Throw '*Construction probe completed*'
        $facts.Minimize | Should -Be 'Visible'
        $facts.Maximize | Should -Be 'Visible'
        $facts.Close | Should -Be 'Visible'
        $remaining = & (Get-Module Fluence.Wpf.PowerShell) {
            Invoke-OnFluenceUi -Script { [System.Windows.Application]::Current.Windows.Count }
        }
        $remaining | Should -Be ($facts.RegisteredWindows - 1)
    }
}
