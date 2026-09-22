#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.0.0' }

BeforeAll {
    $script:ModuleSource = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../src/Fluence.Wpf.PowerShell'))
    $script:ShellPath = (Get-Process -Id $PID).Path
    $script:ImportProbe = Join-Path $TestDrive 'probe.ps1'
    $probe = @'
param([string]$Manifest)
$ErrorActionPreference = 'Stop'
try
{
    $module = Import-Module -Name $Manifest -PassThru -Force -ErrorAction Stop
    [pscustomobject]@{
        Imported = $true
        Commands = @($module.ExportedFunctions.Keys)
        Sample = (New-FluencePrompt -Message 'probe').Message
    } | ConvertTo-Json -Compress
}
catch
{
    [pscustomobject]@{ Imported = $false; Error = $_.Exception.Message } | ConvertTo-Json -Compress
}
'@
    [System.IO.File]::WriteAllText($script:ImportProbe, $probe, [System.Text.UTF8Encoding]::new($true))
}

Describe 'Public module import in a fresh process' {
    It 'exports and executes public commands from a path containing brackets and spaces' {
        $package = Join-Path $TestDrive '[release] module'
        Copy-Item -LiteralPath $script:ModuleSource -Destination $package -Recurse
        $manifest = Join-Path $package 'Fluence.Wpf.PowerShell.psd1'
        $result = & $script:ShellPath -NoProfile -NonInteractive -File $script:ImportProbe -Manifest $manifest | ConvertFrom-Json
        $LASTEXITCODE | Should -Be 0
        $result.Imported | Should -BeTrue
        $result.Sample | Should -Be 'probe'
        $expected = Import-PowerShellDataFile -LiteralPath $manifest
        @($result.Commands).Count | Should -Be @($expected.FunctionsToExport).Count
        foreach ($command in $expected.FunctionsToExport)
        {
            $result.Commands | Should -Contain $command
        }
    }

    It 'fails import when the <Folder> script directory is <Condition>' -ForEach @(
        @{ Folder = 'Public'; Condition = 'missing' }
        @{ Folder = 'Private'; Condition = 'missing' }
        @{ Folder = 'Public'; Condition = 'empty' }
        @{ Folder = 'Private'; Condition = 'empty' }
    ) {
        $package = Join-Path $TestDrive ($Folder + '-' + $Condition)
        Copy-Item -LiteralPath $script:ModuleSource -Destination $package -Recurse
        $directory = Join-Path $package $Folder
        Move-Item -LiteralPath $directory -Destination ($package + '-removed')
        if ($Condition -eq 'empty')
        {
            $null = New-Item -ItemType Directory -Path $directory
        }
        $result = & $script:ShellPath -NoProfile -NonInteractive -File $script:ImportProbe -Manifest (Join-Path $package 'Fluence.Wpf.PowerShell.psd1') | ConvertFrom-Json
        $LASTEXITCODE | Should -Be 0
        $result.Imported | Should -BeFalse
        $result.Error | Should -Match $Folder
    }
}
