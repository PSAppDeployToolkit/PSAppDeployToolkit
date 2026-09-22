#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }
BeforeAll {
    $script:ModulePath = Join-Path $PSScriptRoot '../src/Fluence.Wpf.PowerShell/Fluence.Wpf.PowerShell.psd1'
}

Describe 'Terminal cleanup ownership' {
    It 'does not install terminal cleanup on a child runspace using <HostKind>' -ForEach @(
        @{ HostKind = 'DefaultHost' }
        @{ HostKind = 'the parent console host' }
    ) {
        $runspace = if ($HostKind -eq 'DefaultHost')
        {
            [runspacefactory]::CreateRunspace()
        }
        else
        {
            [runspacefactory]::CreateRunspace($Host)
        }
        $pipeline = $null
        try
        {
            $runspace.Open()
            $pipeline = [powershell]::Create()
            $pipeline.Runspace = $runspace
            $null = $pipeline.AddScript({
                param($manifest)
                Import-Module $manifest
                & (Get-Module Fluence.Wpf.PowerShell) { Register-FluenceRunspaceExit }
                return @(Get-EventSubscriber -Force | Where-Object { $_.SourceIdentifier -eq 'PowerShell.Exiting' }).Count
            }).AddArgument($script:ModulePath)
            $output = $pipeline.Invoke()
            $pipeline.HadErrors | Should -BeFalse
            $output.Count | Should -Be 1
            $output[0] | Should -Be 0
        }
        finally
        {
            if ($null -ne $pipeline) { $pipeline.Dispose() }
            $runspace.Dispose()
        }
    }
}
